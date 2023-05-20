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
                    if (y == 0 || voxels[x, y - 1, z].substance.name == "air") occluded = false;
                    if (y == (height - 1) || voxels[x, y + 1, z].substance.name == "air") occluded = false;

                    if (x == 0 || voxels[x - 1, y, z].substance.name == "air") occluded = false;
                    if (x == (width - 1) || voxels[x + 1, y, z].substance.name == "air") occluded = false;

                    if (z == 0 || voxels[x, y, z - 1].substance.name == "air") occluded = false;
                    if (z == (depth - 1) || voxels[x, y, z + 1].substance.name == "air") occluded = false;

                    if (voxels[x, y, z].substance.name != "air" && !occluded)
                    {
                        string mat = "Materials/" + voxels[x, y, z].substance.name;                         GameObject voxelRepresentation = CreateCube(Resources.Load<Material>(mat));
                        Vector3 pos = new Vector3(x + xOffset, y + yOffset, z + zOffset);
                        voxelRepresentation.transform.position = pos;
                        voxelMesh[x, y, z] = voxelRepresentation;
                    }
                }
            }
        }
    }


    GameObject CreateCube(Material cubeMaterial)     {         // Create a new GameObject         GameObject cube = new GameObject("Cube");          // Add a MeshFilter component to the GameObject. This component holds the mesh data.         MeshFilter meshFilter = cube.AddComponent<MeshFilter>();          // Create a cube mesh         Mesh mesh = new Mesh();          // Set vertices         mesh.vertices = new Vector3[] {             new Vector3(0, 0, 0),             new Vector3(1, 0, 0),             new Vector3(1, 1, 0),             new Vector3(0, 1, 0),             new Vector3(0, 1, 1),             new Vector3(1, 1, 1),             new Vector3(1, 0, 1),             new Vector3(0, 0, 1),         };          // Set triangles         mesh.triangles = new int[] {             0, 2, 1, //face front             0, 3, 2,             2, 3, 4, //face top             2, 4, 5,             1, 2, 5, //face right             1, 5, 6,             0, 7, 4, //face left             0, 4, 3,             5, 4, 7, //face back             5, 7, 6,             0, 6, 7, //face bottom             0, 1, 6         };          // Calculate normals to make Unity lighting work properly         mesh.RecalculateNormals();          // Assign the mesh to the MeshFilter component         meshFilter.mesh = mesh;          // Add a MeshRenderer component to the GameObject. This component uses the mesh data from the MeshFilter and applies a material to it.         MeshRenderer meshRenderer = cube.AddComponent<MeshRenderer>();          // Set the material on the MeshRenderer         meshRenderer.material = cubeMaterial;          // Add a BoxCollider component to the GameObject. This adds physical properties to the GameObject.         cube.AddComponent<BoxCollider>();          return cube;     } 

    private void OnDrawGizmos()     {          for (int x = 0; x < width; x++)         {             for (int y = 0; y < height; y++)             {                 for (int z = 0; z < depth; z++)                 {                     DrawVoxel(x, y, z);                 }             }         }     }     private void DrawVoxel(int x, int y, int z)     {         Gizmos.color = Color.green;         Vector3 position = new Vector3(x, y, z) * 1.0f;         Gizmos.DrawWireCube(position, Vector3.one * 1.0f);     }
}
