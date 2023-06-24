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
        //Voxel[,,] voxels = new Voxel[width, height, depth];
        System.Random random = new System.Random();

        CalculateTerrainHeights(width, depth, scale, heightScale, floorValue, terrainHeights);

        GenerateTerrainBlocks(width, height, depth, terrainHeights, terrain);

        GenerateWorms(terrain, 3);

        GenerateRivers(floorValue, terrain, terrainHeights, 1, Substance.water);

        GenerateRivers(floorValue, terrain, terrainHeights, 1, Substance.lava, 200);

        GenerateClouds(terrain, 30);

        GenerateTrees(width, depth, scale, heightScale, floorValue, treeProbability, terrain, random);

        int maxTowerCount = 10; // Adjust the value as needed
        GenerateTowers(terrain, terrainHeights, floorValue, scale, heightScale, maxTowerCount);



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
    static int towerWidth = 5, towerHeight = 18, towerDepth = 5;
    static int doorHeight = 4, doorWidth = 2;
    static int minimumDistance = 12;
    static List<Vector3Int> towerLocations;


    public static void GenerateTowers(Substance[,,] terrain, int[,] terrainHeights, int floorValue, float scale, float heightScale, int maxTowerCount)
    {
        List<Vector3Int> towerLocs = ScanTerrainForTowerLocations(terrain, floorValue, scale, heightScale, maxTowerCount, minimumDistance);
        List<int> towerHeights = new List<int>();

        // Clustering
        List<List<Vector3Int>> clusters = FindClusters(towerLocs, minimumDistance * 8);

        // Building walls around clusters
        foreach (List<Vector3Int> cluster in clusters)
        {
            List<Vector3Int> convexHull = FindConvexHull(cluster);
            Vector3Int clusterCenter = CalculateClusterCenter(convexHull);

            int roadWidth = 2;// Width of the roads.
            int lotSize = 12; // Size of the lots.
            int minTowerTop = towerHeight + floorValue;
            int averageHeight;
            int townDensity = 70; // Percentage (0-100) of town density.
            // Flatten terrain within cluster
            averageHeight = FlattenTerrainInsideTown(terrain, terrainHeights, convexHull, floorValue, Substance.dirt);
            //lay grid of roads
            LayGridOfRoads(terrain, convexHull, floorValue, averageHeight, roadWidth, lotSize);
            // Lay the houses
            List<HouseData> houses = LayHouses(terrain, convexHull, floorValue, averageHeight, roadWidth, lotSize, townDensity);
            // The 'houses' list now contains data for the houses that were created.
            // You can use this data for NPCs, store locations, etc.

            for (int i = 0; i < convexHull.Count; i++)
            {
                Vector3Int start = convexHull[i];
                Vector3Int end = convexHull[(i + 1) % convexHull.Count];
                int startIndex = towerLocs.IndexOf(start);
                int endIndex = towerLocs.IndexOf(end);
                int startHeight = CalculateTowerAtLocation(terrain, start, clusterCenter, averageHeight, minTowerTop);
                int endHeight = CalculateTowerAtLocation(terrain, end, clusterCenter, averageHeight, minTowerTop);
                towerHeights.Add(startHeight);
                towerHeights.Add(endHeight);
                BuildWallBetweenTowers(terrain, start, end, startHeight, endHeight, towerWidth, towerDepth, clusterCenter);
                GenerateTowerAtLocation(terrain, start, clusterCenter, averageHeight, minTowerTop);
                GenerateTowerAtLocation(terrain, end, clusterCenter, averageHeight, minTowerTop);
            }
        }
    }

    public static Vector3Int CalculateClusterCenter(List<Vector3Int> cluster)
    {
        Vector3Int sum = Vector3Int.zero;
        foreach (var tower in cluster)
        {
            sum += tower;
        }
        return new Vector3Int(sum.x / cluster.Count, sum.y / cluster.Count, sum.z / cluster.Count);
    }

    //generate houses

    public class HouseData
    {
        public Vector3Int Position;
        public int Width;
        public int Depth;
        public int Height;
        // Add more properties as needed for NPCs, stores, etc.
    }
    public static List<HouseData> LayHouses(Substance[,,] terrain, List<Vector3Int> convexHull, int floorValue, int averageHeight, int roadWidth, int lotSize, int townDensity)
    {
        int minX = convexHull.Min(p => p.x);
        int maxX = convexHull.Max(p => p.x);
        int minZ = convexHull.Min(p => p.z);
        int maxZ = convexHull.Max(p => p.z);

        Vector3Int townCenter = new Vector3Int((minX + maxX) / 2, averageHeight, (minZ + maxZ) / 2);

        System.Random random = new System.Random();
        List<HouseData> houses = new List<HouseData>();

       // int doorWidth = 2;
       // int doorHeight = 3;

        // Iterate through the lots
        for (int x = minX + lotSize + roadWidth; x < maxX; x += lotSize + roadWidth)
        {
            for (int z = minZ + lotSize + roadWidth; z <= maxZ; z += lotSize + roadWidth)
            {
                // Check if house should be placed based on town density
                if (random.Next(0, 100) >= townDensity)
                    continue;

                // Check if the lot is inside the convex hull
                bool isSquareLot = true;
                for (int lotX = x; lotX < x + lotSize; lotX++)
                {
                    for (int lotZ = z; lotZ < z + lotSize; lotZ++)
                    {
                        if (!IsPointInPolygon(new Vector3Int(lotX, 0, lotZ), convexHull))
                        {
                            isSquareLot = false;
                            break;
                        }
                    }
                    if (!isSquareLot) break;
                }
                if (!isSquareLot) continue;

                // Randomly decide the house dimensions
                int houseWidth = random.Next(5, lotSize - 1);
                int houseDepth = random.Next(5, lotSize - 1);
                int houseHeight = random.Next(5, 8);

                // Lay house base
                for (int hx = x; hx < x + houseWidth; hx++)
                {
                    for (int hz = z; hz < z + houseDepth; hz++)
                    {
                        for (int hy = averageHeight + 1; hy < averageHeight + houseHeight; hy++)
                        {
                            if (hx == x || hx == x + houseWidth - 1 || hz == z || hz == z + houseDepth - 1)
                            {
                                // Place windows every 4 blocks height
                                if ((hy - averageHeight) % 3 == 0 && hy < averageHeight + houseHeight && random.Next(0, 100) < 50)
                                {
                                    terrain[hx, hy, hz] = Substance.glass;
                                }
                                else
                                {
                                    terrain[hx, hy, hz] = Substance.stone;
                                }
                            }
                        }
                    }
                }

                // Lay sloped wooden roof
                int roofBaseHeight = averageHeight + houseHeight;
                for (int rx = x; rx < x + houseWidth; rx++)
                {
                    for (int rz = z; rz < z + houseDepth; rz++)
                    {
                        int slopeHeight = houseWidth / 2 - Mathf.Abs(rx - (x + houseWidth / 2));
                        for (int rh = 0; rh <= slopeHeight; rh++)
                        {
                            terrain[rx, roofBaseHeight + rh, rz] = Substance.wood;
                        }
                    }
                }


                // Place door that faces the town center
                int doorX, doorZ;
                if (Mathf.Abs(townCenter.x - x) < Mathf.Abs(townCenter.z - z))
                {
                    doorX = x + houseWidth / 2;
                    doorZ = (townCenter.z <= z) ? z : z + houseDepth - 1;
                }
                else
                {
                    doorX = (townCenter.x <= x) ? x : x + houseWidth - 1;
                    doorZ = z + houseDepth / 2;
                }
                for (int dx = 0; dx < doorWidth; dx++)
                {
                    for (int dy = 0; dy < doorHeight; dy++)
                    {
                        terrain[doorX + dx, averageHeight + 1 + dy, doorZ] = Substance.air;
                    }
                }

                // Store house data for future use
                houses.Add(new HouseData
                {
                    Position = new Vector3Int(x, averageHeight + 1, z),
                    Width = houseWidth,
                    Depth = houseDepth,
                    Height = houseHeight
                });
            }
        }

        return houses;
    }

    //Find clusters within minimum distnace
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

    public static int FlattenTerrainInsideTown(Substance[,,] terrain, int[,] terrainHeights, List<Vector3Int> convexHull, int floorValue, Substance foundationType)
    {
        int minX = convexHull.Min(p => p.x);
        int maxX = convexHull.Max(p => p.x);
        int minZ = convexHull.Min(p => p.z);
        int maxZ = convexHull.Max(p => p.z);

        int totalHeight = 0;
        int count = 0;
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (IsPointInPolygon(new Vector3Int(x, 0, z), convexHull))
                {
                    totalHeight += terrainHeights[x, z];
                    count++;
                }
            }
        }

        // Check if count is greater than zero to avoid division by zero
        int averageHeight = (count > 0) ? totalHeight / count : floorValue;


        // Flatten terrain and make it the foundation type
        for (int x = minX; x <= maxX; x++)//ADD 2 to avoid off by one issues
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (IsPointInPolygon(new Vector3Int(x, 0, z), convexHull))
                {
                    // Set foundation type up to the average height
                    //for (int y = floorValue; y <= averageHeight; y++)
                    {
                        terrain[x, averageHeight, z] = foundationType;
                        terrainHeights[x, z] = averageHeight;
                    }

                    // Remove stuff above the average height but not cloud layer
                    for (int y = averageHeight + 1; y < GasFlowSystem.MAX_GAS_HEIGHT; y++)
                    {
                        terrain[x, y, z] = Substance.air;
                    }
                }
            }
        }

        return averageHeight;
    }


    public static bool IsPointInPolygon(Vector3Int point, List<Vector3Int> polygon)
    {
        int count = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector3Int a = polygon[i];
            Vector3Int b = polygon[(i + 1) % polygon.Count];

            if ((a.z <= point.z && b.z > point.z) || (b.z <= point.z && a.z > point.z))
            {
                float t = (float)(point.z - a.z) / (b.z - a.z);
                if (a.x + t * (b.x - a.x) < point.x)
                {
                    count++;
                }
            }
        }
        return count % 2 == 1;
    }

    public static void LayGridOfRoads(Substance[,,] terrain, List<Vector3Int> convexHull, int floorValue, int averageHeight, int roadWidth, int lotSize)
    {
        int minX = convexHull.Min(p => p.x);
        int maxX = convexHull.Max(p => p.x);
        int minZ = convexHull.Min(p => p.z);
        int maxZ = convexHull.Max(p => p.z);

        List<Vector3Int> hull2D = convexHull.Select(p => new Vector3Int(p.x, 0, p.z)).ToList();

        for (int x = minX + lotSize; x < maxX; x += lotSize + roadWidth)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (!IsPointInPolygon(new Vector3Int(x, 0, z), hull2D))
                {
                    continue;
                }

                for (int w = 0; w < roadWidth; w++)
                {
                    int currentX = x + w;
                    if (currentX >= terrain.GetLength(0)) continue;

                    // Only the top level should be asphalt
                    terrain[currentX, averageHeight, z] = Substance.asphalt;
                }
            }
        }

        for (int z = minZ + lotSize; z < maxZ; z += lotSize + roadWidth)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (!IsPointInPolygon(new Vector3Int(x, 0, z), hull2D))
                {
                    continue;
                }

                for (int w = 0; w < roadWidth; w++)
                {
                    int currentZ = z + w;
                    if (currentZ >= terrain.GetLength(2)) continue;

                    // Only the top level should be asphalt
                    terrain[x, averageHeight, currentZ] = Substance.asphalt;
                }
            }
        }
    }


    //Towers must be separated by minimum distance
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


    public static int CalculateTowerAtLocation(Substance[,,] terrain, Vector3Int location, Vector3Int clusterCenter, int averageHeight, int minTowerTop)
    {
        int posX = location.x, posY = location.y, posZ = location.z;

        // Calculate door direction
        Vector3Int doorDirection = clusterCenter - location;
        if (doorDirection.x != 0) doorDirection.x = doorDirection.x > 0 ? 1 : -1;
        if (doorDirection.z != 0) doorDirection.z = doorDirection.z > 0 ? 1 : -1;

        // Create the tower and its foundation
        //GenerateTower(terrain, posX, posY, posZ, towerWidth, towerHeight, towerDepth, doorHeight, doorWidth, doorDirection);
        //GenerateTowerFoundation(terrain, posX, posY, posZ, towerWidth, towerDepth, 8);

        return Mathf.Max(posY + towerHeight, minTowerTop) - posY;

    }

    public static void GenerateTowerAtLocation(Substance[,,] terrain, Vector3Int location, Vector3Int clusterCenter, int averageHeight, int minTowerTop)
    {
        int posX = location.x, posY = location.y, posZ = location.z;

        // Calculate door direction
        Vector3Int doorDirection = clusterCenter - location;
        if (doorDirection.x != 0) doorDirection.x = doorDirection.x > 0 ? 1 : -1;
        if (doorDirection.z != 0) doorDirection.z = doorDirection.z > 0 ? 1 : -1;

        // Create the tower and its foundation
        GenerateTower(terrain, posX, posY, posZ, towerWidth, towerHeight, towerDepth, doorHeight, doorWidth, doorDirection, averageHeight, minTowerTop);
        GenerateTowerFoundation(terrain, posX, posY, posZ, towerWidth, towerDepth, 8, doorDirection);

        //return towerHeight;
    }

    public static void GenerateTower(Substance[,,] terrain, int posX, int posY, int posZ, int towerWidth, int towerHeight, int towerDepth, int doorHeight, int doorWidth, Vector3Int doorDirection, int averageHeight, int minTowerTop)
    {
        int maxX = terrain.GetLength(0);
        int maxY = terrain.GetLength(1);
        int maxZ = terrain.GetLength(2);

        // Ensure towerHeight is at least as tall as minTowerTop
        int towerMaxHeight = Mathf.Max(posY + towerHeight, minTowerTop );

        Vector3Int defaultDoorDirection = new Vector3Int(1, 0, 0); // or any direction you want as default
        if(doorDirection == new Vector3Int(0,0,0))
        {
            doorDirection = defaultDoorDirection;
        }
        // Adjust the starting position based on the door direction
        if (doorDirection.x < 0)
        {
            posX -= towerWidth - 1;
        }

        if (doorDirection.z < 0)
        {
            posZ -= towerDepth - 1;
        }

        // Ensure the tower does not exceed the bounds of the terrain array
        if (posX < 0 || posX + towerWidth >= maxX ||
            posY < 0 || towerMaxHeight >= maxY ||
            posZ < 0 || posZ + towerDepth >= maxZ)
        {
            Debug.LogWarning("Tower cannot be generated as it exceeds the bounds of the terrain array.");
            return;
        }

        // Build the tower
        for (int x = posX; x < posX + towerWidth; x++)
        {
            for (int z = posZ; z < posZ + towerDepth; z++)
            {
                for (int y = posY; y < towerMaxHeight; y++)
                {
                    bool isDoor = false;
                    //max height ends at max of average height+doorHeight and posY+doorHeight
                    // Calculate if the position is a door or inside the tower
                    if (y >= averageHeight && y <= Mathf.Max(averageHeight + doorHeight, posY+doorHeight))
                    {
                        if (doorDirection.x < 0 && x == posX && z >= posZ + (towerDepth / 2) - (doorWidth / 2) && z < posZ + (towerDepth / 2) + (doorWidth / 2))
                            isDoor = true;
                        if (doorDirection.x > 0 && x == posX + towerWidth - 1 && z >= posZ + (towerDepth / 2) - (doorWidth / 2) && z < posZ + (towerDepth / 2) + (doorWidth / 2))
                            isDoor = true;
                        if (doorDirection.z < 0 && z == posZ && x >= posX + (towerWidth / 2) - (doorWidth / 2) && x < posX + (towerWidth / 2) + (doorWidth / 2))
                            isDoor = true;
                        if (doorDirection.z > 0 && z == posZ + towerDepth - 1 && x >= posX + (towerWidth / 2) - (doorWidth / 2) && x < posX + (towerWidth / 2) + (doorWidth / 2))
                            isDoor = true;
                    }

                    // Calculate if the position is a wall of the tower
                    bool isWall = x == posX || x == posX + towerWidth - 1 || z == posZ || z == posZ + towerDepth - 1 || y == posY || y == towerMaxHeight - 1;

                    if (isDoor)
                    {
                        // Make the door
                        terrain[x, y, z] = Substance.air;
                    }
                    else if (isWall)
                    {
                        // Make the walls
                        terrain[x, y, z] = Substance.asphalt;
                    }
                    else
                    {
                        // Leave the inside hollow
                        terrain[x, y, z] = Substance.air;
                    }
                }
            }
        }
    }


    public static void GenerateTowerFoundation(Substance[,,] terrain, int baseX, int baseY, int baseZ, int towerWidth, int towerDepth, int maxFoundationWidth, Vector3Int doorDirection)
    {

        // Align the foundation to be centered below the tower base based on doorDirection
        if (doorDirection.x < 0)
        {
            baseX -= towerWidth - 1;
        }

        if (doorDirection.z < 0)
        {
            baseZ -= towerDepth - 1;
        }

        int y = baseY - 1; // start just below the tower
        bool hitSolidGround = false;

        // Align the foundation to be centered below the tower base.
        //baseX -= towerWidth / 2;
        //baseZ -= towerDepth / 2;

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


    static List<Vector3Int>  wallPositions = new List<Vector3Int>();

    public static List<Vector3Int> BuildWallBetweenTowers(Substance[,,] terrain, Vector3Int tower1, Vector3Int tower2, int tower1Height, int tower2Height, int towerWidth, int towerDepth, Vector3Int clusterCenter)
    {
        if (tower1 == tower2)
        {
            // If the towers are at the same position, no need to build a wall
            return new List<Vector3Int>();
        }
        // Calculate the unit vectors pointing from the cluster center to each tower
        Vector3 dirToTower1 = ((Vector3)(tower1 - clusterCenter)).normalized;
        Vector3 dirToTower2 = ((Vector3)(tower2 - clusterCenter)).normalized;

        // Calculate offsets using the unit vectors
        Vector3Int offset1 = new Vector3Int(Mathf.RoundToInt(dirToTower1.x * towerWidth), 0, Mathf.RoundToInt(dirToTower1.z * towerDepth));
        Vector3Int offset2 = new Vector3Int(Mathf.RoundToInt(dirToTower2.x * towerWidth), 0, Mathf.RoundToInt(dirToTower2.z * towerDepth));

        // Start and end positions of the wall
        Vector3Int start = tower1 + offset1;
        Vector3Int end = tower2 + offset2;

        int totalSteps = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.z - start.z));

        for (int i = 0; i <= totalSteps; i++)
        {
            float t = (float)i / totalSteps;

            int posX = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int posZ = Mathf.RoundToInt(Mathf.Lerp(start.z, end.z, t));
            int posY = Mathf.RoundToInt(Mathf.Lerp(start.y + tower1Height, end.y + tower2Height, t)) - 4;

            // Check if indices are within valid range before accessing the array
            if (posX < 0 || posX >= terrain.GetLength(0) || posY < 0 || posY >= terrain.GetLength(1) || posZ < 0 || posZ >= terrain.GetLength(2))
                continue;

            for (int y = posY; y >= 0; y--)
            {
                terrain[posX, y, posZ] = Substance.stone; //build stone wall through any voxels

                // Rest of the code
            }
        }

        return wallPositions; // Make sure wallPositions is defined and updated in your function
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
                if (random.NextDouble() < treeProbability && !invalidTreeSpot)// && !IsTooCloseToTowerOrWall(treePos, towerLocations,wallPositions, minDistance))
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

