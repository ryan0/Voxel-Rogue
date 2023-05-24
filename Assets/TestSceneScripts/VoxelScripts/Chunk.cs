using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chunk : MonoBehaviour {
    public const int width =  64;
    public const int height = 64;
    public const int depth =  64;
    
    private float s = Voxel.size;
    private int xIndex = 0;
    private int yIndex = 0;
    private int zIndex = 0;

    private Voxel[,,] voxels = new Voxel[width, height, depth];
    private Dictionary<int, GameObject> meshData = new Dictionary<int, GameObject>();

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
        GenerateVoxelMesh();
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
        foreach(KeyValuePair<int, GameObject> entry in meshData)
        {
            Destroy(entry.Value);
        }
        meshData = new Dictionary<int, GameObject>();
        GenerateVoxelMesh();
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

    private void GenerateVoxelMesh()
    {
        Dictionary<int, List<Vector3>> verticesData = new Dictionary<int, List<Vector3>>();
        Dictionary<int, List<int>> trianglesData = new Dictionary<int, List<int>>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    createVoxelRepresentationAt(verticesData, trianglesData, x, y, z);
                }
            }
        }


        foreach (KeyValuePair<int, List<Vector3>> entry in verticesData)
        {
            Mesh mesh = new Mesh();

            mesh.vertices = entry.Value.ToArray();
            mesh.triangles = trianglesData[entry.Key].ToArray();
            mesh.RecalculateNormals();

            float xOffset = xIndex * width * s;
            float yOffset = yIndex * height * s;
            float zOffset = zIndex * depth * s;

            GameObject chunkMesh = new GameObject("chunkMesh");
            MeshFilter meshFilter = chunkMesh.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            string mat = "Materials/" + Substance.getById(entry.Key).name;
            Material meshMat = Resources.Load<Material>(mat);

            if(Substance.getById(entry.Key).state == State.SOLID)
            {
                MeshCollider meshCollider = chunkMesh.AddComponent<MeshCollider>();
            }
            MeshRenderer meshRenderer = chunkMesh.AddComponent<MeshRenderer>();
            meshRenderer.material = meshMat;
            chunkMesh.transform.position = new Vector3(xOffset, yOffset, zOffset);
            meshData.Add(entry.Key, chunkMesh);
        }
    }

    private void createVoxelRepresentationAt(Dictionary<int, List<Vector3>> verticesData, Dictionary<int, List<int>> trianglesData, int x, int y, int z)
    {
        Substance substance = voxels[x, y, z].substance;

        List<Vector3> vertices;
        List<int> triangles;

        if(verticesData.ContainsKey(substance.id))
        {
            vertices = verticesData[substance.id];
            triangles = trianglesData[substance.id];
        }
        else
        {
            vertices = new List<Vector3>();
            verticesData.Add(substance.id, vertices);
            triangles = new List<int>();
            trianglesData.Add(substance.id, triangles);
        }

        if (substance.id != Substance.air.id)
        {
            float xS = x * s;
            float yS = y * s;
            float zS = z * s;

            if (y == 0 || voxels[x, y - 1, z].substance.state == State.GAS) //Bottom
            {

                vertices.Add(new Vector3(xS + 0, yS + 0, zS + 0));
                vertices.Add(new Vector3(xS + 0, yS + 0, zS + s));
                vertices.Add(new Vector3(xS + s, yS + 0, zS + s));
                vertices.Add(new Vector3(xS + s, yS + 0, zS + 0));

                int count = vertices.Count - 4;

                triangles.Add(0 + count); triangles.Add(2 + count); triangles.Add(1 + count);
                triangles.Add(0 + count); triangles.Add(3 + count); triangles.Add(2 + count);
            }

            if ( y == (height - 1) || voxels[x, y + 1, z].substance.state == State.GAS) //Top
            {

                vertices.Add(new Vector3(xS + 0, yS + s, zS + 0));
                vertices.Add(new Vector3(xS + 0, yS + s, zS + s));
                vertices.Add(new Vector3(xS + s, yS + s, zS + s));
                vertices.Add(new Vector3(xS + s, yS + s, zS + 0));

                int count = vertices.Count - 4;

                triangles.Add(0 + count); triangles.Add(1 + count); triangles.Add(2 + count);
                triangles.Add(0 + count); triangles.Add(2 + count); triangles.Add(3 + count);
            }

            if (z == 0 || voxels[x, y, z - 1].substance.state == State.GAS) // Front
            {
                vertices.Add(new Vector3(xS + 0, yS + 0, zS + 0));
                vertices.Add(new Vector3(xS + 0, yS + s, zS + 0));
                vertices.Add(new Vector3(xS + s, yS + s, zS + 0));
                vertices.Add(new Vector3(xS + s, yS + 0, zS + 0));


                int count = vertices.Count - 4;

                triangles.Add(0 + count); triangles.Add(1 + count); triangles.Add(2 + count);
                triangles.Add(0 + count); triangles.Add(2 + count); triangles.Add(3 + count);
            }

            if (z == (depth - 1) || voxels[x, y, z + 1].substance.state == State.GAS) // Back
            {
                vertices.Add(new Vector3(xS + 0, yS + 0, zS + s));
                vertices.Add(new Vector3(xS + 0, yS + s, zS + s));
                vertices.Add(new Vector3(xS + s, yS + s, zS + s));
                vertices.Add(new Vector3(xS + s, yS + 0, zS + s));


                int count = vertices.Count - 4;

                triangles.Add(0 + count); triangles.Add(2 + count); triangles.Add(1 + count);
                triangles.Add(0 + count); triangles.Add(3 + count); triangles.Add(2 + count);
            }

            if (x == 0 || voxels[x - 1, y, z].substance.state == State.GAS) // Left
            {
                vertices.Add(new Vector3(xS + 0, yS + 0, zS + 0));
                vertices.Add(new Vector3(xS + 0, yS + s, zS + 0));
                vertices.Add(new Vector3(xS + 0, yS + s, zS + s));
                vertices.Add(new Vector3(xS + 0, yS + 0, zS + s));


                int count = vertices.Count - 4;

                triangles.Add(0 + count); triangles.Add(2 + count); triangles.Add(1 + count);
                triangles.Add(0 + count); triangles.Add(3 + count); triangles.Add(2 + count);
            }

            if (x == (width - 1) || voxels[x + 1, y, z].substance.state == State.GAS)
            {
                vertices.Add(new Vector3(xS + s, yS + 0, zS + 0));
                vertices.Add(new Vector3(xS + s, yS + s, zS + 0));
                vertices.Add(new Vector3(xS + s, yS + s, zS + s));
                vertices.Add(new Vector3(xS + s, yS + 0, zS + s));


                int count = vertices.Count - 4;

                triangles.Add(0 + count); triangles.Add(1 + count); triangles.Add(2 + count);
                triangles.Add(0 + count); triangles.Add(2 + count); triangles.Add(3 + count);
            }
        }
    }

    private bool isOccluded(int x, int y, int z)
    {
        bool occluded = true;

        if (y == 0 || voxels[x, y - 1, z].substance.name == "air") occluded = false;
        if (y == (height - 1) || voxels[x, y + 1, z].substance.name == "air") occluded = false;

        if (x == 0 || voxels[x - 1, y, z].substance.name == "air") occluded = false;
        if (x == (width - 1) || voxels[x + 1, y, z].substance.name == "air") occluded = false;

        if (z == 0 || voxels[x, y, z - 1].substance.name == "air") occluded = false;
        if (z == (depth - 1) || voxels[x, y, z + 1].substance.name == "air") occluded = false;

        return occluded;
    }


    private void OnDrawGizmos()
    {

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    //DrawVoxel(x, y, z);
                }
            }
        }
    }
    private void DrawVoxel(int x, int y, int z)
    {
        Gizmos.color = Color.green;
        Vector3 position = new Vector3(x, y, z) * 1.0f;
        Gizmos.DrawWireCube(position, Vector3.one * 1.0f);
    }
}
