using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelMap
{
    public const int width = 16;
    public const int height = 8;
    public const int depth = 16;
    public Voxel[,,] map = new Voxel[width, height, depth];

    public VoxelMap()
    {
        map = new Voxel[width, height, depth];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    if (y == 0)
                    {
                        map[x, y, z] = new Voxel(Voxel.Type.solid, new Vector3(x, y, z));
                    }
                    else
                    {
                        map[x, y, z] = new Voxel(Voxel.Type.gas, new Vector3(x, y, z));
                    }
                }
            }
        }
    }


    public Voxel GetVoxel(int x, int y, int z)
    {
        return map[x, y, z];
    }

}
