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
        Substance[,,] terrainData = WorldGeneration.genTerrain();

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
}
