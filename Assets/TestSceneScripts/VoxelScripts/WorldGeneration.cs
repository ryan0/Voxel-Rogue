using UnityEngine;

public class WorldGeneration
{
    private static float PerlinNoise3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float xz = Mathf.PerlinNoise(x, z);
        float yz = Mathf.PerlinNoise(y, z);
        float yx = Mathf.PerlinNoise(y, x);
        float zx = Mathf.PerlinNoise(z, x);
        float zy = Mathf.PerlinNoise(z, y);

        return (xy + xz + yz + yx + zx + zy) / 6f;
    }

    public static Substance[,,] genTerrain()
    {
        int width = World.chunksX * Chunk.width;
        int height = World.chunksY * Chunk.height;
        int depth = World.chunksZ * Chunk.depth;
        Substance[,,] terrain = new Substance[width, height, depth];

        float scale = 0.1f * Voxel.size;  // Adjust this value to change the 'resolution' of your terrain
        float heightScale = 15.0f;  // Adjust this value to change the maximum height of the terrain
        float waterScale = 0.01f;  // Adjust this value to change the 'roughness' of your water distribution (smaller for larger bodies)
        float waterThreshold = 0.5f;  // Lower this value to make water more common
        int floorValue = 64;

        int waterLevel = 3; // Increase this Y coordinate to make the land more often under water
        waterLevel += floorValue;
        // Variables related to tree generation
        float treeProbability = 0.005f;  // Probability of tree being generated at any eligible location
        //int minTreeSpacing = 3;  // Minimum distance between trees
        int[,] terrainHeights = new int[width, depth];  // Store terrain height for each (x, z)
        System.Random random = new System.Random();  // Seed this for deterministic tree placement

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                // Calculate the height of the terrain at this point
                int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, z * scale) * heightScale);
                terrainHeight += floorValue;
                // Calculate the water noise at this point
                float waterNoise = Mathf.PerlinNoise(x * waterScale, z * waterScale);

                for (int y = 0; y < height; y++)
                {
                    if (y < terrainHeight)
                    {
                        // Below the terrain height, we fill with Voxel types
                        // Here, we make a simple decision: if it's the top layer, place Dirt; otherwise, Stone
                        if (y == terrainHeight - 1 && y >= waterLevel)
                        {
                            terrain[x, y, z] = Substance.dirt;
                        }
                        else
                        {
                            terrain[x, y, z] = Substance.stone;
                        }
                    }
                    else
                    {
                        // Above the terrain height, we fill with Air or Water based on the water noise
                        if (waterNoise > waterThreshold && y <= waterLevel)
                        {
                            terrain[x, y, z] = Substance.water;
                        }
                        else
                        {
                            terrain[x, y, z] = Substance.air;
                        }
                    }
                }
            }
        }



        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, z * scale) * heightScale);
                terrainHeight += floorValue;

                if (random.NextDouble() < treeProbability && terrainHeight > waterLevel)
                {
                    Vector3Int treePos = new Vector3Int(x, terrainHeight, z);
                    GenerateTree(terrain, treePos);
                }
            }
        }

        // Generate some worms
        int numWorms = 5;  // Start with a reasonable number of worms
        for (int i = 0; i < numWorms; i++)
        {
            GenerateWorm(terrain);
        }

        return terrain;
    }

    private static void GenerateWorm(Substance[,,] terrain)
    {
        int width = terrain.GetLength(0);
        int height = terrain.GetLength(1);
        int depth = terrain.GetLength(2);

        // Random starting position for the worm
        Vector3Int wormPos = new Vector3Int(Random.Range(0, width), Random.Range(0, height), Random.Range(0, depth));

        // Length of the worm
        int wormLength = 400;  // Adjust as necessary

        // Random direction for the worm to move in
        Vector3 wormDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        wormDirection.Normalize(); // ensure the direction vector is normalized

        // Noise scale
        float noiseScale = 0.4f;

        // Base size of the worm/cave
        int baseWormSize = 8; // The larger the size, the larger the cave. Adjust as necessary.

        for (int i = 0; i < wormLength; i++)
        {
            // Use Perlin noise to get a size multiplier ranging from 0.5 to 1.5
            float sizeMultiplier = Mathf.PerlinNoise(i * noiseScale, i * noiseScale) + 0.5f;

            // Determine the size of the worm at this point
            int wormSize = Mathf.FloorToInt(baseWormSize * sizeMultiplier);

            // Carve out a path for the worm
            for (int dx = -wormSize; dx <= wormSize; dx++)
            {
                for (int dy = -wormSize; dy <= wormSize; dy++)
                {
                    for (int dz = -wormSize; dz <= wormSize; dz++)
                    {
                        // Determine if this point is within the sphere
                        double distance = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);

                        if (distance <= wormSize)
                        {
                            int x = wormPos.x + dx;
                            int y = wormPos.y + dy;
                            int z = wormPos.z + dz;

                            // Check that we're within the world boundaries
                            if (x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth)
                            {
                                terrain[x, y, z] = Substance.air;
                            }
                        }
                    }
                }
            }

            // Change the direction more frequently and with larger range
            if (Random.value < 0.4f)  // 40% chance to change direction
            {
                // Randomly select a new direction for the worm to move in
                wormDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
                wormDirection.Normalize();
            }

            // Move the worm
            wormPos += Vector3Int.FloorToInt(wormDirection * (Random.Range(1, 3)));

            // Make sure the worm stays within the world boundaries
            wormPos.x = Mathf.Clamp(wormPos.x, 1, width - 2);
            wormPos.y = Mathf.Clamp(wormPos.y, 1, height - 2);
            wormPos.z = Mathf.Clamp(wormPos.z, 1, depth - 2);
        }
    }


    private static void GenerateTree(Substance[,,] voxels, Vector3Int position)
    {
        System.Random random = new System.Random();  // Seed this for deterministic tree placement

        int width = voxels.GetLength(0);
        int height = voxels.GetLength(1);
        int depth = voxels.GetLength(2);

        // Introduce randomness in tree dimensions
        int trunkHeight = random.Next(5, 10); // Trees will have height between 3 and 7
        int trunkThickness = random.Next(1, 4); // Trunk will have thickness between 1 and 3
        int crownRadius = random.Next(4, 7); // Crown will have radius between 2 and 4

        // Generate trunk
        for (int xt = position.x; xt < position.x + trunkThickness; xt++)
        {
            for (int zt = position.z; zt < position.z + trunkThickness; zt++)
            {
                for (int y = position.y; y < position.y + trunkHeight; y++)
                {
                    if (xt < width && zt < depth && y < height)
                    { // Check bounds
                        voxels[xt, y, zt] = Substance.wood;
                    }
                }
            }
        }

        // Generate crown with a more varied shape
        Vector3Int crownCenter = position + new Vector3Int(0, trunkHeight, 0);

        for (int x = crownCenter.x - crownRadius; x <= crownCenter.x + crownRadius; x++)
        {
            for (int y = crownCenter.y; y <= crownCenter.y + crownRadius; y++)
            {
                for (int z = crownCenter.z - crownRadius; z <= crownCenter.z + crownRadius; z++)
                {
                    Vector3Int voxelPosition = new Vector3Int(x, y, z);
                    // Generate some random roughness factors
                    float roughnessX = 0.8f + 0.4f * (float)random.NextDouble();
                    float roughnessY = 0.8f + 0.4f * (float)random.NextDouble();
                    float roughnessZ = 0.8f + 0.4f * (float)random.NextDouble();

                    // Check if this voxel position is within the "rough" ellipsoid (crown)
                    if ((Mathf.Pow(voxelPosition.x - crownCenter.x, 2) / Mathf.Pow(crownRadius * roughnessX, 2)
                        + Mathf.Pow(voxelPosition.y - crownCenter.y, 2) / Mathf.Pow(crownRadius * roughnessY, 2)
                        + Mathf.Pow(voxelPosition.z - crownCenter.z, 2) / Mathf.Pow(crownRadius * roughnessZ, 2) <= 1
                        // Special case for the top of the trunk
                        || (voxelPosition.x >= position.x && voxelPosition.x < position.x + trunkThickness
                            && voxelPosition.z >= position.z && voxelPosition.z < position.z + trunkThickness
                            && voxelPosition.y <= position.y + trunkHeight))
                        && x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth)
                    { // Check bounds
                        voxels[x, y, z] = Substance.leaf;
                    }
                }
            }
        }
    }


}
