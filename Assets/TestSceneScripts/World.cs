using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public const int chunksX = 2;
    public const int chunksY = 2;
    public const int chunksZ = 2;

    private Chunk[,,] chunks = new Chunk[chunksX, chunksY, chunksZ];

    // Start is called before the first frame update
    void Start()
    {
        Substance[,,] terrainData = genTerrain();

        for (int x = 0; x < chunksX; x++)
        {
            for (int y = 0; y < chunksY; y++)
            {
                for (int z = 0; z < chunksZ; z++)
                {
                    Chunk.CreateChunk(x, y, z, terrainData);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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

    private static Substance[,,] genTerrain()
    {
        int width = chunksX * Chunk.width;
        int height = chunksY * Chunk.height;
        int depth = chunksZ * Chunk.depth;
        Substance[,,] terrain = new Substance[width, height, depth];

        float scale = 0.1f;  // Adjust this value to change the 'roughness' of your terrain
        float heightScale = 10.0f;  // Adjust this value to change the maximum height of the terrain
        float waterScale = 0.02f;  // Adjust this value to change the 'roughness' of your water distribution (smaller for larger bodies)
        float waterThreshold = 0.4f;  // Lower this value to make water more common

        int waterLevel = 3; // Increase this Y coordinate to make the land more often under water

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < depth; z++)
            {
                // Calculate the height of the terrain at this point
                int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(x * scale, z * scale) * heightScale);

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

        return terrain;
    }


}
