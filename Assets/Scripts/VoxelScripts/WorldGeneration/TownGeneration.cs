using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TownData
{
    public Vector3Int ClusterCenter { get; set; }
    public List<HouseData> Houses { get; set; }
    public List<RectInt> lots;
    public List<Vector3Int> roadPositions;
    public List<Vector3Int> towerPositions;
    public List<Vector3Int> gatePositions;
    public TownType type;
    public TownData(Vector3Int _clusterCenter, List<HouseData> _houses, 
    List<RectInt> _lots, List<Vector3Int> _roadPositions, List<Vector3Int> _towerPositions, List<Vector3Int> _gatePositions, TownType _type = TownType.village)
    {
        ClusterCenter = _clusterCenter;
        Houses = _houses;
        lots = _lots;
        roadPositions = _roadPositions;
        towerPositions = _towerPositions;
        gatePositions = _gatePositions;
        type = _type;

    }
}

public enum TownType
{
    village,
    city
}


public class TownGeneration
{

    public static List<TownData> worldTownsData;
    private Substance[,,] terrain;
    private int chunkSize;
    private int townRadius;
    private float townProbability;
    Geometry geo;
    public int[,] terrainHeights;
    public TownGeneration(Substance[,,] terrain, int chunkSize, int townRadius, float townProbability)
    {
        this.terrain = terrain;
        this.chunkSize = chunkSize;
        this.townRadius = townRadius;
        this.townProbability = townProbability;
        this.terrainHeights = WorldGeneration.terrainHeights;
        geo = new Geometry();
        worldTownsData = new List<TownData>();

    }

    public void GenerateTowns()
    {
        // Custom town grid size
        int townGridSize = 64; // You can adjust this value

        // Size of the terrain in town grid units
        int terrainWidthInTownGridUnits = terrain.GetLength(0) / townGridSize;
        int terrainDepthInTownGridUnits = terrain.GetLength(2) / townGridSize;

        bool[,] townGrid = new bool[terrainWidthInTownGridUnits, terrainDepthInTownGridUnits];

        System.Random random = new System.Random();

        // Decide where the towns will be
        for (int x = 0; x < terrainWidthInTownGridUnits; x++)
        {
            for (int z = 0; z < terrainDepthInTownGridUnits; z++)
            {
                double rng = random.NextDouble();
                if (rng < townProbability)
                {
                    //Debug.Log(rng);
                    // Check for neighboring towns within townRadius
                    bool canPlaceTown = true;
                    for (int dx = -townRadius; dx <= townRadius; dx++)
                    {
                        for (int dz = -townRadius; dz <= townRadius; dz++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;
                            if (nx >= 0 && nx < terrainWidthInTownGridUnits && nz >= 0 && nz < terrainDepthInTownGridUnits && townGrid[nx, nz])
                            {
                                canPlaceTown = false;
                                break;
                            }
                        }
                        if (!canPlaceTown) break;
                    }

                    // Place town
                    if (canPlaceTown)
                    {
                        Debug.Log("Town" + " " + terrainWidthInTownGridUnits);
                        townGrid[x, z] = true;
                        GenerateTown(x, z, townGridSize);
                    }
                }
            }
        }

        //generate roads
        ConnectTownsUsingRoads(worldTownsData, gateWidth);
  
    }

    private void GenerateTown(int gridCoordX, int gridCoordZ, int gridSize)
    {
        int floorValue = 0; // The value at which the terrain should be flattened
        Substance foundationType = Substance.stone; // The substance to use as the foundation
        int townDensity = 60; // Density of the town (percentage of lots that have houses)

        // Calculate the world coordinates of this chunk
        //int worldX = chunkX * chunkSize;
        //int worldZ = chunkZ * chunkSize;
        int centerX = gridCoordX * gridSize + gridSize / 2;//in world coords
        int centerZ = gridCoordZ * gridSize + gridSize / 2;
    

        Vector3Int townCenter = new Vector3Int(centerX, 0, centerZ);
        // Assuming you have some logic to determine townType
        TownType townType = TownType.village; // Example town type

        (List<RectInt> lots, List<Vector3Int> roadPositions) = CreateLotsSquare(townCenter, floorValue, townType, new List<RectInt>());
        int averageHeight = DrawLots(lots, roadPositions, townCenter);
        HouseGeneration houseGen = new HouseGeneration();
        List<HouseData>  houses = houseGen.LayHouses(terrain, townCenter, averageHeight, lots, roadPositions, townDensity);
        int towerSize = 2;
        (List<Vector3Int> towerPositions, List<Vector3Int> gatePositions) = BuildWallsAroundTown(townCenter, lots, 10, averageHeight, towerSize); // 10 is wall height, 3 is road width, and true means it will build towers.
        TownData townData = new TownData(townCenter, houses, lots, roadPositions, towerPositions, gatePositions);
        worldTownsData.Add(townData);

    }

