using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkMesh
{
    readonly float s = Voxel.size;
    private Dictionary<int, GameObject> meshData = new Dictionary<int, GameObject>();


    public void DestroyMesh()
    {
        foreach (KeyValuePair<int, GameObject> entry in meshData)
        {
            Object.Destroy(entry.Value);
        }
    }

    public void GenerateMesh(Voxel[,,] voxels, int chunkIndexX, int chunkIndexY, int chunkIndexZ)
    {
        int width = voxels.GetLength(0);
        int height = voxels.GetLength(1);
        int depth = voxels.GetLength(2);

        Dictionary<int, List<Vector3>> verticesData = new Dictionary<int, List<Vector3>>();
        Dictionary<int, List<int>> trianglesData = new Dictionary<int, List<int>>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    createVoxelRepresentationAt(voxels, verticesData, trianglesData, x, y, z);
                }
            }
        }


        foreach (KeyValuePair<int, List<Vector3>> entry in verticesData)
        {
            Mesh mesh = new Mesh();

            mesh.vertices = entry.Value.ToArray();
            mesh.triangles = trianglesData[entry.Key].ToArray();
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
            chunkMesh.transform.position = new Vector3(xOffset, yOffset, zOffset);
            meshData.Add(entry.Key, chunkMesh);
        }
    }


    private void createVoxelRepresentationAt(Voxel[,,] voxels, Dictionary<int, List<Vector3>> verticesData, Dictionary<int, List<int>> trianglesData, int x, int y, int z)
    {
        int width = voxels.GetLength(0);
        int height = voxels.GetLength(1);
        int depth = voxels.GetLength(2);

        Substance substance = voxels[x, y, z].substance;

        List<Vector3> vertices;
        List<int> triangles;

        if (verticesData.ContainsKey(substance.id))
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

            if (y == (height - 1) || voxels[x, y + 1, z].substance.state == State.GAS) //Top
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

}
