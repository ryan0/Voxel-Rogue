using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public const int chunksX = 16;
    public const int chunksY = 4;
    public const int chunksZ = 16;

    [SerializeField]
    private GameObject player;

    private Chunk[,,] chunks = new Chunk[chunksX, chunksY, chunksZ];

    private const float substanceSystemInterval = 1.0f;
    private float substanceSystemTimer = 0.0f;

    private const float temperatureSystemInterval = 5.0f;
    private float temperatureSystemTimer = 0.5f;

    SubstanceInteractionSystem substanceInteractionSystem = new();
    TemperatureSystem temperatureSystem = new();

    // Start is called before the first frame update
    void Start()
    {
        Substance[,,] terrainData = WorldGeneration.genTerrain();

        for (int x = 0; x < chunksX; x++)
        {
            for (int y = 0; y < chunksY; y++)
            {
                for (int z = 0; z < chunksZ; z++)
                {
                    float biomeTemp = 70.0f;

                    if (x > 5 && z > 5) biomeTemp = 0.0f;

                    chunks[x, y, z] = Chunk.CreateChunk(x, y, z, terrainData, biomeTemp);
                }
            }
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        substanceSystemTimer += Time.deltaTime;
        if(substanceSystemTimer >= substanceSystemInterval)
        {
            substanceSystemTimer -= substanceSystemInterval;
            this.substanceInteractionSystem.UpdateSubstances(getActiveChunks());
        }

        temperatureSystemTimer += Time.deltaTime;
        if(temperatureSystemTimer >= temperatureSystemInterval)
        {
            temperatureSystemTimer -= temperatureSystemInterval;
            this.temperatureSystem.UpdateTemperatures(getActiveChunks());
        }
        
    }
    
    public Chunk getChunkAt(Vector3Int pos)
    {
        Debug.Log(pos.x + " " + pos.y + " " + pos.z);
        return chunks[pos.x, pos.y, pos.z];
    }

    public Chunk getChunkPlayerIsIn()
    {
        Vector3 playerPosition = player.transform.position * (1 / Voxel.size);

        int playerX = (int) (playerPosition.x / Chunk.width);
        int playerY = (int) (playerPosition.y / Chunk.height);
        int playerZ = (int) (playerPosition.z / Chunk.depth);

        return getChunkAt(new Vector3Int(playerX, playerY, playerZ));
    }

    public List<Chunk> getActiveChunks()
    {
        const int activeAreaRadiusX = 1;
        const int activeAreaRadiusY = 2;
        const int activeAreaRadiusZ = 1;

        Vector3 playerPosition = player.transform.position * (1 / Voxel.size);
        int playerX = (int)(playerPosition.x / Chunk.width);
        int playerY = (int)(playerPosition.y / Chunk.height);
        int playerZ = (int)(playerPosition.z / Chunk.depth);

        List<Chunk> activeChunks = new();

        int xStart = playerX - activeAreaRadiusX > 0 ? playerX - activeAreaRadiusX : 0;
        int xMax = playerX + activeAreaRadiusX < chunksX - 1 ? playerX + activeAreaRadiusX : chunksX - 1;

        int yStart = playerY - activeAreaRadiusY > 0 ? playerY - activeAreaRadiusY : 0;
        int yMax = playerY + activeAreaRadiusY < chunksY - 1 ? playerY + activeAreaRadiusY : chunksY - 1;

        int zStart = playerZ - activeAreaRadiusZ > 0 ? playerZ - activeAreaRadiusZ : 0;
        int zMax = playerZ + activeAreaRadiusZ < chunksZ - 1 ? playerZ + activeAreaRadiusZ : chunksZ - 1;


        for (int x = xStart; x <= xMax; x++)
        {
            for (int y = yStart; y <= yMax; y++)
            {
                for (int z = zStart; z <= zMax; z++)
                {
                    activeChunks.Add(chunks[x, y, z]);
                }
            }
        }

        return activeChunks;
    }

    public void destroyVoxelAt(Vector3Int coord)
    {
        int chunkX = coord.x / Chunk.width;
        int chunkY = coord.y / Chunk.height;
        int chunkZ = coord.z / Chunk.depth;

        int voxelX = coord.x - (chunkX * Chunk.width);
        int voxelY = coord.y - (chunkY * Chunk.height);
        int voxelZ = coord.z - (chunkZ * Chunk.depth);

        Debug.Log("hit Voxel: " + voxelX + ", " + voxelY + ", " + voxelZ);
        Debug.Log("in Chunk: " + chunkX + ", " + chunkY + ", " + chunkZ);

        chunks[chunkX, chunkY, chunkZ].destroyVoxelAt(voxelX, voxelY, voxelZ);
    }
}
