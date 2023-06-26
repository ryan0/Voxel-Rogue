using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TownData
{
    public Vector3Int ClusterCenter { get; set; }
    public List<HouseData> Houses { get; set; }
    public List<Vector3Int> WallGates { get; set; }
    public HashSet<Vector3Int> TowerLocs { get; set; }
    public List<Vector3Int> WallData { get; set; }

    public TownData(Vector3Int clusterCenter, List<HouseData> houses, List<Vector3Int> wallGates, HashSet<Vector3Int> towers, List<Vector3Int> WalLData)
    {
        ClusterCenter = clusterCenter;
        Houses = houses;
        WallGates = wallGates;
        TowerLocs = towers;
        WallData = WallData;
    }
}

//generate houses
public enum HouseType
{
    house,
    store,
    mayor
}
public class HouseData
{
    public Vector3Int Position;
    public int Width;
    public int Depth;
    public int Height;
    public HouseType houseType;
    // Add more properties as needed for NPCs, stores, etc.
}

public class TownGeneration
{
    /// <summary>
    /// Tower generation and tower fields
    /// </summary>
    static int towerWidth = 5, towerHeight = 18, towerDepth = 5;
    static int doorHeight = 4, doorWidth = 2;
    static int minimumDistance = 12;//minimum distance between towers
    // Gate dimensions
    static int gateHeight = 8, gateWidth = 12;
    static List<Vector3Int> towerLocations;

   
    public static List<TownData> WorldTownsData = new List<TownData>();
    public static List<Vector3Int> towerLocs;
    public static List<Vector3Int> wallPositionsAll = new List<Vector3Int>();

