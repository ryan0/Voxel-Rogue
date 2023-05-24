using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public const int chunksX = 4;
    public const int chunksY = 2;
    public const int chunksZ = 4;

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
                    chunks[x, y, z] = Chunk.CreateChunk(x, y, z, terrainData);
                }
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void destroyVoxelAt(int x, int y, int z)
    {
        int chunkX = x / Chunk.width;
        int chunkY = y / Chunk.height;
        int chunkZ = z / Chunk.depth;

        int voxelX = x - (chunkX * Chunk.width);
        int voxelY = y - (chunkY * Chunk.height);
        int voxelZ = z - (chunkZ * Chunk.depth);

        Debug.Log("hit Voxel: " + voxelX + ", " + voxelY + ", " + voxelZ);
        Debug.Log("in Chunk: " + chunkX + ", " + chunkY + ", " + chunkZ);

        chunks[chunkX, chunkY, chunkZ].destroyVoxelAt(voxelX, voxelY, voxelZ);
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

        float scale = 0.1f * Voxel.size;  // Adjust this value to change the 'resolution' of your terrain
        float heightScale = 15.0f;  // Adjust this value to change the maximum height of the terrain
        float waterScale = 0.01f;  // Adjust this value to change the 'roughness' of your water distribution (smaller for larger bodies)
        float waterThreshold = 0.5f;  // Lower this value to make water more common

        int waterLevel = 3; // Increase this Y coordinate to make the land more often under water

        // Variables related to tree generation
        float treeProbability = 0.00005f;  // Probability of tree being generated at any eligible location
        //int minTreeSpacing = 3;  // Minimum distance between trees
        int[,] terrainHeights = new int[width, depth];  // Store terrain height for each (x, z)
        System.Random random = new System.Random();  // Seed this for deterministic tree placement

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
                    if (random.NextDouble() < treeProbability && y > waterLevel)
                    {
                        Vector3Int treePos = new Vector3Int(x, terrainHeight, z);
                        GenerateTree(terrain, treePos, 4, 3);
                    }
                }
            }
        }

        return terrain;
    }

    private static void GenerateTree(Substance[,,] voxels, Vector3Int position, int trunkHeight, int crownRadius)
    {
        int width = voxels.GetLength(0);
        int height = voxels.GetLength(1);
        int depth = voxels.GetLength(2);

        // Generate trunk
        for (int y = position.y; y < position.y + trunkHeight; y++)
        {
            if (y < height)
            { // Check bounds
                voxels[position.x, y, position.z] = Substance.wood;
            }
        }

        // Generate crown
        Vector3Int crownCenter = position + new Vector3Int(0, trunkHeight, 0);

        for (int x = crownCenter.x - crownRadius; x <= crownCenter.x + crownRadius; x++)
        {
            for (int y = crownCenter.y; y <= crownCenter.y + crownRadius; y++)
            {
                for (int z = crownCenter.z - crownRadius; z <= crownCenter.z + crownRadius; z++)
                {
                    Vector3Int voxelPosition = new Vector3Int(x, y, z);
                    // Check if this voxel position is within the sphere (crown)
                    if (Vector3Int.Distance(voxelPosition, crownCenter) <= crownRadius
                        && x >= 0 && x < width && y >= 0 && y < height && z >= 0 && z < depth)
                    { // Check bounds
                        voxels[x, y, z] = Substance.leaf;
                    }
                }
            }
        }
    }




}