    static int lotSize = 8;
    static int roadWidth = 4; // Width of the roads
    private (List<RectInt>, List<Vector3Int>) CreateLotsSquare(Vector3Int center, int floorValue, TownType type, List<RectInt> lots)
    {
        int townSize = (type == TownType.village) ? 4 : 10; // example sizes in num of lots
        int halfTownSize = townSize / 2;
        int cellSize = lotSize + roadWidth; // Size of cell (lot + road)

        List<Vector3Int> roadPositions = new List<Vector3Int>(); // List to keep track of road positions

        for (int x = -halfTownSize * cellSize; x < halfTownSize * cellSize + roadWidth; x++)
        {
            for (int z = -halfTownSize * cellSize; z < halfTownSize * cellSize + roadWidth; z++)
            {
                // Convert from town-local coordinates to world coordinates
                int currentX = center.x + x;
                int currentZ = center.z + z;

                // Determine the position within a cell
                int posXInCell = (x + halfTownSize * cellSize) % cellSize;
                int posZInCell = (z + halfTownSize * cellSize) % cellSize;

                // Determine if current position is part of a road
                bool isRoadX = posXInCell < roadWidth;
                bool isRoadZ = posZInCell < roadWidth;

                if (isRoadX || isRoadZ)
                {
                    // This is a road, add position to roadPositions list
                    roadPositions.Add(new Vector3Int(currentX, floorValue, currentZ));
                }
                else
                {
                    // Check if this is the start of a new lot
                    if (posXInCell == roadWidth && posZInCell == roadWidth)
                    {
                        // This is the start of a new lot
                        int lotStartX = currentX;
                        int lotStartZ = currentZ;
                        lots.Add(new RectInt(lotStartX, lotStartZ, lotSize, lotSize)); // Changed lotSize - 1 to lotSize
                    }
                }
            }
        }

        return (lots, roadPositions); // Return both lots and road positions
    }


    private int DrawLots(List<RectInt> lots, List<Vector3Int> roadPositions, Vector3Int townCenter)
    {
        int totalHeight = 0;
        int numEdges = 0;
        int terrainWidth = terrainHeights.GetLength(0);
        int terrainDepth = terrainHeights.GetLength(1);

        // Calculate the average height of the outer bounds
        foreach (RectInt lot in lots)
        {
            for (int x = lot.xMin; x <= lot.xMax; x++)
            {
                if (x >= 0 && x < terrainWidth && lot.yMin >= 0 && lot.yMin < terrainDepth)
                {
                    totalHeight += terrainHeights[x, lot.yMin];
                    numEdges++;
                }

                if (x >= 0 && x < terrainWidth && lot.yMax >= 0 && lot.yMax < terrainDepth)
                {
                    totalHeight += terrainHeights[x, lot.yMax];
                    numEdges++;
                }
            }
            for (int z = lot.yMin + 1; z <= lot.yMax - 1; z++)
            {
                if (lot.xMin >= 0 && lot.xMin < terrainWidth && z >= 0 && z < terrainDepth)
                {
                    totalHeight += terrainHeights[lot.xMin, z];
                    numEdges++;
                }

                if (lot.xMax >= 0 && lot.xMax < terrainWidth && z >= 0 && z < terrainDepth)
                {
                    totalHeight += terrainHeights[lot.xMax, z];
                    numEdges++;
                }
            }
        }

        int averageHeight = (numEdges > 0) ? totalHeight / numEdges : 0;

        int maxCloudHeight = GasFlowSystem.MAX_GAS_HEIGHT;

        // Draw the lots
        foreach (RectInt lot in lots)
        {
            for (int x = lot.xMin; x <= lot.xMax; x++)
            {
                for (int z = lot.yMin; z <= lot.yMax; z++)
                {
                    if (x >= 0 && x < terrainWidth && z >= 0 && z < terrainDepth)
                    {
                        terrainHeights[x, z] = averageHeight;
                        // Set substance to grassSubstance
                        terrain[x, averageHeight, z] = Substance.debug;

                        // Set tiles above to air
                        int terrainHeight = terrain.GetLength(1);
                        for (int y = averageHeight + 1; y < maxCloudHeight && y < terrainHeight; y++)
                        {
                            terrain[x, y, z] = Substance.air;
                        }
                        WorldGeneration.terrainHeights[x, z] = averageHeight;
}
                }
            }
        }

        // Draw the roads
        foreach (Vector3Int roadPos in roadPositions)
        {
            if (roadPos.x >= 0 && roadPos.x < terrainWidth && roadPos.z >= 0 && roadPos.z < terrainDepth)
            {
                // Set to asphalt substance and average height
                terrainHeights[roadPos.x, roadPos.z] = averageHeight;
                terrain[roadPos.x, averageHeight, roadPos.z] = Substance.asphalt;

                // Set tiles above to air
                int terrainHeight = terrain.GetLength(1);
                for (int y = averageHeight + 1; y < maxCloudHeight && y < terrainHeight; y++)
                {
                    terrain[roadPos.x, y, roadPos.z] = Substance.air;
                }
            }
        }
        return averageHeight;
    }

