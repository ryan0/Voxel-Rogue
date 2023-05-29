using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
    public const int width =  64;
    public const int height = 64;
    public const int depth =  64;
    
    private int xIndex = 0;
    private int yIndex = 0;
    private int zIndex = 0;

    private Voxel[,,] voxels = new Voxel[width, height, depth];
    private ChunkMesh mesh = new();

    public static Chunk CreateChunk(int xIndex, int yIndex, int zIndex, Substance[,,] terrainData)
    {
        GameObject obj = new GameObject("Chunk");
        Chunk chunk = obj.AddComponent<Chunk>();

        chunk.xIndex = xIndex;
        chunk.yIndex = yIndex;
        chunk.zIndex = zIndex;

        int xOffset = xIndex * width;
        int yOffset = yIndex * height;
        int zOffset = zIndex * depth;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    chunk.voxels[x, y, z] = new Voxel(terrainData[x + xOffset, y + yOffset, z + zOffset]);
                }
            }
        }

        return chunk;
    }

    private void Start()
    {
        mesh.GenerateMesh(voxels, xIndex, yIndex, zIndex);
    }

    private void Update()
    {
        if (Input.GetKeyDown("["))
        {
            UpdateTemperatures(0);
        }
        if (Input.GetKeyDown("]"))
        {
            UpdateTemperatures(1);
        }
    }

    public void destroyVoxelAt(int x, int y, int z)
    {
        voxels[x, y, z].substance = Substance.air;

        mesh.DestroyMesh();
        mesh.GenerateMesh(voxels, xIndex, yIndex, zIndex);
    }

    void UpdateTemperatures(int direction)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (direction == 0)
                    {
                        voxels[x, y, z].Temperature -= 10;
                    }
                    else
                    {
                        voxels[x, y, z].Temperature += 10;
                    }
                    //if(voxels[x, y, z].changeState())
                    //{
                    //    Destroy(voxelMesh[x, y, z]);
                    //    createVoxelRepresentationAt(x, y, z);
                    //}
                }
            }
        }
    }

}
