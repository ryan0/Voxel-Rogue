﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
    public Chunk northNeighbour;
    public Chunk southNeighbour;
    public Chunk eastNeighbour;
    public Chunk westNeighbour;
    public Chunk topNeighbour;
    public Chunk bottomNeighbour;
    public Chunk[] neighbors;
    public World world;

    public const int width = 16;
    public const int height = 32;
    public const int depth = 16;

    public Vector3Int index = new();

    private float biomeTemperature;

    private bool signalToRegenMesh = false;

    private Voxel[,,] voxels = new Voxel[width, height, depth];
    private ChunkMesh mesh = new();

    public static Chunk CreateChunk(int xIndex, int yIndex, int zIndex, Substance[,,] terrainData, float biomeTemprature, World world)
    {
        GameObject obj = new GameObject("Chunk");
        Chunk chunk = obj.AddComponent<Chunk>();
        chunk.world = world;
        chunk.biomeTemperature = biomeTemprature;
        chunk.index.x = xIndex;
        chunk.index.y = yIndex;
        chunk.index.z = zIndex;

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
                    chunk.voxels[x, y, z] = new Voxel(x, y, z, chunk, substance, biomeTemprature, 5);
                }
            }
        }

        return chunk;
    }

    public Vector3Int GetIndex()
    {
        return index;
    }

    private void Start()
    {
        mesh.GenerateMesh(voxels, index, world);
    }

    public void createVoxelAt(int x, int y, int z, Substance substance, int mote)
    {
        voxels[x, y, z] = new Voxel(x, y, z, this, substance, biomeTemperature, mote);
        SignalMeshRegen();
    }

    public void SignalMeshRegen()
    {
        this.signalToRegenMesh = true;
    }

    private void Update()
    {
        if (signalToRegenMesh)
        {
            signalToRegenMesh = false;
            mesh.DestroyMesh();
            mesh.GenerateMesh(voxels, index, world);
        }
    }

    public Voxel[,,] getVoxels()
    {
        return voxels;
    }

    public Voxel GetVoxelAt(int x, int y, int z)
    {
        return voxels[x, y, z];
    }

    public Voxel GetVoxelAt(Vector3Int pos)
    {
        return voxels[pos.x, pos.y, pos.z];
    }


    public Voxel[] GetVoxelsAdjacentTo(int x, int y, int z)
    {
        Voxel[] adjacentVoxels = new Voxel[6];

        // Handling voxel adjacencies within the same chunk
        adjacentVoxels[0] = (x < width - 1) ? voxels[x + 1, y, z] : null;//east
        adjacentVoxels[1] = (y < height - 1) ? voxels[x, y + 1, z] : null;//top
        adjacentVoxels[2] = (z < depth - 1) ? voxels[x, y, z + 1] : null;//north
        adjacentVoxels[3] = (x > 0) ? voxels[x - 1, y, z] : null;//west
        adjacentVoxels[4] = (y > 0) ? voxels[x, y - 1, z] : null;//bottom
        adjacentVoxels[5] = (z > 0) ? voxels[x, y, z - 1] : null;//south

        // Handling voxel adjacencies between chunks
        if (x == 0 && westNeighbour != null)
        {
            //Debug.Log("Adding voxel from West neighbour.");
            adjacentVoxels[3] = westNeighbour.getVoxels()[width - 1, y, z];
        }
        if (x == width - 1 && eastNeighbour != null)
        {
            //Debug.Log("Adding voxel from East neighbour.");
            adjacentVoxels[0] = eastNeighbour.getVoxels()[0, y, z];
        }
        if (y == 0 && bottomNeighbour != null)
        {
            //Debug.Log("Adding voxel from Bottom neighbour.");
            adjacentVoxels[4] = bottomNeighbour.getVoxels()[x, height - 1, z];
        }
        if (y == height - 1 && topNeighbour != null)
        {
            //Debug.Log("Adding voxel from Top neighbour.");
            adjacentVoxels[1] = topNeighbour.getVoxels()[x, 0, z];
        }
        if (z == 0 && southNeighbour != null)
        {
            //Debug.Log("Adding voxel from South neighbour.");
            adjacentVoxels[5] = southNeighbour.getVoxels()[x, y, depth - 1];
        }
        if (z == depth - 1 && northNeighbour != null)
        {
            //Debug.Log("Adding voxel from North neighbour.");
            adjacentVoxels[2] = northNeighbour.getVoxels()[x, y, 0];
        }

        return adjacentVoxels;
    }

    public Voxel[] GetVoxelsHorizonatllyAdjacentTo(int x, int y, int z)
    {
        Voxel[] adjacentVoxels = new Voxel[4];

        // Handling voxel adjacencies within the same chunk
        adjacentVoxels[0] = (x < width - 1) ? voxels[x + 1, y, z] : null;//east
        adjacentVoxels[1] = (z < depth - 1) ? voxels[x, y, z + 1] : null;//north
        adjacentVoxels[2] = (x > 0) ? voxels[x - 1, y, z] : null;//west
        adjacentVoxels[3] = (z > 0) ? voxels[x, y, z - 1] : null;//south

        // Handling voxel adjacencies between chunks
        if (x == 0 && westNeighbour != null)
        {
            //Debug.Log("Adding voxel from West neighbour.");
            adjacentVoxels[2] = westNeighbour.getVoxels()[width - 1, y, z];
        }
        if (x == width - 1 && eastNeighbour != null)
        {
            //Debug.Log("Adding voxel from East neighbour.");
            adjacentVoxels[0] = eastNeighbour.getVoxels()[0, y, z];
        }
        if (z == 0 && southNeighbour != null)
        {
            //Debug.Log("Adding voxel from South neighbour.");
            adjacentVoxels[3] = southNeighbour.getVoxels()[x, y, depth - 1];
        }
        if (z == depth - 1 && northNeighbour != null)
        {
            //Debug.Log("Adding voxel from North neighbour.");
            adjacentVoxels[1] = northNeighbour.getVoxels()[x, y, 0];
        }

        return adjacentVoxels;
    }


    public void destroyVoxelAt(int x, int y, int z)
    {
        voxels[x, y, z].substance = Substance.air;

        mesh.DestroyMesh();
        mesh.GenerateMesh(voxels, index, world);
    }


    public void highlightAdjVoxels(int x, int y, int z)
    {
        /*Voxel[] adjVoxels = GetVoxelsAdjacentTo(x,y,z);
        foreach(Voxel v in adjVoxels)
        {
            if (v != null)
            {
                v.substance = Substance.debug;
            }
        }
        mesh.DestroyMesh();
        mesh.GenerateMesh(voxels, xIndex, yIndex, zIndex);
        */
        //highlight bottom voxel       
        //Voxel[] adjVoxels = GetVoxelsAdjacentTo(x, y, z);
        //{
        //    Voxel voxelBelow = adjVoxels[4];//voxel.chunk.bottomNeighbour.getVoxels()[x, voxel.chunk.heightPub - 1, z];//adjacentVoxels.Find(v => v.chunk.yIndex < chunk.yIndex); // Neighbor below has a lower y-coordinate
        //    voxelBelow.substance = Substance.debug;
        //}
        //foreach (Chunk c in neighbors)
        //{//need to regen neighboring chunks to show neighboring interactions
        //    if (c != null)
        //    {
        //        c.mesh.DestroyMesh();
        //        c.mesh.GenerateMesh(c.voxels, c.xIndex, c.yIndex, c.zIndex);
        //    }
        //}
        //mesh.DestroyMesh();
        //mesh.GenerateMesh(voxels, xIndex, yIndex, zIndex);
        //mesh.DestroyMesh();
        //mesh.GenerateMesh(voxels, xIndex, yIndex, zIndex);
    }
}

public class ChunkComparer : IComparer<Chunk>
{
    public int Compare(Chunk x, Chunk y)
    {
        if (x == null)
        {
            if (y == null)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        else
        {
            if (y == null)
            {
                return 1;
            }
            else
            {
                return x.index.y.CompareTo(y.index.y);
            }
        }
    }
}
