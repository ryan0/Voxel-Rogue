using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
    public const int width =  16;
    public const int height = 16;
    public const int depth =  16;
    
    private int xIndex = 0;
    private int yIndex = 0;
    private int zIndex = 0;

    private float biomeTemperature;

    private bool signalToRegenMesh = false;

    private Voxel[,,] voxels = new Voxel[width, height, depth];
    private ChunkMesh mesh = new();

    public static Chunk CreateChunk(int xIndex, int yIndex, int zIndex, Substance[,,] terrainData, float biomeTemprature)
    {
        GameObject obj = new GameObject("Chunk");
        Chunk chunk = obj.AddComponent<Chunk>();

        chunk.biomeTemperature = biomeTemprature;
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
                    Substance substance = terrainData[x + xOffset, y + yOffset, z + zOffset];
                    chunk.voxels[x, y, z] = new Voxel(x, y, z, chunk, substance, biomeTemprature);
                }
            }
        }

        return chunk;
    }

    private void Start()
    {
        mesh.GenerateMesh(voxels, xIndex, yIndex, zIndex);
    }

    public void SignalMeshRegen()
    {
        this.signalToRegenMesh = true;
    }

    private void Update()
    {
        if(signalToRegenMesh)
        {
            signalToRegenMesh = false;
            mesh.DestroyMesh();
            mesh.GenerateMesh(voxels, xIndex, yIndex, zIndex);
        }
    }

    public Voxel[,,] getVoxels()
    {
        return voxels;
    }

    public List<Voxel> GetVoxelsAdjacentTo(int x, int y, int z)
    {
        List<Voxel> adjacentVoxels = new();

        if (x < width - 1) adjacentVoxels.Add(voxels[x + 1, y, z]);
        if (y < height - 1) adjacentVoxels.Add(voxels[x, y + 1, z]);
        if (z < depth - 1) adjacentVoxels.Add(voxels[x, y, z + 1]);

        if (x > 0) adjacentVoxels.Add(voxels[x - 1, y, z]);
        if (y > 0) adjacentVoxels.Add(voxels[x, y - 1, z]);
        if (z > 0) adjacentVoxels.Add(voxels[x, y, z - 1]);

        return adjacentVoxels;
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
                        voxels[x, y, z].temperature -= 10;
                    }
                    else
                    {
                        voxels[x, y, z].temperature += 10;
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
