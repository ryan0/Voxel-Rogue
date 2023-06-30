using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh
{
    readonly float s = Voxel.size;
    private Dictionary<int, GameObject> meshData = new Dictionary<int, GameObject>();
    Vector2[] faceUVs = new Vector2[4] {
        new Vector2(0, 0),
        new Vector2(0, 1),
        new Vector2(1, 1),
        new Vector2(1, 0)
    };

    public void DestroyMesh()
    {
        foreach (KeyValuePair<int, GameObject> entry in meshData)
        {
            Object.Destroy(entry.Value);
        }
        meshData = new Dictionary<int, GameObject>();
    }

    public void GenerateMesh(Voxel[,,] voxels, Vector3Int chunkIndex, World world)
    {
        int width = voxels.GetLength(0);
        int height = voxels.GetLength(1);
        int depth = voxels.GetLength(2);

        //Dictionary<int, List<Vector3>> verticesData = new Dictionary<int, List<Vector3>>();

        List<int> substanceIds = new();
        List<Vector3>[] verticesData = new List<Vector3>[Substance.NumberSubstances()];
        List<int>[] trianglesData = new List<int>[Substance.NumberSubstances()];
        List<Vector2>[] uvData = new List<Vector2>[Substance.NumberSubstances()]; // Add this line

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Substance substance = voxels[x, y, z].substance;

                    if (substance.id != Substance.air.id)
                    {
                        createVoxelRepresentationAt(substanceIds, voxels, verticesData, trianglesData, uvData, substance, x, y, z); // Pass uvData here                
                    }
                }
            }
        }


        foreach (int i in substanceIds)
        {
            Mesh mesh = new Mesh();

            mesh.vertices = verticesData[i].ToArray();
            mesh.triangles = trianglesData[i].ToArray();
            mesh.uv = uvData[i].ToArray(); // Add UV data to the mesh
            mesh.RecalculateNormals();

            float xOffset = chunkIndex.x * width * s;
            float yOffset = chunkIndex.y * height * s;
            float zOffset = chunkIndex.z * depth * s;

            GameObject chunkMesh = new GameObject("chunkMesh");
            MeshFilter meshFilter = chunkMesh.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            string mat = "Materials/" + Substance.getById(i).name;
            Material meshMat = Resources.Load<Material>(mat);

            if (Substance.getById(i).state == State.SOLID)
            {
                chunkMesh.AddComponent<MeshCollider>();
            }
            MeshRenderer meshRenderer = chunkMesh.AddComponent<MeshRenderer>();
            meshRenderer.material = meshMat;
            //meshRenderer.material = Resources.Load<Material>("Shaders/SmoothVoxelMaterial");
            chunkMesh.transform.position = new Vector3(xOffset, yOffset, zOffset);
            meshData.Add(i, chunkMesh);
        }
    }


    private void createVoxelRepresentationAt(List<int> substanceIds, Voxel[,,] voxels, List<Vector3>[] verticesData, List<int>[] trianglesData, List<Vector2>[] uvData, Substance substance, int x, int y, int z)
    {
        List<Vector3> vertices;
        List<int> triangles;
        List<Vector2> uvs; // Add this line

        if (verticesData[substance.id] != null)
        {
            vertices = verticesData[substance.id];
            triangles = trianglesData[substance.id];
            uvs = uvData[substance.id]; // Add this line
        }
        else
        {
            substanceIds.Add(substance.id);
            vertices = new List<Vector3>();
            verticesData[substance.id] = vertices;
            triangles = new List<int>();
            trianglesData[substance.id] = triangles;
            uvs = new List<Vector2>(); // Add this line
            uvData[substance.id] =  uvs; // Add this line
        }

        int width = voxels.GetLength(0);
        int height = voxels.GetLength(1);
        int depth = voxels.GetLength(2);

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

            // Add UVs
            uvs.Add(faceUVs[0]);
            uvs.Add(faceUVs[1]);
            uvs.Add(faceUVs[2]);
            uvs.Add(faceUVs[3]);
        }

        if (y == (height - 1) || voxels[x, y + 1, z].substance.state == State.GAS) //Top
        {

            vertices.Add(new Vector3(xS + 0, yS + s, zS + 0));
            vertices.Add(new Vector3(xS + 0, yS + s, zS + s));
            vertices.Add(new Vector3(xS + s, yS + s, zS + s));
            vertices.Add(new Vector3(xS + s, yS + s, zS + 0));

            int count = vertices.Count - 4;

            triangles.Add(0 + count); triangles.Add(1 + count); triangles.Add(2 + count);
            triangles.Add(0 + count); triangles.Add(2 + count); triangles.Add(3 + count);

            // Add UVs
            uvs.Add(faceUVs[0]);
            uvs.Add(faceUVs[1]);
            uvs.Add(faceUVs[2]);
            uvs.Add(faceUVs[3]);
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

            // Add UVs
            uvs.Add(faceUVs[0]);
            uvs.Add(faceUVs[1]);
            uvs.Add(faceUVs[2]);
            uvs.Add(faceUVs[3]);
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

            // Add UVs
            uvs.Add(faceUVs[0]);
            uvs.Add(faceUVs[1]);
            uvs.Add(faceUVs[2]);
            uvs.Add(faceUVs[3]);
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

            // Add UVs
            uvs.Add(faceUVs[0]);
            uvs.Add(faceUVs[1]);
            uvs.Add(faceUVs[2]);
            uvs.Add(faceUVs[3]);
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

            // Add UVs
            uvs.Add(faceUVs[0]);
            uvs.Add(faceUVs[1]);
            uvs.Add(faceUVs[2]);
            uvs.Add(faceUVs[3]);
        }
    }

}
