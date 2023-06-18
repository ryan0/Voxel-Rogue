using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public const int chunksX = 12;
    public const int chunksY = 12;
    public const int chunksZ = 12;

    [SerializeField]
    private GameObject player;

    private Chunk[,,] chunks = new Chunk[chunksX, chunksY, chunksZ];
    public Chunk[,,] getChunks()
    {
        return chunks;
    }

    private const float substanceSystemInterval = 4.0f;
    private float substanceSystemTimer = 0.0f;

    private const float temperatureSystemInterval = 5.0f;
    private float temperatureSystemTimer = 0.5f;

    private const float fluidFlowSystemInterval = .1f;
    private float fluidFlowSystemTimer = 0.0f;


    private const float gasFlowSystemInterval = .2f;
    private float gasFlowSystemTimer = 0.0f;

    private const float wCycleystemInterval = 20f;
    private float wCycleSystemTimer = 0.0f;


    FluidFlowSystem fluidFlowSystem = new ();
    SubstanceInteractionSystem substanceInteractionSystem = new();
    TemperatureSystem temperatureSystem = new();
    GasFlowSystem gasSystem = new ();
    WaterCycleSystem waterCycleSystem = new ();


    // Start is called before the first frame update
    void Start()
    {
        Substance[,,] terrainData = WorldGeneration.GenerateTerrain();

        for (int x = 0; x < chunksX; x++)
        {
            for (int y = 0; y < chunksY; y++)
            {
                for (int z = 0; z < chunksZ; z++)
                {
                    float biomeTemp = 70.0f;

                    if (x > 5 && z > 5) biomeTemp = 0.0f;

                    chunks[x, y, z] = Chunk.CreateChunk(x, y, z, terrainData, biomeTemp, this);
                }
            }
        }

        for (int x = 0; x < chunksX; x++)
        {
            for (int y = 0; y < chunksY; y++)
            {
                for (int z = 0; z < chunksZ; z++)
                {
                    Chunk chunk = chunks[x, y, z];
                    chunk.neighbors = new Chunk[6];
                    // Set the neighbors
                    //Debug.Log("Setting neighbours for chunk at " + x + ", " + y + ", " + z);
                    chunk.northNeighbour = (z < chunksZ - 1) ? chunks[x, y, z + 1] : null;
                    chunk.southNeighbour = (z > 0) ? chunks[x, y, z - 1] : null;
                    chunk.eastNeighbour = (x < chunksX - 1) ? chunks[x + 1, y, z] : null;
                    chunk.westNeighbour = (x > 0) ? chunks[x - 1, y, z] : null;
                    chunk.topNeighbour = (y < chunksY - 1) ? chunks[x, y + 1, z] : null;
                    chunk.bottomNeighbour = (y > 0) ? chunks[x, y - 1, z] : null;
                    for (int i = 0; i < 6; i++)
                    {
                        if (i == 0) chunk.neighbors[i] = chunk.northNeighbour;
                        if (i == 1) chunk.neighbors[i] = chunk.southNeighbour;
                        if (i == 2) chunk.neighbors[i] = chunk.eastNeighbour;
                        if (i == 3) chunk.neighbors[i] = chunk.westNeighbour;
                        if (i == 4) chunk.neighbors[i] = chunk.topNeighbour;
                        if (i == 5) chunk.neighbors[i] = chunk.bottomNeighbour;
                    }

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
            //this.substanceInteractionSystem.UpdateSubstances(getActiveChunks());
        }

        temperatureSystemTimer += Time.deltaTime;
        if(temperatureSystemTimer >= temperatureSystemInterval)
        {
            temperatureSystemTimer -= temperatureSystemInterval;
            //this.temperatureSystem.UpdateTemperatures(getActiveChunks());
        }

        fluidFlowSystemTimer += Time.deltaTime;
        if (fluidFlowSystemTimer >= fluidFlowSystemInterval)
        {
            fluidFlowSystemTimer -= fluidFlowSystemInterval;
            this.fluidFlowSystem.UpdateFluidFlow(getActiveChunks());
        }

        gasFlowSystemTimer += Time.deltaTime;
        if (gasFlowSystemTimer >= gasFlowSystemInterval)
        {
            gasFlowSystemTimer -= gasFlowSystemInterval;
            this.gasSystem.UpdateGasFlow(getActiveChunks());
        }

        wCycleSystemTimer += Time.deltaTime;
        if(wCycleSystemTimer >= wCycleystemInterval)
        {
            wCycleSystemTimer -= wCycleystemInterval;
            this.waterCycleSystem.UpdateWaterCycle(getActiveChunks());
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

    public HashSet<Chunk> getActiveChunks()
    {
        const int activeAreaRadiusX = 1;
        const int activeAreaRadiusY = 1;
        const int activeAreaRadiusZ = 1;

        Vector3 playerPosition = player.transform.position * (1 / Voxel.size);
        int playerX = (int)(playerPosition.x / Chunk.width);
        int playerY = (int)(playerPosition.y / Chunk.height);
        int playerZ = (int)(playerPosition.z / Chunk.depth);

        HashSet<Chunk> activeChunks = new();

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


        //Spawn debris logic
        Voxel[,,] voxels = chunks[chunkX, chunkY, chunkZ].getVoxels();///debug debug debug
        Substance substance = voxels[voxelX, voxelY, voxelZ].substance;///Debug debug dbeug
        chunks[chunkX, chunkY, chunkZ].destroyVoxelAt(voxelX, voxelY, voxelZ);
        spawnDebrisAt(substance,coord, 3);//DEBUG DEBUG DEBUG*/
        //end of spawn debris chunk
    }

    public void spawnVoxelAt(Vector3Int coord, Substance substance, int mote)
    {
        int chunkX = coord.x / Chunk.width;
        int chunkY = coord.y / Chunk.height;
        int chunkZ = coord.z / Chunk.depth;

        int voxelX = coord.x - (chunkX * Chunk.width);
        int voxelY = coord.y - (chunkY * Chunk.height);
        int voxelZ = coord.z - (chunkZ * Chunk.depth);

        // Access the appropriate chunk
        Chunk targetChunk = chunks[chunkX, chunkY, chunkZ];
        // Use a method in the Chunk class to create a new voxel at the specified local position
        targetChunk.createVoxelAt(voxelX, voxelY, voxelZ, substance, mote);
    }

    public void spawnDebrisAt(Substance substance, Vector3Int coord, int nChunks)
    {
        string prefabName = substance.name + "_debris";
        // Load the debris prefab
        GameObject debrisPrefab = Resources.Load<GameObject>(prefabName);

        if (debrisPrefab == null)
        {
            Debug.LogError("Debris prefab not found in Resources folder "+prefabName);
            return;
        }

        // Convert the voxel coordinates to world coordinates
        Vector3 worldCoord = new Vector3(coord.x * Voxel.size, coord.y * Voxel.size, coord.z * Voxel.size);

        for (int i = 0; i < nChunks; i++)
        {
            // Instantiate the debris prefab
            GameObject debris = Instantiate(debrisPrefab, worldCoord, Quaternion.identity);
            //initialize PickupAble fields if available
            PickupAble itemData;
            if(itemData = debris.GetComponent<PickupAble>())
            {
                itemData.substance = substance;
            }

        }
    }

    public void HighlightAdjVoxel(Vector3Int coord)
    {
        //Material highlightMaterial = Resources.Load("Material/highlight.mat", typeof(Material)) as Material;
        // Determine the chunk that the voxel is in
        int chunkX = coord.x / Chunk.width;
        int chunkY = coord.y / Chunk.height;
        int chunkZ = coord.z / Chunk.depth;

        // Determine the local position of the voxel within the chunk
        int voxelX = coord.x - (chunkX * Chunk.width);
        int voxelY = coord.y - (chunkY * Chunk.height);
        int voxelZ = coord.z - (chunkZ * Chunk.depth);

        //Spawn debris logic
        //Voxel[,,] voxels = chunks[chunkX, chunkY, chunkZ].getVoxels();///debug debug debug
        //Substance substance = voxels[voxelX, voxelY, voxelZ].substance;///Debug debug dbeug
        //Voxel voxel = voxels[voxelX, voxelY, voxelZ];
        // If the voxel is null or already highlighted, return
        //if (voxel == null || voxel.substance == Substance.debug) return;
        //Call chunk highlightAdjVoxels
        chunks[chunkX, chunkY, chunkZ].highlightAdjVoxels(voxelX, voxelY, voxelZ);
        //voxel.substance = Substance.debug;
    }


}
