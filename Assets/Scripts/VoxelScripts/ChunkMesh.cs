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

    public void GenerateMesh(Voxel[,,] voxels, int chunkIndexX, int chunkIndexY, int chunkIndexZ)
    {
        int width = voxels.GetLength(0);
        int height = voxels.GetLength(1);
        int depth = voxels.GetLength(2);

        Dictionary<int, List<Vector3>> verticesData = new Dictionary<int, List<Vector3>>();
        Dictionary<int, List<int>> trianglesData = new Dictionary<int, List<int>>();
        Dictionary<int, List<Vector2>> uvData = new Dictionary<int, List<Vector2>>(); // Add this line

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    createVoxelRepresentationAt(voxels, verticesData, trianglesData, uvData, x, y, z); // Pass uvData here                }
                }
            }
        }


        foreach (KeyValuePair<int, List<Vector3>> entry in verticesData)
        {
            Mesh mesh = new Mesh();

            mesh.vertices = entry.Value.ToArray();
            mesh.triangles = trianglesData[entry.Key].ToArray();
            mesh.uv = uvData[entry.Key].ToArray(); // Add UV data to the mesh
            mesh.RecalculateNormals();

            float xOffset = chunkIndexX * width * s;
            float yOffset = chunkIndexY * height * s;
            float zOffset = chunkIndexZ * depth * s;

            GameObject chunkMesh = new GameObject("chunkMesh");
            MeshFilter meshFilter = chunkMesh.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            string mat = "Materials/" + Substance.getById(entry.Key).name;
            Material meshMat = Resources.Load<Material>(mat);

            if (Substance.getById(entry.Key).state == State.SOLID)
            {
                chunkMesh.AddComponent<MeshCollider>();
            }
            MeshRenderer meshRenderer = chunkMesh.AddComponent<MeshRenderer>();
            meshRenderer.material = meshMat;
            //meshRenderer.material = Resources.Load<Material>("Shaders/SmoothVoxelMaterial");
            chunkMesh.transform.position = new Vector3(xOffset, yOffset, zOffset);
            meshData.Add(entry.Key, chunkMesh);
        }
    }



    private void createVoxelRepresentationAt(Voxel[,,] voxels, Dictionary<int, List<Vector3>> verticesData, Dictionary<int, List<int>> trianglesData, Dictionary<int, List<Vector2>> uvData, int x, int y, int z)
    {
        int width = voxels.GetLength(0);
        int height = voxels.GetLength(1);
        int depth = voxels.GetLength(2);

        Substance substance = voxels[x, y, z].substance;

        List<Vector3> vertices;
        List<int> triangles;
        List<Vector2> uvs; // Add this line

        if (verticesData.ContainsKey(substance.id))
        {
            vertices = verticesData[substance.id];
            triangles = trianglesData[substance.id];
            uvs = uvData[substance.id]; // Add this line
        }
        else
        {
            vertices = new List<Vector3>();
            verticesData.Add(substance.id, vertices);
            triangles = new List<int>();
            trianglesData.Add(substance.id, triangles);
            uvs = new List<Vector2>(); // Add this line
            uvData.Add(substance.id, uvs); // Add this line
        }

        if (!uvData.ContainsKey(substance.id))
        {
            uvs = new List<Vector2>();
            uvData.Add(substance.id, uvs);
        }
        else
        {
            uvs = uvData[substance.id];
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

}
