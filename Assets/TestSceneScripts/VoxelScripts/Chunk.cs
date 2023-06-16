using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
    public Chunk northNeighbour;
    public Chunk southNeighbour;
    public Chunk eastNeighbour;
    public Chunk westNeighbour;
    public Chunk topNeighbour;
    public Chunk bottomNeighbour;

    public World world;

    public const int width =  16;
    public const int height = 16;
    public const int depth =  16;
    
    public int xIndex = 0;
    public int yIndex = 0;
    public int zIndex = 0;

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
                    chunk.voxels[x, y, z] = new Voxel(x, y, z, chunk, substance, biomeTemprature, 5);
                }
            }
        }

        return chunk;
    }

    private void Start()
    {
        mesh.GenerateMesh(voxels, xIndex, yIndex, zIndex);
    }

    public void createVoxelAt(int x, int y, int z, Substance substance, int mote)
    {
        // Create a new voxel at the specified position
        voxels[x, y, z] = new Voxel(x, y, z, this, substance, biomeTemperature, mote);

        // Signal that the mesh needs to be regenerated due to voxel changes
        SignalMeshRegen();
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

    public Voxel GetVoxel(int x, int y, int z)
    {
        // Add boundary checks
        if (x < 0 || x >= width || y < 0 || y >= height || z < 0 || z >= depth)
            return null;

        return voxels[x, y, z];
    }


    public List<Voxel> GetVoxelsAdjacentTo(int x, int y, int z)
    {
        List<Voxel> adjacentVoxels = new List<Voxel>();

        // Handling voxel adjacencies within the same chunk
        if (x < width - 1) adjacentVoxels.Add(voxels[x + 1, y, z]);
        if (y < height - 1) adjacentVoxels.Add(voxels[x, y + 1, z]);
        if (z < depth - 1) adjacentVoxels.Add(voxels[x, y, z + 1]);
        if (x > 0) adjacentVoxels.Add(voxels[x - 1, y, z]);
        if (y > 0) adjacentVoxels.Add(voxels[x, y - 1, z]);
        if (z > 0) adjacentVoxels.Add(voxels[x, y, z - 1]);

        // Handling voxel adjacencies between chunks
        if (x == 0 && westNeighbour != null)
        {
            Debug.Log("Adding voxel from West neighbour.");
            adjacentVoxels.Add(westNeighbour.getVoxels()[width - 1, y, z]);
        }
        if (x == width - 1 && eastNeighbour != null)
        {
            Debug.Log("Adding voxel from East neighbour.");
            adjacentVoxels.Add(eastNeighbour.getVoxels()[0, y, z]);
        }
        if (y == 0 && bottomNeighbour != null)
        {
            Debug.Log("Adding voxel from Bottom neighbour.");
            adjacentVoxels.Add(bottomNeighbour.getVoxels()[x, height - 1, z]);
        }
        if (y == height - 1 && topNeighbour != null)
        {
            Debug.Log("Adding voxel from Top neighbour.");
            adjacentVoxels.Add(topNeighbour.getVoxels()[x, 0, z]);
        }
        if (z == 0 && southNeighbour != null)
        {
            Debug.Log("Adding voxel from South neighbour.");
            adjacentVoxels.Add(southNeighbour.getVoxels()[x, y, depth - 1]);
        }
        if (z == depth - 1 && northNeighbour != null)
        {
            Debug.Log("Adding voxel from North neighbour.");
            adjacentVoxels.Add(northNeighbour.getVoxels()[x, y, 0]);
        }

        return adjacentVoxels;
    }


    public void destroyVoxelAt(int x, int y, int z)
    {
        voxels[x, y, z].substance = Substance.air;

        mesh.DestroyMesh();
        mesh.GenerateMesh(voxels, xIndex, yIndex, zIndex);

    }


}
