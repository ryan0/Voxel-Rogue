using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

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

    public static Substance[,,] GenerateTerrain()
    {
        int width = World.chunksX * Chunk.width;
        int height = World.chunksY * Chunk.height;
        int depth = World.chunksZ * Chunk.depth;
        int[,] terrainHeights = new int[width, depth];
        Substance[,,] terrain = new Substance[width, height, depth];
        float scale = 0.1f * Voxel.size;
        float heightScale = 30.0f;
        int floorValue = 64;
        float treeProbability = 0.005f;
        System.Random random = new System.Random();

        CalculateTerrainHeights(width, depth, scale, heightScale, floorValue, terrainHeights);

        GenerateTerrainBlocks(width, height, depth, terrainHeights, terrain);

        GenerateWorms(terrain, 5);

        GenerateRivers(floorValue, terrain, terrainHeights, 1);

        GenerateTrees(width, depth, scale, heightScale, floorValue, treeProbability, terrain, random);

        return terrain;
    }

    private static void CalculateTerrainHeights(int width, int depth, float scale, float heightScale, int floorValue, int[,] terrainHeights)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, z * scale) * heightScale);
                terrainHeight += floorValue;
                terrainHeights[x, z] = terrainHeight;
            }
        }
    }

    private static void GenerateTerrainBlocks(int width, int height, int depth, int[,] terrainHeights, Substance[,,] terrain)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int terrainHeight = terrainHeights[x, z];

                for (int y = 0; y < height; y++)
                {
                    if (y < terrainHeight)
                    {
                        if (y == terrainHeight - 1)
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
                       
                        terrain[x, y, z] = Substance.air;
                    }
                }
            }
        }
    }

    private static void GenerateWorms(Substance[,,] terrain, int numWorms)
    {
        for (int i = 0; i < numWorms; i++)
        {
            GenerateWorm(terrain);
        }
    }

    private static void GenerateRivers(int floorValue, Substance[,,] terrain, int[,] terrainHeights, int numRivers)
    {
        for (int i = 0; i < numRivers; i++)
        {
            GenerateRiver(floorValue, terrainHeights, terrain);
        }
    }


    private static void GenerateRiver(int floorValue, int[,] terrainHeights, Substance[,,] terrain)
    {
        int width = terrain.GetLength(0);
        int depth = terrain.GetLength(2);
        floorValue += 16;

        // Random starting position for the river
        Vector3Int riverPos = new Vector3Int(Random.Range(0, width), floorValue, Random.Range(0, depth));

        // Length of the river
        int riverLength = 500;  // Adjust as necessary

        // Random direction for the river to move in (only in x and z)
        Vector3 riverDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        riverDirection.Normalize(); // ensure the direction vector is normalized

        // Noise scale
        float noiseScale = 0.05f;

        // Base size of the river
        int baseRiverSize = 8; // The larger the size, the larger the river. Adjust as necessary.

        for (int i = 0; i < riverLength; i++)
        {
            // Use Perlin noise to get a size multiplier ranging from 0.5 to 1.5
            float sizeMultiplier = Mathf.PerlinNoise(i * noiseScale, i * noiseScale) + 0.5f;

            // Determine the size of the river at this point
            int riverSize = Mathf.FloorToInt(baseRiverSize * sizeMultiplier);

            // Carve out a path for the river
            for (int dx = -riverSize; dx <= riverSize; dx++)
            {
                for (int dz = -riverSize; dz <= riverSize; dz++)
                {
                    // Determine if this point is within the circle (a slice of the sphere at y = floorValue)
                    double distance = Mathf.Sqrt(dx * dx + dz * dz);

                    if (distance <= riverSize)
                    {
                        int x = riverPos.x + dx;
                        int z = riverPos.z + dz;

                        // Wrap around the world boundaries
                        x = (x + width) % width;
                        z = (z + depth) % depth;

                        // Replace the ground with air and water at the halfway point
                        for (int y = 0; y < terrain.GetLength(1); y++)
                        {
                            if (y < floorValue - riverSize / 2)
                            {
                                terrain[x, y, z] = Substance.air;
                            }
                            else if (y == floorValue - riverSize / 2)
                            {
                                terrain[x, y, z] = Substance.water;
                            }
                        }
                    }
                }
            }

            // Change the direction more frequently and with larger range
            if (Random.value < 0.3f)  // 30% chance to change direction
            {
                // Randomly select a new direction for the river to move in (only in x and z)
                riverDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
                riverDirection.Normalize();
            }

            // Move the river
            riverPos += Vector3Int.FloorToInt(riverDirection * (Random.Range(1, 3)));

            // Wrap around the world boundaries
            riverPos.x = (riverPos.x + width) % width;
            riverPos.z = (riverPos.z + depth) % depth;
        }
    }




    private static void GenerateTrees(int width, int depth, float scale, float heightScale, int floorValue, float treeProbability, Substance[,,] terrain, System.Random random)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, z * scale) * heightScale);
                terrainHeight += floorValue;

                bool invalidTreeSpot = CheckInvalidTreeSpot(x, z, terrain, terrainHeight, floorValue);

                if (random.NextDouble() < treeProbability && !invalidTreeSpot)
                {
                    Vector3Int treePos = new Vector3Int(x, terrainHeight, z);
                    GenerateTree(terrain, treePos);
                }
            }
        }
    }

    private static bool CheckInvalidTreeSpot(int x, int z, Substance[,,] terrain, int terrainHeight, int floor)
    {
        bool invalidTreeSpot = false;
        for (int y = terrainHeight - 1; y >= floor; y--)
        {
            if (terrain[x, y, z] == Substance.water || terrain[x, y, z] == Substance.air)
            {
                invalidTreeSpot = true;
                break;
            }
        }
        return invalidTreeSpot;
    }

    public static void GenerateTerraceFarms(Substance[,,] terrain, int[,] terrainHeights, float riverThreshold = 0.2f, int maxRiverLength = 100)
    {
        int width = terrain.GetLength(0);
        int depth = terrain.GetLength(2);
        float scale = 0.05f;

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float noise = Mathf.PerlinNoise(x * scale, z * scale);
                if (noise > riverThreshold)
                {
                    CarveTerraceFarms(terrain, terrainHeights, new Vector2Int(x, z), maxRiverLength);
                }
            }
        }
    }

    private static void CarveTerraceFarms(Substance[,,] terrain, int[,] terrainHeights, Vector2Int start, int maxRiverLength)
    {
        Vector2Int[] directions = new Vector2Int[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        Vector2Int current = start;
        int riverDepth = 5;  // Adjust this value to control the depth of the rivers

        for (int i = 0; i < maxRiverLength; i++)
        {
            int x = current.x;
            int z = current.y;

            // Calculate start and end height for river carving
            int startHeight = Mathf.Max(terrainHeights[x, z] - riverDepth, 0);
            int endHeight = terrainHeights[x, z];

            // Make river bottom
            for (int y = startHeight; y < endHeight; y++)
            {
                terrain[x, y, z] = Substance.water;
            }

            // Make river banks
            for (int y = startHeight; y < endHeight; y++)
            {
                foreach (Vector2Int dir in directions)
                {
                    int nx = x + dir.x;
                    int nz = z + dir.y;
                    if (nx >= 0 && nx < terrain.GetLength(0) && nz >= 0 && nz < terrain.GetLength(2))
                    {
                        // Change surrounding blocks that are at or below the terrain surface to dirt
                        if (terrain[nx, y, nz] != Substance.water && y <= terrainHeights[nx, nz])
                        {
                            terrain[nx, y, nz] = Substance.dirt;
                        }
                    }
                }
            }

            // Find next point with lowest height
            Vector2Int? next = null;
            foreach (Vector2Int dir in directions)
            {
                int nx = x + dir.x;
                int nz = z + dir.y;
                if (nx >= 0 && nx < terrain.GetLength(0) && nz >= 0 && nz < terrain.GetLength(2))
                {
                    if (next == null || terrainHeights[nx, nz] < terrainHeights[next.Value.x, next.Value.y])
                    {
                        next = new Vector2Int(nx, nz);
                    }
                }
            }

            if (next.HasValue && terrainHeights[next.Value.x, next.Value.y] < terrainHeights[x, z])
            {
                current = next.Value;
            }
            else
            {
                break;
            }
        }
    }

    private static void GenerateFlow(Substance[,,] terrain, int[,] terrainHeights, int riverAirThreshold)
    {
        int width = terrain.GetLength(0);
        int height = terrain.GetLength(1);
        int depth = terrain.GetLength(2);

        // Variables related to river generation
        Vector3Int riverPos = new Vector3Int(Random.Range(0, width), 0, Random.Range(0, depth));  // Initial position of the river
        Vector3 riverDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
        riverDirection.Normalize();  // Ensure the direction vector is normalized

        // Length of the river
        int riverLength = 500;  // Adjust as necessary

        // Base size of the river
        int baseRiverSize = 8;  // The larger the size, the larger the river. Adjust as necessary.

        for (int i = 0; i < riverLength; i++)
        {
            // We count the number of Air blocks above the current point
            int airCount = 0;
            for (int y = terrainHeights[riverPos.x, riverPos.z] + 1; y < height; y++)
            {
                if (terrain[riverPos.x, y, riverPos.z] == Substance.air)
                {
                    airCount++;
                }
            }

            // If there are fewer than X Air blocks above this point, we don't generate a river here
            if (airCount < riverAirThreshold)
            {
                continue;
            }

            // Carve out a path for the river
            for (int dx = -baseRiverSize; dx <= baseRiverSize; dx++)
            {
                for (int dz = -baseRiverSize; dz <= baseRiverSize; dz++)
                {
                    // Determine if this point is within the river
                    double distance = Mathf.Sqrt(dx * dx + dz * dz);

                    if (distance <= baseRiverSize)
                    {
                        int x = riverPos.x + dx;
                        int z = riverPos.z + dz;

                        // Wrap around the world boundaries
                        x = (x + width) % width;
                        z = (z + depth) % depth;

                        // We don't lower the terrain here, we just fill it with water
                        for (int y = 0; y <= terrainHeights[x, z]; y++)
                        {
                            terrain[x, y, z] = Substance.water;
                            if (y > 0)
                            {
                                terrain[x, y - 1, z] = Substance.dirt;  // Replace the block under the water with dirt
                            }
                        }
                    }
                }
            }

            // Change the direction more frequently
            if (Random.value < 0.4f)  // 40% chance to change direction
            {
                riverDirection = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f));
                riverDirection.Normalize();
            }

            // Move the river
            riverPos += Vector3Int.FloorToInt(riverDirection);

            // Wrap around the world boundaries
            riverPos.x = (riverPos.x + width) % width;
            riverPos.z = (riverPos.z + depth) % depth;
        }
    }

    private static void GenerateRiver1(Substance[,,] terrain, int[,] terrainHeights, int riverAirThreshold)
    {

        int width = terrain.GetLength(0);
        int height = terrain.GetLength(1);
        int depth = terrain.GetLength(2);

        // Variables related to river generation
        Vector3Int riverPos = new Vector3Int(Random.Range(0, width), 0, Random.Range(0, depth));  // Initial position of the river

        // Random starting position for the worm
        Vector3Int wormPos = new Vector3Int(riverPos.x, terrainHeights[riverPos.x, riverPos.y], riverPos.z);

        // Length of the worm
        int wormLength = 1000;  // Adjust as necessary

        // Random direction for the worm to move in
        Vector3 wormDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-.01f,0), Random.Range(-1f, 1f));
        wormDirection.Normalize(); // ensure the direction vector is normalized

        // Noise scale
        float noiseScale = 0.05f;

        // Base size of the worm/cave
        int baseWormSize = 6; // The larger the size, the larger the cave. Adjust as necessary.

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

                            // Wrap around the world boundaries
                            x = (x + width) % width;
                            y = (y + height) % height;
                            z = (z + depth) % depth;

                            terrain[x, y, z] = Substance.water;
                        }
                    }
                }
            }

            // Change the direction more frequently and with larger range
            if (Random.value < 0.05f)  // 40% chance to change direction
            {
                // Randomly select a new direction for the worm to move in
                wormDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.01f, 0), Random.Range(-1f, 1f));
                wormDirection.Normalize();
            }


            // Move the worm
            wormPos += Vector3Int.FloorToInt(wormDirection * (Random.Range(1, 3)));

            // Wrap around the world boundaries
            wormPos.x = (wormPos.x + width) % width;
            wormPos.y = (wormPos.y + height) % height;
            wormPos.z = (wormPos.z + depth) % depth;
        }
    }

    private static void GenerateWorm(Substance[,,] terrain)
    {
        int width = terrain.GetLength(0);
        int height = terrain.GetLength(1);
        int depth = terrain.GetLength(2);

        // Random starting position for the worm
        Vector3Int wormPos = new Vector3Int(Random.Range(0, width), Random.Range(0, height), Random.Range(0, depth));

        // Length of the worm
        int wormLength = 500;  // Adjust as necessary

        // Random direction for the worm to move in
        Vector3 wormDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        wormDirection.Normalize(); // ensure the direction vector is normalized

        // Noise scale
        float noiseScale = 0.05f;

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

                            // Wrap around the world boundaries
                            x = (x + width) % width;
                            y = (y + height) % height;
                            z = (z + depth) % depth;

                            terrain[x, y, z] = Substance.air;
                        }
                    }
                }
            }

            // Change the direction more frequently and with larger range
            if (Random.value < 0.3f)  // 40% chance to change direction
            {
                // Randomly select a new direction for the worm to move in
                wormDirection = new Vector3(Random.Range(-1f, 1f), Random.Range(-0.5f, .2f), Random.Range(-1f, 1f));
                wormDirection.Normalize();
            }


            // Move the worm
            wormPos += Vector3Int.FloorToInt(wormDirection * (Random.Range(1, 3)));

            // Wrap around the world boundaries
            wormPos.x = (wormPos.x + width) % width;
            wormPos.y = (wormPos.y + height) % height;
            wormPos.z = (wormPos.z + depth) % depth;
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

