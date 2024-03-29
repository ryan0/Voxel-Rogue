using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldGeneration
{
    static Geometry geo;
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

    public static int width = World.chunksX * Chunk.width;
    public static int height = World.chunksY * Chunk.height;
    public static int depth = World.chunksZ * Chunk.depth;
    public static int[,] terrainHeights = new int[width, depth];

    public static Substance[,,] GenerateTerrain()
    {
        geo = new Geometry();
        Substance[,,] terrain = new Substance[width, height, depth];
        float scale = 0.1f * Voxel.size;
        float heightScale = 30.0f;
        int floorValue = 64;
        float treeProbability = 0.005f;
        //Voxel[,,] voxels = new Voxel[width, height, depth];
        System.Random random = new System.Random();

        CalculateTerrainHeights(width, depth, scale, heightScale, floorValue, terrainHeights);

        GenerateTerrainBlocks(width, height, depth, terrainHeights, terrain);

        //GenerateWorms(terrain, 3);

        //GenerateRivers(floorValue, terrain, terrainHeights, 1, Substance.water);

        //GenerateRivers(floorValue, terrain, terrainHeights, 1, Substance.lava, 200);

        //GenerateClouds(terrain, 30);

        int maxTowerCount = 40; // Adjust the value as needed
        TownGeneration townGenerator = new TownGeneration(terrain, Chunk.width, 1, 1.0f);//town probability is 100
        townGenerator.GenerateTowns();

        //GenerateTrees(width, depth, scale, heightScale, floorValue, treeProbability, terrain, random);


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

    /// <summary>
    /// Cloud generation
    /// </summary>
    public static void GenerateClouds(Substance[,,] terrain, int numClouds)
    {
        int width = terrain.GetLength(0);
        int height = terrain.GetLength(1);
        int depth = terrain.GetLength(2);

        int cloudHeight = GasFlowSystem.MAX_GAS_HEIGHT; // The height at which clouds should generate
        float cloudSizeLarge = 0.02f; // The scale of the larger cloud structures
        float cloudSizeSmall = 0.1f; // The scale of the smaller cloud structures
        float cloudDensity = 0.4f; // The threshold for cloud density, higher values result in fewer clouds

        float largeCloudOblongScale = 3.0f; // The scale in the x direction for the larger cloud structures
        float smallCloudOblongScale = 0.5f; // The scale in the x direction for the smaller cloud structures

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                float cloudValueLarge = Mathf.PerlinNoise(x * cloudSizeLarge * largeCloudOblongScale, z * cloudSizeLarge);
                float cloudValueSmall = Mathf.PerlinNoise(x * cloudSizeSmall * smallCloudOblongScale, z * cloudSizeSmall);

                // Combine the large and small cloud values, weighting the large value more heavily
                float cloudValue = 0.7f * cloudValueLarge + 0.3f * cloudValueSmall;

                if (cloudValue > cloudDensity)
                {
                    if (cloudHeight < height)
                    {
                        terrain[x, cloudHeight, z] = Substance.steam;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Perlin worm
    /// </summary>
    private static void GenerateWorms(Substance[,,] terrain, int numWorms)
    {
        for (int i = 0; i < numWorms; i++)
        {
            GenerateWorm(terrain);
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
    /// <summary>
    /// River generation
    /// </summary>
    private static void GenerateRivers(int floorValue, Substance[,,] terrain, int[,] terrainHeights, int numRivers, Substance riverType, int riverLength = 800, int baseRiverSize = 5)
    {
        for (int i = 0; i < numRivers; i++)
        {
            GenerateRiver(floorValue, terrainHeights, terrain, riverType, riverLength, baseRiverSize);
        }
    }


    private static void GenerateRiver(int floorValue, int[,] terrainHeights, Substance[,,] terrain, Substance riverType, int riverLength = 800, int baseRiverSize = 5)
    {
        int width = terrain.GetLength(0);
        int depth = terrain.GetLength(2);
        floorValue += 8;

        // Random starting position for the river
        Vector3Int riverPos = new Vector3Int(Random.Range(0, width), floorValue, Random.Range(0, depth));

        // Length of the river
        //int riverLength = 800;  // Adjust as necessary

        // Random direction for the river to move in (only in x and z)
        Vector3 riverDirection = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
        riverDirection.Normalize(); // ensure the direction vector is normalized

        // Noise scale
        float noiseScale = 0.05f;

        // Base size of the river
        //int baseRiverSize = 5; // The larger the size, the larger the river. Adjust as necessary.

        for (int i = 0; i < riverLength; i++)
        {
            // Use Perlin noise to get a size multiplier ranging from 0.5 to 1.5
            //float sizeMultiplier = Mathf.PerlinNoise(i * noiseScale, i * noiseScale) + .5f;
            // Perlin noise now generates values between 0.75 and 1.25
            float sizeMultiplier = (Mathf.PerlinNoise(i * noiseScale, i * noiseScale) / 2f) + 0.75f;

            // Determine the size of the river at this point
            int riverSize = Mathf.FloorToInt(baseRiverSize * sizeMultiplier);

            // Carve out a path for the river
            for (int dx = -riverSize; dx <= riverSize; dx++)
            {
                for (int dz = -riverSize; dz <= riverSize; dz++)
                {
                    for (int dy = -riverSize; dy <= riverSize; dy++)
                    {
                        // Determine if this point is within the sphere
                        double distance = Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
                        if (distance <= riverSize)
                        {
                            int x = riverPos.x + dx;
                            int y = riverPos.y + dy;
                            int z = riverPos.z + dz;

                            // Wrap around the world boundaries
                            x = (x + width) % width;
                            //y = (y + height) % height;
                            z = (z + depth) % depth;

                            terrain[x, y, z] = Substance.air;

                            if (dy < riverSize / 2)
                            {
                                if (terrain[x, y, z] == Substance.air)
                                {
                                    terrain[x, y, z] = riverType;
                                }
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

    /// <summary>
    /// Tree generation
    /// </summary>

    private static void GenerateTrees(int width, int depth, float scale, float heightScale, int floorValue, float treeProbability, Substance[,,] terrain, System.Random random)
    {
        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                //int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, z * scale) * heightScale);
                //terrainHeight += floorValue;
                int terrainHeight = terrainHeights[x, z];
                bool invalidTreeSpot = true;//= CheckInvalidTreeSpot(x, z, terrain, terrainHeight, floorValue, TownGeneration.WorldTownsData);
                /// TO DO FIX THIS
                Vector3Int treePos = new Vector3Int(x, terrainHeight, z);
                //int minDistance = 8;
                if (random.NextDouble() < treeProbability && !invalidTreeSpot)// && !IsTooCloseToTowerOrWall(treePos, TownGeneration.towerLocs, TownGeneration.wallPositionsAll, minDistance))
                {
                    GenerateTree(terrain, treePos);
                }

            }
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

    private static bool CheckInvalidTreeSpot(int x, int z, Substance[,,] terrain, int terrainHeight, int floor, List<TownData> worldTownsData)
    {
        bool invalidTreeSpot = false;

        int y = terrainHeight-1;

        if (terrain[x, y, z] == Substance.water || terrain[x, y, z] == Substance.air || terrain[x, y, z] == Substance.asphalt || terrain[x, y, z] == Substance.stone)
        {
            invalidTreeSpot = true;
            //terrain[x, y, z] = Substance.debug;

        }

        // Check if position is within any town
       /*foreach (var town in worldTownsData)
        {

            // Check if the position is within the bounds of this town
            if (geo.IsPointInPolygonWithExtension(new Vector3Int(x,y,z),town.TowerLocs.ToList(), 12))//12 away from walls
            {
                invalidTreeSpot = true;
                break;
            }
            else
            {

            }
        }*/

        return invalidTreeSpot;
    }


    private static bool IsTooCloseToTowerOrWall(Vector3Int position, List<Vector3Int> towerPositions, List<Vector3Int> wallPositions, int minDistance)
    {
        foreach (Vector3Int towerPosition in towerPositions)
        {
            if (Vector3Int.Distance(position, towerPosition) < minDistance)
            {
                return true;
            }
        }

        foreach (Vector3Int wallPosition in wallPositions)
        {
            if (Vector3Int.Distance(position, wallPosition) < minDistance)
            {
                return true;
            }
        }

        return false;
    }


    /// <summary>
    /// misc experiments
    /// </summary>
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

    public static bool IsWithinBounds(int x, int y, int z)
    {
        return x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth;
    }


}