    public void GenerateTowers(Substance[,,] terrain, int[,] terrainHeights, int floorValue, float scale, float heightScale, int maxTowerCount)
    {
        towerLocs = ScanTerrainForTowerLocations(terrain, floorValue, scale, heightScale, maxTowerCount, minimumDistance);
        List<int> towerHeights = new List<int>();

        // Clustering
        List<List<Vector3Int>> clusters = FindClusters(towerLocs, minimumDistance * 5);//Cluster radius is minimumDistance * X

        // Building walls around clusters
        foreach (List<Vector3Int> cluster in clusters)
        {
            List<Vector3Int> convexHull = FindConvexHull(cluster);
            if (convexHull.Count > 3)
            {
                Vector3Int clusterCenter = CalculateClusterCenter(convexHull);

                int roadWidth = 2;// Width of the roads.
                int lotSize = 12; // Size of the lots.
                int minTowerTop = towerHeight + floorValue + 8;
                int averageHeight;
                int townDensity = 70; // Percentage (0-100) of town density.
                List<HouseData> houses = CheckHouses(terrain, convexHull, floorValue, floorValue, roadWidth, lotSize, townDensity);//averageheihgt set to floorvalue
                if (houses.Count < 4)
                {
                    continue;
                }
                // Flatten terrain within cluster
                averageHeight = FlattenTerrainInsideTown(terrain, terrainHeights, convexHull, floorValue, Substance.dirt, 10);
                //lay grid of roads
                LayGridOfRoads(terrain, convexHull, floorValue, averageHeight, roadWidth, lotSize);
                // Lay the houses
                LayHouses(terrain, convexHull, floorValue, averageHeight, roadWidth, lotSize, townDensity);
                // If less than 4 houses can be placed, skip generating walls for this town.

                // The 'houses' list now contains data for the houses that were created.
                // You can use this data for NPCs, store locations, etc.

                List<Vector3Int> allGatesPositions = new List<Vector3Int>();
                List<Vector3Int> wallPositionsTown = new List<Vector3Int>();

                HashSet<Vector3Int> uniqueTowerPositions = new HashSet<Vector3Int>(); // Use HashSet to avoid duplication

                for (int i = 0; i < convexHull.Count; i++)//for each polygon
                {
                    Vector3Int start = convexHull[i];
                    Vector3Int end = convexHull[(i + 1) % convexHull.Count];
                    int startIndex = towerLocs.IndexOf(start);
                    int endIndex = towerLocs.IndexOf(end);
                    int startHeight = CalculateTowerAtLocation(terrain, start, clusterCenter, averageHeight, minTowerTop);
                    int endHeight = CalculateTowerAtLocation(terrain, end, clusterCenter, averageHeight, minTowerTop);
                    towerHeights.Add(startHeight);
                    towerHeights.Add(endHeight);
                    //build the wall, add the walls within the town to a town wall position list
                    wallPositionsTown.Concat(BuildWallBetweenTowers(terrain, start, end, startHeight, endHeight, towerWidth, towerDepth, clusterCenter, averageHeight));
                    // Get the gate positions and add them to allGatesPositions
                    List<Vector3Int> gatePositions = GetWallGates(start, end, clusterCenter, towerWidth, towerDepth);
                    allGatesPositions.AddRange(gatePositions);
                    // Generate towers
                    GenerateTowerAtLocation(terrain, start, clusterCenter, averageHeight, minTowerTop);
                    GenerateTowerAtLocation(terrain, end, clusterCenter, averageHeight, minTowerTop);
                    // Add the positions to HashSet to ensure uniqueness
                    uniqueTowerPositions.Add(start);
                    uniqueTowerPositions.Add(end);
                }
                wallPositionsAll.Concat(wallPositionsTown);
                // Store the town data in the global list
                WorldTownsData.Add(new TownData(clusterCenter, houses, allGatesPositions, uniqueTowerPositions, wallPositionsTown));
            }
            else
            {
                Vector3Int clusterCenter = CalculateClusterCenter(convexHull);

                int roadWidth = 2;// Width of the roads.
                int lotSize = 12; // Size of the lots.
                int minTowerTop = towerHeight + floorValue + 8;
                int averageHeight;
                int townDensity = 70; // Percentage (0-100) of town density.
                                      // Flatten terrain within cluster
                                      //draw the lone towers
                for (int i = 0; i < convexHull.Count; i++)
                {
                    Vector3Int start = convexHull[i];
                    Vector3Int end = convexHull[(i + 1) % convexHull.Count];
                    // Generate towers
                    GenerateTowerAtLocation(terrain, start, clusterCenter, floorValue, minTowerTop);
                    GenerateTowerAtLocation(terrain, end, clusterCenter, floorValue, minTowerTop);
                }
            }

            // Within your existing GenerateTowers method or somewhere appropriate:
            foreach (var townA in WorldTownsData)
            {
                TownData closestTown = null;
                Vector3Int[] closestGates = null;
                float minDistance = float.MaxValue;

                foreach (var townB in WorldTownsData)
                {
                    if (townA != townB)
                    {
                        Vector3Int[] gates = FindClosestGates(townA, townB);
                        float distance = Vector3.Distance(gates[0], gates[1]);
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            closestGates = gates;
                            closestTown = townB;
                        }
                    }
                }

                if (closestTown != null)
                {
                    GenerateRoadBetweenGates(terrain, closestGates[0], closestGates[1], gateWidth, terrainHeights);
                }
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

    public static List<HouseData> CheckHouses(Substance[,,] terrain, List<Vector3Int> convexHull, int floorValue, int averageHeight, int roadWidth, int lotSize, int townDensity)
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

    public static int FlattenTerrainInsideTown(Substance[,,] terrain, int[,] terrainHeights, List<Vector3Int> convexHull, int floorValue, Substance foundationType, int padding)
    {
        int minX = Mathf.Max(0, convexHull.Min(p => p.x) - padding);
        int maxX = Mathf.Min(terrain.GetLength(0) - 1, convexHull.Max(p => p.x) + padding);
        int minZ = Mathf.Max(0, convexHull.Min(p => p.z) - padding);
        int maxZ = Mathf.Min(terrain.GetLength(2) - 1, convexHull.Max(p => p.z) + padding);

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
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                // Extend the flattening around the town by including the padding area
                if (IsPointInPolygonWithExtension(new Vector3Int(x, 0, z), convexHull, padding))
                {
                    // Set foundation type up to the average height
                    terrain[x, averageHeight, z] = foundationType;
                    //terrainHeights[x, z] = averageHeight;
                    WorldGeneration.terrainHeights[x, z] = averageHeight;

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

    public static bool IsPointInPolygonWithExtension(Vector3Int point, List<Vector3Int> convexHull, int padding)
    {
        // First check if the point is inside the polygon
        if (IsPointInPolygon(point, convexHull))
        {
            return true;
        }

        // Check if the point is within the padding area around the polygon
        for (int i = 0; i < convexHull.Count; i++)
        {
            Vector3Int vertex1 = convexHull[i];
            Vector3Int vertex2 = convexHull[(i + 1) % convexHull.Count]; // Next vertex, wrap around to 0 for the last one

            float distance = DistancePointToLineSegment(new Vector3(point.x, 0, point.z), new Vector3(vertex1.x, 0, vertex1.z), new Vector3(vertex2.x, 0, vertex2.z));
            if (distance <= padding)
            {
                return true;
            }
        }

        return false;
    }

    private static float DistancePointToLineSegment(Vector3 point, Vector3 vertex1, Vector3 vertex2)
    {
        Vector3 line = vertex2 - vertex1;
        Vector3 pointToVertex1 = point - vertex1;

        float t = Mathf.Max(0, Mathf.Min(1, Vector3.Dot(pointToVertex1, line) / line.sqrMagnitude));
        Vector3 projection = vertex1 + t * line; // This is the projection of the point onto the line segment

        return Vector3.Distance(point, projection);
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
        int towerMaxHeight = Mathf.Max(posY + towerHeight, minTowerTop);

        Vector3Int defaultDoorDirection = new Vector3Int(1, 0, 0); // or any direction you want as default
        if (doorDirection == new Vector3Int(0, 0, 0))
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
                    if (y >= averageHeight && y <= Mathf.Max(averageHeight + doorHeight, posY + doorHeight))
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


    
    public static List<Vector3Int> BuildWallBetweenTowers(Substance[,,] terrain, Vector3Int tower1, Vector3Int tower2, int tower1Height, int tower2Height, int towerWidth, int towerDepth, Vector3Int clusterCenter, int averageHeight)
    {
        if (tower1 == tower2)
        {
            // If the towers are at the same position, no need to build a wall
            return new List<Vector3Int>();
        }

        // Gate dimensions (assuming you want to use these for the gate area)
        int gateWidth = 12;
        int gateHeight = 8;

        // Calculate the unit vectors pointing from the cluster center to each tower
        Vector3 dirToTower1 = ((Vector3)(tower1 - clusterCenter)).normalized;
        Vector3 dirToTower2 = ((Vector3)(tower2 - clusterCenter)).normalized;

        // Calculate offsets using the unit vectors
        Vector3Int offset1 = new Vector3Int(Mathf.RoundToInt(dirToTower1.x * towerWidth), 0, Mathf.RoundToInt(dirToTower1.z * towerDepth));
        Vector3Int offset2 = new Vector3Int(Mathf.RoundToInt(dirToTower2.x * towerWidth), 0, Mathf.RoundToInt(dirToTower2.z * towerDepth));

        // Start and end positions of the wall
        Vector3Int start = tower1 + offset1;
        Vector3Int end = tower2 + offset2;

        // Calculate the center position of the wall
        Vector3Int centerPosition = (start + end) / 2;

        int totalSteps = Mathf.Max(Mathf.Abs(end.x - start.x), Mathf.Abs(end.z - start.z));

        // Initialize the list to keep track of wall positions
        List<Vector3Int> wallPositions = new List<Vector3Int>();

        // Loop through the positions to build the wall
        for (int i = 0; i <= totalSteps; i++)
        {
            float t = (float)i / totalSteps;

            int posX = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int posZ = Mathf.RoundToInt(Mathf.Lerp(start.z, end.z, t));
            int posY = Mathf.RoundToInt(Mathf.Lerp(start.y + tower1Height, end.y + tower2Height, t)) - 4;

            // Check if indices are within valid range before accessing the array
            if (posX < 0 || posX >= terrain.GetLength(0) || posY < 0 || posY >= terrain.GetLength(1) || posZ < 0 || posZ >= terrain.GetLength(2))
                continue;

            // Check if current position is within the gate area in terms of x/z
            bool isWithinGateArea = (i >= totalSteps / 2 - gateWidth / 2) && (i <= totalSteps / 2 + gateWidth / 2);

            for (int y = 0; y <= posY; y++)
            {
                Vector3Int currentPosition = new Vector3Int(posX, y, posZ);

                // If we are within the gate area and not below the base, set to air (empty space for gate)
                if (isWithinGateArea && y < gateHeight + averageHeight)
                {
                    terrain[posX, y, posZ] = Substance.air;
                }
                else // Otherwise, build the wall
                {
                    terrain[posX, y, posZ] = Substance.stone;
                    // Add the current position to wallPositions
                    wallPositions.Add(currentPosition);
                }
            }
        }

        return wallPositions;
    }



    public static List<Vector3Int> GetWallGates(Vector3Int tower1, Vector3Int tower2, Vector3Int clusterCenter, int towerWidth, int towerDepth)
    {
        List<Vector3Int> gatePositions = new List<Vector3Int>();

        // Gate dimensions
        //int gateHeight = 8;
        //int gateWidth = 12;

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

        // Loop through the positions to find the gate positions
        for (int i = 0; i <= totalSteps; i++)
        {
            float t = (float)i / totalSteps;

            int posX = Mathf.RoundToInt(Mathf.Lerp(start.x, end.x, t));
            int posZ = Mathf.RoundToInt(Mathf.Lerp(start.z, end.z, t));

            // Check if current position is within the gate area in terms of x/z
            bool isWithinGateArea = (i >= totalSteps / 2 - gateWidth / 2) && (i <= totalSteps / 2 + gateWidth / 2);

            if (isWithinGateArea)
            {
                for (int y = 0; y < gateHeight; y++)
                {
                    gatePositions.Add(new Vector3Int(posX, y, posZ));
                }
            }
        }

        return gatePositions;
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


    public Vector3Int[] FindClosestGates(TownData townA, TownData townB)
    {
        Vector3Int closestGateA = Vector3Int.zero;
        Vector3Int closestGateB = Vector3Int.zero;
        float minDistance = float.MaxValue;

        foreach (var gateA in townA.WallGates)
        {
            foreach (var gateB in townB.WallGates)
            {
                float distance = Vector3.Distance(gateA, gateB);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestGateA = gateA;
                    closestGateB = gateB;
                }
            }
        }

        return new Vector3Int[] { closestGateA, closestGateB };
    }

    public void GenerateRoadBetweenGates(Substance[,,] terrain, Vector3Int startGate, Vector3Int endGate, int gateWidth, int[,] terrainHeights)
    {
        int totalSteps = Mathf.Max(Mathf.Abs(endGate.x - startGate.x), Mathf.Abs(endGate.z - startGate.z));

        // Update the heights of the start and end gates based on terrainHeights
        startGate.y = terrainHeights[startGate.x, startGate.z];
        endGate.y = terrainHeights[endGate.x, endGate.z];

        for (int i = 0; i <= totalSteps; i++)
        {
            float t = (float)i / totalSteps;
            int posX = Mathf.RoundToInt(Mathf.Lerp(startGate.x, endGate.x, t));
            int posZ = Mathf.RoundToInt(Mathf.Lerp(startGate.z, endGate.z, t));

            // Linearly interpolate between the starting and ending gate heights
            int centerY = Mathf.RoundToInt(Mathf.Lerp(startGate.y, endGate.y, t));

            // Build road by clearing area of gateWidth
            for (int roadX = posX - gateWidth / 2; roadX <= posX + gateWidth / 2; roadX++)
            {
                for (int roadZ = posZ - gateWidth / 2; roadZ <= posZ + gateWidth / 2; roadZ++)
                {
                    if (roadX >= 0 && roadZ >= 0 && roadX < terrain.GetLength(0) && roadZ < terrain.GetLength(2))
                    {
                        // Fetch the height values for the left and right edges of the road
                        int leftEdgeHeight = terrainHeights[Mathf.Max(roadX - gateWidth / 2, 0), roadZ];
                        int rightEdgeHeight = terrainHeights[Mathf.Min(roadX + gateWidth / 2, terrain.GetLength(0) - 1), roadZ];

                        // Linearly interpolate between the left and right edge heights
                        float edgeLerpFactor = (float)(roadX - posX + gateWidth / 2) / gateWidth;
                        int edgeHeight = Mathf.RoundToInt(Mathf.Lerp(leftEdgeHeight, rightEdgeHeight, edgeLerpFactor));

                        // Damping factor for smoothness (value between 0 and 1)
                        float dampingFactor = 0.8f;
                        int roadY = Mathf.RoundToInt(centerY * dampingFactor + edgeHeight * (1 - dampingFactor));

                        // Set the terrain value to road material
                        terrain[roadX, roadY, roadZ] = Substance.asphalt;

                        // Update terrainHeights array
                        //terrainHeights[roadX, roadZ] = roadY;
                        WorldGeneration.terrainHeights[roadX, roadZ] = roadY;

                        // Clear voxels above the road
                        for (int y = roadY + 1; y < GasFlowSystem.MAX_GAS_HEIGHT; y++)
                        {
                            //Vector3Int currentPosition = new Vector3Int(roadX, y, roadZ);

                            //terrain[roadX, y, roadZ] = Substance.air;
                        }
                    }
                }
            }
        }
    }

}
