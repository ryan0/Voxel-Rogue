using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


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
    public TownData townAddress;/////TO DO ASSIGN TOWNS TO HOUSES WHEN THEY ARE CREATED
    // Add more properties as needed for NPCs, stores, etc.
}

public class HouseGeneration
{
    Geometry geo;
    int doorWidth = 2;
    int doorHeight = 4;
    public HouseGeneration()
    {
        geo = new Geometry();
    }
    public List<HouseData> CheckHouses(Substance[,,] terrain, List<Vector3Int> convexHull, int floorValue, int averageHeight, int roadWidth, int lotSize, int townDensity)
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
                        if (!geo.IsPointInPolygon(new Vector3Int(lotX, 0, lotZ), convexHull))
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

    public List<HouseData> LayHouses(Substance[,,] terrain, Vector3Int townCenter, int averageHeight, List<RectInt> lots, List<Vector3Int> roadPositions, int townDensity)
    {
        System.Random random = new System.Random();
        List<HouseData> houses = new List<HouseData>();
        int maxX = terrain.GetLength(0) - 1;
        int maxY = terrain.GetLength(1) - 1;
        int maxZ = terrain.GetLength(2) - 1;

        // Iterate through the lots
        foreach (RectInt lot in lots)
        {
            // Check if house should be placed based on town density
            if (random.Next(0, 100) >= townDensity)
                continue;

            int x = lot.xMin;
            int z = lot.yMin;

            // Randomly decide the house dimensions
            int houseWidth = random.Next(5, lot.width);
            int houseDepth = random.Next(5, lot.height);
            int houseHeight = random.Next(5, 8);

            // Lay house base
            for (int hx = x; hx < x + houseWidth; hx++)
            {
                for (int hz = z; hz < z + houseDepth; hz++)
                {
                    for (int hy = averageHeight + 1; hy < averageHeight + houseHeight; hy++)
                    {
                        if (hx >= 0 && hx < maxX && hy >= 0 && hy < maxY && hz >= 0 && hz < maxZ) // Improved boundary check  
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
                    if (rx >= 0 && rx < maxX &&
            roofBaseHeight + rh >= 0 && roofBaseHeight + rh < maxY &&
            rz >= 0 && rz < maxZ) // Improved boundary check
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
            int doorWidth = 2;
            int doorHeight = 3;
            for (int dx = 0; dx < doorWidth; dx++)
            {
                for (int dy = 0; dy < doorHeight; dy++)
                {
                    if (doorX + dx >= 0 && doorX + dx < maxX && averageHeight + 1 + dy >= 0 && averageHeight + 1 + dy < maxY && doorZ >= 0 && doorZ < maxZ) // ADDITIONAL CHECK HERE
                    {
                        terrain[doorX + dx, averageHeight + 1 + dy, doorZ] = Substance.air;
                    }
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

        return houses;
    }

}

