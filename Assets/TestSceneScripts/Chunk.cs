using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
    public const int width = 16;
    public const int height = 16;
    public const int depth = 16;

    private int xIndex = 0;
    private int yIndex = 0;
    private int zIndex = 0;

    private Voxel[,,] voxels = new Voxel[width, height, depth];
    private GameObject[,,] voxelMesh = new GameObject[width, height, depth];

    private void Start()
    {
        GenerateVoxelMesh();
    }

    public static Chunk CreateChunk(int xIndex, int yIndex, int zIndex, Material[,,] terrainData)
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


    private void GenerateVoxelMesh()
    {

        int xOffset = xIndex * width;
        int yOffset = yIndex * height;
        int zOffset = zIndex * depth;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    bool occluded = true;
                    if (y == 0 || voxels[x, y - 1, z].material.name == "air") occluded = false;
                    if (y == (height - 1) || voxels[x, y + 1, z].material.name == "air") occluded = false;

                    if (x == 0 || voxels[x - 1, y, z].material.name == "air") occluded = false;
                    if (x == (width - 1) || voxels[x + 1, y, z].material.name == "air") occluded = false;

                    if (z == 0 || voxels[x, y, z - 1].material.name == "air") occluded = false;
                    if (z == (depth - 1) || voxels[x, y, z + 1].material.name == "air") occluded = false;

                    if (voxels[x, y, z].material.name != "air" && !occluded)
                    {
                        string fab = "Prefabs/" + voxels[x, y, z].material.name;
                        Vector3 pos = new Vector3(x + xOffset, y + yOffset, z + zOffset);
                        voxelMesh[x, y, z] = Instantiate(Resources.Load(fab), pos, Quaternion.identity) as GameObject;
                    }
                }
            }
        }
    }


    private void OnDrawGizmos()     {          for (int x = 0; x < width; x++)         {             for (int y = 0; y < height; y++)             {                 for (int z = 0; z < depth; z++)                 {                     DrawVoxel(x, y, z);                 }             }         }     }     private void DrawVoxel(int x, int y, int z)     {         Gizmos.color = Color.green;         Vector3 position = new Vector3(x, y, z) * 1.0f;         Gizmos.DrawWireCube(position, Vector3.one * 1.0f);     }
}
