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
        //Voxel[,,] voxels = new Voxel[width, height, depth];

        CalculateTerrainHeights(width, depth, scale, heightScale, floorValue, terrainHeights);

        GenerateTerrainBlocks(width, height, depth, terrainHeights, terrain);

        GenerateWorms(terrain, 3);

        GenerateRivers(floorValue, terrain, terrainHeights, 1, Substance.water);

        GenerateRivers(floorValue, terrain, terrainHeights, 1, Substance.lava, 200);

        GenerateClouds(terrain, 30);

        int maxTowerCount = 10; // Adjust the value as needed
        GenerateTowers(terrain, floorValue, scale, heightScale, maxTowerCount);

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

    /// <summary>
    /// Tower generation and tower fields
    /// </summary>
    static int towerWidth = 5, towerHeight = 16, towerDepth = 5;
    static int doorHeight = 4, doorWidth = 2;
    static int minimumDistance = 12;
    static List<Vector3Int> towerLocations;

    public static void GenerateTowers(Substance[,,] terrain, int floorValue, float scale, float heightScale, int maxTowerCount)
    {
        List<Vector3Int> towerLocs = ScanTerrainForTowerLocations(terrain, floorValue, scale, heightScale, maxTowerCount, minimumDistance);

        foreach (Vector3Int loc in towerLocs)
        {
            int towerHeight = GenerateTowerAtLocation(terrain, loc);
            floorValue += towerHeight; // Adjust floorValue to avoid overlapping towers
        }

        // Clustering
        List<List<Vector3Int>> clusters = FindClusters(towerLocs, minimumDistance);

        // Building walls around clusters
        foreach (List<Vector3Int> cluster in clusters)
        {
            List<Vector3Int> convexHull = FindConvexHull(cluster);
            for (int i = 0; i < convexHull.Count; i++)
            {
                Vector3Int start = convexHull[i];
                Vector3Int end = convexHull[(i + 1) % convexHull.Count];
                BuildWallBetweenTowers(terrain, start, end, towerHeight, towerHeight, towerWidth, towerDepth);
            }
        }
    }

    public static List<List<Vector3Int>> FindClusters(List<Vector3Int> towerLocations, float minDistance)
    {
        List<List<Vector3Int>> clusters = new List<List<Vector3Int>>();
        foreach (var tower in towerLocations)
        {
            bool addedToCluster = false;
            foreach (var cluster in clusters)
            {
                foreach (var existingTower in cluster)
                {
                    if (Vector3Int.Distance(existingTower, tower) < minDistance)
                    {
                        cluster.Add(tower);
                        addedToCluster = true;
                        break;
                    }
                }
                if (addedToCluster) break;
            }
            if (!addedToCluster)
            {
                clusters.Add(new List<Vector3Int> { tower });
            }
        }
        return clusters;
    }

    public static List<Vector3Int> FindConvexHull(List<Vector3Int> points)
    {
        if (points.Count < 3) return points;

        // Finding the leftmost point
        Vector3Int leftMost = points[0];
        foreach (var p in points)
        {
            if (p.x < leftMost.x) leftMost = p;
        }

        // Gift wrapping algorithm (Jarvis March)
        List<Vector3Int> hull = new List<Vector3Int>();
        Vector3Int current = leftMost;
        Vector3Int next;
        do
        {
            hull.Add(current);
            next = points[0];
            foreach (var p in points)
            {
                // If p is to the left of the line from current to next, update next
                int crossProduct = (next.x - current.x) * (p.z - current.z) - (next.z - current.z) * (p.x - current.x);
                if (crossProduct > 0 || (crossProduct == 0 && Vector3Int.Distance(current, p) > Vector3Int.Distance(current, next)))
                {
                    next = p;
                }
            }
            current = next;
        } while (current != leftMost);

        return hull;
    }



    public static List<Vector3Int> ScanTerrainForTowerLocations(Substance[,,] terrain, int floorValue, float scale, float heightScale, int maxTowerCount, float minDistance)
    {
        towerLocations = new List<Vector3Int>();

        int width = terrain.GetLength(0);
        int height = terrain.GetLength(1);
        int depth = terrain.GetLength(2);

        int towerThreshold = (int)(0.9f * height);
        int towerCount = 0;

        System.Random random = new System.Random(); // Create a random number generator

        while (towerCount < maxTowerCount)
        {
            int x = random.Next(width);
            int z = random.Next(depth);

            int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, z * scale) * heightScale);
            terrainHeight += floorValue;

            bool isValidTowerSpot = true;

            for (int i = 0; i < towerLocations.Count; i++)
            {
                Vector3Int existingTower = towerLocations[i];

                // Calculate the distance between the existing tower and the potential tower location
                float distance = Mathf.Sqrt((x - existingTower.x) * (x - existingTower.x) + (z - existingTower.z) * (z - existingTower.z));

                if (distance < minDistance)
                {
                    isValidTowerSpot = false;
                    break;
                }
            }

            if (isValidTowerSpot)
            {
                towerLocations.Add(new Vector3Int(x, terrainHeight, z));
                towerCount++;
            }
        }

        return towerLocations;
    }



    public static int GenerateTowerAtLocation(Substance[,,] terrain, Vector3Int location)
    {
        //int towerWidth = 5, towerHeight = 36, towerDepth = 5;
        //int doorHeight = 4, doorWidth = 2;
        int posX = location.x, posY = location.y, posZ = location.z;

        // Create the tower and its foundation
        GenerateTower(terrain, posX, posY, posZ, towerWidth, towerHeight, towerDepth, doorHeight, doorWidth);
        GenerateTowerFoundation(terrain, posX, posY, posZ, towerWidth, towerDepth, 8);

        return towerHeight;
    }

    public static void GenerateTower(Substance[,,] terrain, int posX, int posY, int posZ, int towerWidth, int towerHeight, int towerDepth, int doorHeight, int doorWidth)
    {
        for (int x = posX; x < posX + towerWidth; x++)
        {
            for (int z = posZ; z < posZ + towerDepth; z++)
            {
                for (int y = posY; y < posY + towerHeight; y++)
                {
                    if (x >= 0 && x < terrain.GetLength(0) && y >= 0 && y < terrain.GetLength(1) && z >= 0 && z < terrain.GetLength(2))
                    {
                        if (x == posX || x == posX + towerWidth - 1 || z == posZ || z == posZ + towerDepth - 1 || y == posY || y == posY + towerHeight - 1)
                        {
                            terrain[x, y, z] = Substance.stone;
                        }
                        else
                        {
                            terrain[x, y, z] = Substance.air;
                        }

                        if (x >= posX + towerWidth / 2 - doorWidth / 2 && x < posX + towerWidth / 2 + doorWidth / 2 && z == posZ && y < posY + doorHeight && y >= posY)
                        {
                            terrain[x, y, z] = Substance.air;
                        }
                    }
                }
            }
        }
    }

    public static void GenerateTowerFoundation(Substance[,,] terrain, int baseX, int baseY, int baseZ, int towerWidth, int towerDepth, int maxFoundationWidth)
    {
        int y = baseY - 1; // start just below the tower
        bool hitSolidGround = false;

        while (y >= 0 && !hitSolidGround)
        {
            hitSolidGround = true; // Assume this layer will hit the ground until proven otherwise

            int startX = Mathf.Max(baseX, 0);
            int endX = Mathf.Min(baseX + towerWidth - 1, terrain.GetLength(0) - 1);
            int startZ = Mathf.Max(baseZ, 0);
            int endZ = Mathf.Min(baseZ + towerDepth - 1, terrain.GetLength(2) - 1);

            for (int x = startX; x <= endX; x++)
            {
                for (int z = startZ; z <= endZ; z++)
                {
                    if (terrain[x, y, z] == Substance.air)
                    {
                        terrain[x, y, z] = Substance.stone;
                        hitSolidGround = false; // There's still air here, so not yet at ground
                    }
                }
            }

            // decrement y to move down a layer
            y--;

            // If the pyramid is still within the maximum width, increase the size
            if (towerWidth < maxFoundationWidth)
            {
                baseX--;
                baseZ--;
                towerWidth += 2;
                towerDepth += 2;
            }
        }
    }


    static List<Vector3Int> wallPositions = new List<Vector3Int>();
    public static List<Vector3Int> BuildWallBetweenTowers(Substance[,,] terrain, Vector3Int tower1, Vector3Int tower2, int tower1Height, int tower2Height, int towerWidth, int towerDepth)
    {
        if (tower1 == tower2)
        {
            // If the towers are at the same position, no need to build a wall
            return wallPositions;
        }

        int posY = Mathf.Max(tower1.y + tower1Height, tower2.y + tower2Height);

        Vector3Int diff = tower2 - tower1;
        Vector3Int step = new Vector3Int(diff.x != 0 ? (diff.x > 0 ? 1 : -1) : 0, 0, diff.z != 0 ? (diff.z > 0 ? 1 : -1) : 0);

        int posX = tower1.x, posZ = tower1.z;

        while (!(posX == tower2.x && posZ == tower2.z))
        {
            if (terrain.GetLength(0) <= posX || terrain.GetLength(1) <= posY || terrain.GetLength(2) <= posZ)
                break;

            for (int y = posY; y >= 0; y--)
            {
                /*if (terrain[posX, y, posZ] == Substance.air)
                {
                    terrain[posX, y, posZ] = Substance.stone;
                    wallPositions.Add(new Vector3Int(posX, y, posZ));
                }
                else
                {
                    break;
                }*/
                terrain[posX, y, posZ] = Substance.stone;//build stone wall through any voxels
                wallPositions.Add(new Vector3Int(posX, y, posZ));
            }

            if (posX != tower2.x)
                posX += step.x;
            if (posZ != tower2.z)
                posZ += step.z;
        }

        return wallPositions;
    }

    public static List<(Vector3Int, Vector3Int)> GenerateMinimumSpanningTree(List<Vector3Int> towerLocations)
    {
        List<(Vector3Int, Vector3Int)> mstEdges = new List<(Vector3Int, Vector3Int)>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        if (towerLocations.Count > 0)
        {
            Vector3Int current = towerLocations[0];
            visited.Add(current);

            while (visited.Count < towerLocations.Count)
            {
                float minDistance = float.MaxValue;
                Vector3Int nearestNeighbor = default;
                Vector3Int nearestVisited = default;

                foreach (Vector3Int tower in towerLocations)
                {
                    if (visited.Contains(tower))
                    {
                        continue;
                    }

                    foreach (Vector3Int visitedTower in visited)
                    {
                        float distance = Vector3Int.Distance(visitedTower, tower);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestNeighbor = tower;
                            nearestVisited = visitedTower;
                        }
                    }
                }

                mstEdges.Add((nearestVisited, nearestNeighbor));
                visited.Add(nearestNeighbor);
            }
        }

        return mstEdges;
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
                int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, z * scale) * heightScale);
                terrainHeight += floorValue;

                bool invalidTreeSpot = CheckInvalidTreeSpot(x, z, terrain, terrainHeight, floorValue);
                Vector3Int treePos = new Vector3Int(x, terrainHeight, z);
                int minDistance = 8;
                if (random.NextDouble() < treeProbability && !invalidTreeSpot && !IsTooCloseToTowerOrWall(treePos, towerLocations,wallPositions, minDistance))
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



}

