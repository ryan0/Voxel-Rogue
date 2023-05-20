using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMapGenerator : MonoBehaviour
{
    private VoxelMap voxelMap;


    private void Awake()
    {
        voxelMap = new VoxelMap(); // Replace this line if you have a different way of creating your VoxelMap.
    }
   
    private void Start()
    {
        GenerateMap();
    }

    private void GenerateMap()
    {
        for (int x = 0; x < VoxelMap.width; x++)
        {
            for (int y = 0; y < VoxelMap.height; y++)
            {
                for (int z = 0; z < VoxelMap.depth; z++)
                {
                    Voxel voxel = voxelMap.GetVoxel(x, y, z);
                    GameObject voxelInstance = Instantiate(Resources.Load("Prefabs/" + Voxel.typeAsString(voxel.type)), voxel.position, Quaternion.identity) as GameObject;
                }
            }
        }
    }





    private void OnDrawGizmos()     {          for (int x = 0; x < VoxelMap.width; x++)         {             for (int y = 0; y < VoxelMap.height; y++)             {                 for (int z = 0; z < VoxelMap.depth; z++)                 {                     DrawVoxel(x, y, z);                 }             }         }     }     private void DrawVoxel(int x, int y, int z)     {         Gizmos.color = Color.green;         Vector3 position = new Vector3(x, y, z) * 1.0f;         Gizmos.DrawWireCube(position, Vector3.one * 1.0f);     } 
}

