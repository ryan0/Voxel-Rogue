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
    public TownType type;
    public TownData(Vector3Int _clusterCenter, List<HouseData> _houses, List<RectInt> _lots, List<Vector3Int> _roadPositions, TownType _type = TownType.village)
    {
        ClusterCenter = _clusterCenter;
        Houses = _houses;
        lots = _lots;
        roadPositions = _roadPositions;
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
                    Debug.Log(rng);
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

        (List<RectInt> lots, List<Vector3Int> roadPositions) = CreateLots(townCenter, floorValue, townType, new List<RectInt>());
        int averageHeight = DrawLots(lots, roadPositions, townCenter);
        HouseGeneration houseGen = new HouseGeneration();
        List<HouseData>  houses = houseGen.LayHouses(terrain, townCenter, averageHeight, lots, roadPositions, townDensity);
        TownData townData = new TownData(townCenter, houses, lots, roadPositions);
        WorldGeneration.worldTownsData.Add(townData);

    }


    private (List<RectInt>, List<Vector3Int>) CreateLots(Vector3Int center, int floorValue, TownType type, List<RectInt> lots)
    {
        int townSize = (type == TownType.village) ? 4 : 10; // example sizes in num of lots
        int halfTownSize = townSize / 2;
        int lotSize = 7;
        int roadWidth = 2; // Width of the roads
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
                        for (int y = averageHeight + 1; y < maxCloudHeight; y++)
                        {
                            terrain[x, y, z] = Substance.air;
                        }
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
                for (int y = averageHeight + 1; y < maxCloudHeight; y++)
                {
                    terrain[roadPos.x, y, roadPos.z] = Substance.air;
                }
            }
        }
        return averageHeight;
    }


    private List<Vector3Int> GenerateConvexHull(int worldX, int worldZ, TownType townType)
    {
        // Parameters for convex hull generation
        int villageLots = 10; // Number of lots in a village
        int minDistanceBetweenLots = 5; // Minimum distance between lots

        // Determine the number of lots based on the town type
        int numberOfLots;
        switch (townType)
        {
            case TownType.village:
                numberOfLots = villageLots;
                break;
            case TownType.city:
                numberOfLots = villageLots * 2;
                break;
            default:
                numberOfLots = villageLots;
                break;
        }

        // Generate random points for lots
        List<Vector3Int> lots = new List<Vector3Int>();
        while (lots.Count < numberOfLots)
        {
            // Randomly select a point within the chunk
            int x = UnityEngine.Random.Range(worldX, worldX + chunkSize);
            int z = UnityEngine.Random.Range(worldZ, worldZ + chunkSize);
            Vector3Int newLot = new Vector3Int(x, 0, z);

            // Check if it is at least minDistanceBetweenLots from all other lots
            bool tooClose = false;
            foreach (var lot in lots)
            {
                if (Vector3Int.Distance(newLot, lot) < minDistanceBetweenLots)
                {
                    tooClose = true;
                    break;
                }
            }

            // If it's not too close to any other lot, add it
            if (!tooClose)
            {
                lots.Add(newLot);
            }
        }

        // Calculate the convex hull using the Graham's scan algorithm

        return GrahamScan.ConvexHull(lots);
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



}
