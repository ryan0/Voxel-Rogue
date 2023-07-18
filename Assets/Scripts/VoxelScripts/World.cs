using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public const int chunksX = 12;
    public const int chunksY = 4;
    public const int chunksZ = 12;

    [SerializeField]
    private GameObject player;

    private Chunk[,,] chunks = new Chunk[chunksX, chunksY, chunksZ];

    public Chunk[,,] Chunks => chunks;

    private const float substanceSystemInterval = .5f;
    private float substanceSystemTimer = 0.0f;

    private const float temperatureSystemInterval = 5.0f;
    private float temperatureSystemTimer = 0.5f;

    private const float fluidFlowSystemInterval = .1f;
    private float fluidFlowSystemTimer = 0.0f;


    private const float gasFlowSystemInterval = .5f;
    private float gasFlowSystemTimer = 0.0f;

    private const float wCycleystemInterval = 10f;
    private float wCycleSystemTimer = 0.0f;

    private const float fireInterval = 1f;
    private float fireTimer = 0.0f;


    FluidFlowSystem fluidFlowSystem;
    SubstanceInteractionSystem substanceInteractionSystem = new();
    TemperatureSystem temperatureSystem = new();
    GasFlowSystem gasSystem = new ();
    WaterCycleSystem waterCycleSystem = new ();
    FireManager fireManager = new ();

    NPCGenerator npcGen = new NPCGenerator ();

    public World()
    {
        fluidFlowSystem = new FluidFlowSystem();
    }

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
    
        foreach(TownData town in TownGeneration.worldTownsData){
            foreach(HouseData house in town.Houses){
                /// TO DO spawn npc prefab at house spawn location
                //spawn prefab
                //Resources.Load etc.
                //Create npc data
                //npc = npcGen.GenerateNPC("Name", 50, new PatrolBehavior(npcController of prefab))
                //Attach npc to npcController of prefab to link Npc data to the prefab
                /// END OF TO DO
            }
        }
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        HashSet<Chunk> activeChunks = getActiveChunks();
        //substanceSystemTimer += Time.deltaTime;
        //if(substanceSystemTimer >= substanceSystemInterval)
        //{
        //    substanceSystemTimer -= substanceSystemInterval;
        //   //this.substanceInteractionSystem.UpdateSubstances(activeChunks);
        //}

        //temperatureSystemTimer += Time.deltaTime;
        //if(temperatureSystemTimer >= temperatureSystemInterval)
        //{
        //    temperatureSystemTimer -= temperatureSystemInterval;
        //    //this.temperatureSystem.UpdateTemperatures(activeChunks);
        //}

        fluidFlowSystemTimer += Time.deltaTime;
        if (fluidFlowSystemTimer >= fluidFlowSystemInterval)
        {
            fluidFlowSystemTimer -= fluidFlowSystemInterval;
            this.fluidFlowSystem.UpdateFluidFlow(activeChunks);
        }

        //gasFlowSystemTimer += Time.deltaTime;
        //if (gasFlowSystemTimer >= gasFlowSystemInterval)
        //{
        //    gasFlowSystemTimer -= gasFlowSystemInterval;
        //    this.gasSystem.UpdateGasFlow(activeChunks);
        //}

        //wCycleSystemTimer += Time.deltaTime;
        //if(wCycleSystemTimer >= wCycleystemInterval)
        //{
        //    wCycleSystemTimer -= wCycleystemInterval;
        //    this.waterCycleSystem.UpdateWaterCycle(activeChunks);
        //}

        //fireTimer += Time.deltaTime;
        //if (fireTimer >= fireInterval)
        //{
        //    fireTimer -= fireInterval;
        //    this.fireManager.UpdateFires(activeChunks);
        //}

        npcGen.PerformAllNPCActions();




    }

    public Chunk GetChunkAt(Vector3Int pos)
    {
        return chunks[pos.x, pos.y, pos.z];
    }

    //TODO Delete this method
    public Voxel GetVoxelAt(Vector3Int voxelPos)
    {
        Vector3Int chunkPos = new(
            voxelPos.x / Chunk.width, 
            voxelPos.y / Chunk.height, 
            voxelPos.z / Chunk.depth
        );

        Vector3Int relativeVoxelPos = new(
            voxelPos.x % Chunk.width,
            voxelPos.y % Chunk.height,
            voxelPos.z % Chunk.depth
        );

        return GetVoxelAt(chunkPos, relativeVoxelPos);
    }

    public Voxel GetVoxelAt(Vector3Int chunkPos, Vector3Int voxelPos)
    {
        return GetChunkAt(chunkPos).GetVoxelAt(voxelPos);
    }

    public Chunk GetChunkPlayerIsIn()
    {
        Vector3Int chunkPos = WorldCoordToChunkCoord(player.transform.position);
        return GetChunkAt(chunkPos);
    }

    private Vector3Int WorldCoordToChunkCoord(Vector3 worldCoord)
    {
        int playerX = (int)(worldCoord.x / (Chunk.width * Voxel.size));
        int playerY = (int)(worldCoord.y / (Chunk.height * Voxel.size));
        int playerZ = (int)(worldCoord.z / (Chunk.depth * Voxel.size));
        return new Vector3Int(playerX, playerY, playerZ);
    }

    public HashSet<Chunk> getActiveChunks()
    {
        const int activeAreaRadiusX = 1;
        const int activeAreaRadiusY = 2;
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

        //Debug.Log("hit Voxel: " + voxelX + ", " + voxelY + ", " + voxelZ);
        //Debug.Log("in Chunk: " + chunkX + ", " + chunkY + ", " + chunkZ);


        //Spawn debris logic
        Voxel[,,] voxels = chunks[chunkX, chunkY, chunkZ].getVoxels();///debug debug debug
        Substance substance = voxels[voxelX, voxelY, voxelZ].substance;///Debug debug dbeug
        chunks[chunkX, chunkY, chunkZ].destroyVoxelAt(voxelX, voxelY, voxelZ);
        spawnDebrisAt(substance,coord, 3);//DEBUG DEBUG DEBUG*/
        //terrainData[coord.x, coord.y, coord.z] = Substance.air;
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
        //terrainData[coord.x, coord.y, coord.z] = substance;

    }

    public void setFireVoxel(Vector3Int coord)
    {
        int chunkX = coord.x / Chunk.width;
        int chunkY = coord.y / Chunk.height;
        int chunkZ = coord.z / Chunk.depth;

        int voxelX = coord.x - (chunkX * Chunk.width);
        int voxelY = coord.y - (chunkY * Chunk.height);
        int voxelZ = coord.z - (chunkZ * Chunk.depth);

        //Debug.Log("hit Voxel: " + voxelX + ", " + voxelY + ", " + voxelZ);
        //Debug.Log("in Chunk: " + chunkX + ", " + chunkY + ", " + chunkZ);


        //Set fire
        Voxel[,,] voxels = chunks[chunkX, chunkY, chunkZ].getVoxels();///debug debug debug
        Voxel voxel = voxels[voxelX, voxelY, voxelZ];//.SetOnFire(new Fire(voxels[voxelX, voxelY, voxelZ]));
        fireManager.StartFire(voxel);
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


    public static Vector3Int WorldCoordToVoxelCoord(Vector3 worldCoord)
    {
        int voxelX = Mathf.FloorToInt(worldCoord.x / Voxel.size);
        int voxelY = Mathf.FloorToInt(worldCoord.y / Voxel.size);
        int voxelZ = Mathf.FloorToInt(worldCoord.z / Voxel.size);

        return new Vector3Int(voxelX, voxelY, voxelZ);
    }

  /* public static Vector3 VoxelCoordToWorldCoord(Vector3Int voxelCoord)
    {
        float worldX = voxelCoord.x * Voxel.size;
        float worldY = voxelCoord.y * Voxel.size;
        float worldZ = voxelCoord.z * Voxel.size;

        return new Vector3(worldX, worldY, worldZ);
    }*/

    public static Vector3 VoxelCoordToWorldCoord(Vector3Int voxelPosition)
    {
        // Assumes each voxel has a size of 1 unit
        float worldX = voxelPosition.x * Voxel.size + Voxel.size / 2f; // add 0.5 to get center of voxel
        float worldY = voxelPosition.y * Voxel.size + Voxel.size / 2f;
        float worldZ = voxelPosition.z * Voxel.size + Voxel.size / 2f;

        return new Vector3(worldX, worldY, worldZ);
    }


    public static bool IsVoxelInBounds(Vector3Int voxelPosition)
    {
        return voxelPosition.x >= 0 && voxelPosition.x < chunksX * Chunk.width &&
               voxelPosition.y >= 0 && voxelPosition.y < chunksY * Chunk.height &&
               voxelPosition.z >= 0 && voxelPosition.z < chunksZ * Chunk.depth;
    }

}