    // Method for flattening the terrain
    public int FlattenTerrainInsideTown(int[,] terrainHeights, List<Vector3Int> convexHull, int floorValue, Substance foundationType, int padding)
    {
        int minX = Mathf.Max(0, convexHull.Min(p => p.x) - padding);
        int maxX = Mathf.Min(this.terrain.GetLength(0) - 1, convexHull.Max(p => p.x) + padding);
        int minZ = Mathf.Max(0, convexHull.Min(p => p.z) - padding);
        int maxZ = Mathf.Min(this.terrain.GetLength(2) - 1, convexHull.Max(p => p.z) + padding);

        int totalHeight = 0;
        int count = 0;
        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                if (geo.IsPointInPolygon(new Vector3Int(x, 0, z), convexHull))
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
                if (geo.IsPointInPolygonWithExtension(new Vector3Int(x, 0, z), convexHull, padding))
                {
                    // Set foundation type up to the average height
                    this.terrain[x, averageHeight, z] = foundationType;
                    //terrainHeights[x, z] = averageHeight;
                    WorldGeneration.terrainHeights[x, z] = averageHeight;

                    // Remove stuff above the average height but not cloud layer
                    for (int y = averageHeight + 1; y < GasFlowSystem.MAX_GAS_HEIGHT; y++)
                    {
                        this.terrain[x, y, z] = Substance.air;
                    }
                }
            }
        }

        return averageHeight;
    }

    int gateWidth = 3;
    int gateHeight = 5;
    public (List<Vector3Int>, List<Vector3Int>) BuildWallsAroundTown(Vector3Int townCenter, List<RectInt> lots, int wallHeight, int averageHeight, int towerSize = 0)
    {
        // Convert the lots to a list of points
        List<Vector3Int> points = new List<Vector3Int>();
        foreach (var lot in lots)
        {
            points.Add(new Vector3Int(lot.x, 0, lot.y));
            points.Add(new Vector3Int(lot.x + lot.width, 0, lot.y));
            points.Add(new Vector3Int(lot.x, 0, lot.y + lot.height));
            points.Add(new Vector3Int(lot.x + lot.width, 0, lot.y + lot.height));
        }


        // Find the convex hull around the points
        List<Vector3Int> convexHull = geo.FindConvexHull(points);
        for (int i = 0; i < convexHull.Count; i++)
        {
            Vector3Int point = convexHull[i];
            Vector3 direction = ((Vector3)(point-townCenter)).normalized;
            Vector3Int offset = Vector3Int.RoundToInt(direction * roadWidth);
            convexHull[i] = point + offset;
        }

        List<Vector3Int> gatePositions = new List<Vector3Int>();    
        List<Vector3Int> towerPositions = new List<Vector3Int>();
        // Build the walls around the convex hull
        for (int i = 0; i < convexHull.Count; i++)
        {
            Vector3Int start = convexHull[i];
            Vector3Int end = convexHull[(i + 1) % convexHull.Count]; // Get the next point, wrapping around

            // If towerSize is greater than 0, build towers at the corners
            if (towerSize > 0)
            {
                // Build tower at the start of the wall segment
                BuildTower(start,(int)(wallHeight*1.5f), towerSize, averageHeight, townCenter);

                // Adjust the start and end points for the wall segment to not overlap with the tower
                Vector3 direction = ((Vector3)(end - start)).normalized;
                start += Vector3Int.RoundToInt(direction * towerSize);
                end -= Vector3Int.RoundToInt(direction * towerSize);
            }

            // Build wall segment between the adjusted start and end
            BuildWallSegment(start, end, wallHeight, towerSize, this.gateWidth, this.gateHeight); // With a gate width of 3 and height of 5

            // Add gate position (center point) to the list
            Vector3Int middlePoint = (start + end) / 2;
            gatePositions.Add(new Vector3Int(middlePoint.x, averageHeight, middlePoint.z));
        }
        return (towerPositions, gatePositions);
    }


    public void BuildWallSegment(Vector3Int start, Vector3Int end, int height, int towerSize, int gateWidth, int gateHeight)
    {
        Vector3 direction = ((Vector3)(end - start)).normalized;
        float length = Vector3Int.Distance(start, end);
        int terrainWidth = terrainHeights.GetLength(0);
        int terrainDepth = terrainHeights.GetLength(1);

        Vector3Int middlePoint = (start + end) / 2; // Middle point of the wall segment

        for (float i = 0; i < length; i += 0.5f) // 0.5f step for denser interpolation
        {
            int x = Mathf.RoundToInt(start.x + direction.x * i);
            int z = Mathf.RoundToInt(start.z + direction.z * i);

            if (x >= 0 && x < terrainWidth && z >= 0 && z < terrainDepth)
            {
                int baseHeight = terrainHeights[x, z];

                // Determine if this position is part of the gate
                bool isGate = Mathf.Abs(x - middlePoint.x) < gateWidth / 2.0f &&
                              Mathf.Abs(z - middlePoint.z) < gateWidth / 2.0f;

                for (int y = baseHeight; y <= baseHeight + height; y++)
                {
                    // If this position is part of the gate and below the gateHeight, leave it empty
                    if (isGate && y < baseHeight + gateHeight)
                    {
                        continue;
                    }

                    if (IsWithinBounds(x, y, z)) // Check if the position is within the terrain bounds
                    {
                        // Set the wall block, for example, use Substance.wall or whatever you use for walls
                        terrain[x, y, z] = Substance.stone;
                    }
                }
            }
        }
    }


    public void BuildTower(Vector3Int position, int height, int towerSize, int averageHeight, Vector3Int townCenter)
    {
        int doorHeight = 5;
        int doorWidth = 3; // Set door width appropriately
                           // Calculate the base height (terrain height) at the tower position
        int baseHeight = terrainHeights[position.x, position.z];

        Vector2Int innerCorner = getTowerInnerCorner(position, towerSize, townCenter);

        // Calculate the inward direction vectors for the inner corner
        Vector2Int[] inwardDirections =
        {
        new Vector2Int(position.x - innerCorner.x, 0),
        new Vector2Int(0, position.z - innerCorner.y)
        };

        // Iterate through each position in the tower's volume
        for (int x = position.x - towerSize; x <= position.x + towerSize; x++)
        {
            for (int z = position.z - towerSize; z <= position.z + towerSize; z++)
            {
                for (int y = baseHeight; y <= baseHeight + height; y++)
                {
                    // Skip positions that are out of bounds
                    if (!IsWithinBounds(x, y, z)) continue;

                    // Determine if this position is part of the walls
                    bool isWall = x == position.x - towerSize || x == position.x + towerSize ||
                                  z == position.z - towerSize || z == position.z + towerSize;

                    // Determine if the position is at the door level and within the door area
                    bool isDoor = y < baseHeight + doorHeight &&
                                  ((inwardDirections[0].x * (x - innerCorner.x) >= 0 && Mathf.Abs(x - innerCorner.x) < doorWidth && z == innerCorner.y) ||
                                   (inwardDirections[1].y * (z - innerCorner.y) >= 0 && Mathf.Abs(z - innerCorner.y) < doorWidth && x == innerCorner.x));


                    // Skip the position if it's within the tower's inner volume, or is part of the door
                    if ((!isWall && y > baseHeight) || isDoor) continue;

                    // Set the block for the tower wall
                    terrain[x, y, z] = Substance.stone;
                }
            }
        }
    }

    private Vector2Int getTowerInnerCorner(Vector3Int towerPosition, int towerSize, Vector3Int townCenter)
    {
        Vector3Int[] corners = new Vector3Int[]
        {
    new Vector3Int(towerPosition.x - towerSize, towerPosition.y, towerPosition.z - towerSize),
    new Vector3Int(towerPosition.x + towerSize, towerPosition.y, towerPosition.z - towerSize),
    new Vector3Int(towerPosition.x - towerSize, towerPosition.y, towerPosition.z + towerSize),
    new Vector3Int(towerPosition.x + towerSize, towerPosition.y, towerPosition.z + towerSize)
        };

        Vector3Int innerCorner = corners[0];
        float minDistanceSq = (innerCorner - townCenter).sqrMagnitude;

        for (int i = 1; i < corners.Length; i++)
        {
            float distanceSq = (corners[i] - townCenter).sqrMagnitude;
            if (distanceSq < minDistanceSq)
            {
                innerCorner = corners[i];
                minDistanceSq = distanceSq;
            }
        }

        // innerCorner now contains the x, y, z coordinates of the corner closest to the town center.
        int innerCornerX = innerCorner.x;
        int innerCornerZ = innerCorner.z;
        return new Vector2Int(innerCorner.x, innerCorner.z);
    }


    public void ConnectTownsUsingRoads(List<TownData> townsData, int gateWidth)
    {
        int cloudHeight = GasFlowSystem.MAX_GAS_HEIGHT; // set to your cloud height value

        // Calculate all edges with their weights (distances) between towns
        List<Edge> edges = new List<Edge>();
        for (int t1 = 0; t1 < townsData.Count; t1++)
        {
            for (int t2 = t1 + 1; t2 < townsData.Count; t2++)
            {
                TownData town1 = townsData[t1];
                TownData town2 = townsData[t2];

                for (int i = 0; i < town1.gatePositions.Count; i++)
                {
                    for (int j = 0; j < town2.gatePositions.Count; j++)
                    {
                        float weight = Vector3Int.Distance(town1.gatePositions[i], town2.gatePositions[j]);
                        edges.Add(new Edge(town1.gatePositions[i], town2.gatePositions[j], weight, town1, town2));
                    }
                }
            }
        }

        // Sort edges based on weight
        edges.Sort((a, b) => a.Weight.CompareTo(b.Weight));

        // Kruskal's Algorithm
        UnionFind uf = new UnionFind(townsData.Count);
        foreach (Edge edge in edges)
        {
            int startTownIndex = townsData.IndexOf(edge.StartTown);
            int endTownIndex = townsData.IndexOf(edge.EndTown);

            if (uf.Find(startTownIndex) != uf.Find(endTownIndex))
            {
                // Connect these towns with a road between the respective gate positions
                BuildRoad(edge.Start, edge.End, gateWidth, cloudHeight);

                uf.Union(startTownIndex, endTownIndex);
            }
        }
    }

    public void BuildRoad(Vector3Int start, Vector3Int end, int width, int cloudHeight)
    {
        Vector3 direction = ((Vector3)(end - start)).normalized;
        float length = Vector3.Distance(start, end);

        int startY = start.y;
        int endY = end.y;

        // Start at 0.5f and end at length - 0.5f to skip the first and last steps
        for (float i = 2f; i < length - 2f; i += 0.5f)
        {
            int x = Mathf.RoundToInt(start.x + direction.x * i);
            int z = Mathf.RoundToInt(start.z + direction.z * i);
            float t = i / length; // Normalized distance along the road [0, 1]

            // Linearly interpolate the height along the road
            int interpolatedY = Mathf.RoundToInt(Mathf.Lerp(startY, endY, t));

            for (int dx = -width / 2; dx <= width / 2; dx++)
            {
                for (int dz = -width / 2; dz <= width / 2; dz++)
                {
                    if (IsWithinBounds(x + dx, interpolatedY, z + dz))
                    {
                        terrain[x + dx, interpolatedY, z + dz] = Substance.asphalt;

                        // Set above tiles to air
                        for (int y = interpolatedY + 1; y <= cloudHeight; y++)
                        {
                            if (IsWithinBounds(x + dx, y, z + dz))
                            {
                                terrain[x + dx, y, z + dz] = Substance.air;
                            }
                        }
                    }
                }
            }
        }
    }


    public bool IsWithinBounds(int x, int y, int z)
    {
        // Check if x, y, z are within the bounds of the terrain array
        return x >= 0 && x < terrain.GetLength(0) &&
               y >= 0 && y < terrain.GetLength(1) &&
               z >= 0 && z < terrain.GetLength(2);
    }


}
