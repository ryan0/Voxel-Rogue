using System.Collections.Generic;
using UnityEngine;

public class FluidFlowSystem
{
    // Define active fluid voxels
    // Key - index of chunk
    // value - a list containing all active fluid voxels in chunk at key index
    private Dictionary<Vector3Int, HashSet<Voxel>> activeFluidVoxelsMap = new();

    const int MaxMotes = 5; // Max compresseble number of motes

    private  System.Random rng = new System.Random();

    public FluidFlowSystem()
    {

    }

    public void UpdateFluidFlow(HashSet<Chunk> activeChunks)
    {
        HashSet<Voxel> newlyActive = new();
        HashSet<Voxel> noLongerActive = new();

        //Initialize any uncreated active maps for chunks
        foreach (Chunk chunk in activeChunks)
        {
            if (!activeFluidVoxelsMap.ContainsKey(chunk.GetIndex()))
            {
                InitializeChunkInMap(chunk);
            }
        }

        //Flow logic and determine if regen mesh
        int flowCalls = 0;
        foreach (Chunk chunk in activeChunks)
        {
            HashSet<Voxel> activeFluidVoxelsForChunk = activeFluidVoxelsMap[chunk.GetIndex()];
            bool signalMeshRegen = false;

            foreach (Voxel v in activeFluidVoxelsForChunk)
            {
                signalMeshRegen = Flow(v, chunk, newlyActive, noLongerActive);
                flowCalls += 1;
            }

            if (signalMeshRegen)
            {
                chunk.SignalMeshRegen();
            }
        }
        Debug.Log(flowCalls);

        //remove no longer active
        foreach (Voxel v in noLongerActive)
        {
            Vector3Int chunkIndex = v.chunk.GetIndex();

            if (activeFluidVoxelsMap.ContainsKey(chunkIndex))
            {
                activeFluidVoxelsMap[chunkIndex].Remove(v);
            }
            else
            {
                InitializeChunkInMap(v.chunk);
                activeFluidVoxelsMap[chunkIndex].Remove(v);
            }
        }

        // Add newly active
        foreach (Voxel v in newlyActive)
        {
            Vector3Int chunkIndex = v.chunk.GetIndex();

            if (v.substance.id != Substance.air.id)
            {
                if (activeFluidVoxelsMap.ContainsKey(chunkIndex))
                {
                    activeFluidVoxelsMap[chunkIndex].Add(v);
                }
                else
                {
                    InitializeChunkInMap(v.chunk);
                    activeFluidVoxelsMap[chunkIndex].Add(v);
                }
            }
        }
    }

    private void InitializeChunkInMap(Chunk chunk)
    {
        HashSet<Voxel> activeFluidVoxelsForChunk = new();

        for (int x = 0; x < Chunk.width; x++)
        {
            for (int y = 0; y < Chunk.height; y++)
            {
                for (int z = 0; z < Chunk.depth; z++)
                {
                    Voxel v = chunk.GetVoxelAt(x, y, z);
                    if (v.substance.state == State.LIQUID)
                    {
                        activeFluidVoxelsForChunk.Add(v);
                    }
                }
            }
        }

        activeFluidVoxelsMap.Add(chunk.GetIndex(), activeFluidVoxelsForChunk);
    }


    private bool Flow(Voxel voxel, Chunk chunk, HashSet<Voxel> newlyActive, HashSet<Voxel> noLongerActive)
    {
        Substance fluidType = voxel.substance;

        int x = voxel.x;
        int y = voxel.y;
        int z = voxel.z;


        bool signalMeshRegen = false;
        Voxel[] adjacentVoxels = chunk.GetVoxelsAdjacentTo(x, y, z);
        Voxel voxelBelow = adjacentVoxels[4];


        if (voxelBelow != null && voxelBelow.substance.id == Substance.air.id) //if voxel below is air
        {
            voxelBelow.motes = voxel.motes;
            voxel.motes = 0;

            voxelBelow.substance = voxel.substance;
            voxel.substance = Substance.air;

            signalMeshRegen = true;

            noLongerActive.Add(voxel);
            addAdjacentFluidsToNewlyActive(adjacentVoxels, newlyActive);
        } 
        else if (voxelBelow != null && voxelBelow.substance.id == fluidType.id && voxelBelow.motes < MaxMotes) //if fluid is below and not at max pressure
        {
            TransferMotesBetweenFLuidVertically(voxel, voxelBelow);

            if(voxel.substance.id == Substance.air.id)
            {
                signalMeshRegen = true;

                noLongerActive.Add(voxel);
                addAdjacentFluidsToNewlyActive(adjacentVoxels, newlyActive);
            }
        }
        else if (voxel.motes > 1 && FlowHorizontally(voxel, fluidType, chunk, x, y, z)) //horizontal flow
        {
            addAdjacentFluidsToNewlyActive(adjacentVoxels, newlyActive);
            signalMeshRegen = true;
        }
        else
        {
            noLongerActive.Add(voxel);
        }
       

        return signalMeshRegen;
    }


    private void addAdjacentFluidsToNewlyActive(Voxel[] adjacentVoxels, HashSet<Voxel> newlyActive)
    {
        foreach (Voxel v in adjacentVoxels)
        {
            if (v != null && v.substance.state == State.LIQUID)
            {
                newlyActive.Add(v);
            }
        }
    }

    private bool FlowHorizontally(Voxel voxel, Substance fluidType, Chunk chunk, int x, int y, int z)
    {
        bool flow = false;

        Voxel[] horizontallyAdjacentVoxels = chunk.GetVoxelsHorizonatllyAdjacentTo(x, y, z);
        shuffleVoxelArray(horizontallyAdjacentVoxels);

        bool canFlow = true;
        while (canFlow)
        {
            canFlow = false;
            foreach (Voxel v in horizontallyAdjacentVoxels)
            {
                if (voxel.motes <= 1)
                {
                    break;
                }

                if (v != null && v.substance.id == Substance.air.id)
                {
                    voxel.motes -= 1;
                    v.motes = 1;
                    v.substance = fluidType;
                    flow = true;
                    canFlow = true;
                }
                else if (v != null && v.substance.id == fluidType.id && v.motes < MaxMotes && v.motes < voxel.motes)
                {
                    voxel.motes -= 1;
                    v.motes += 1;
                    flow = true;
                    canFlow = true;
                }
            }
        }

        return flow;
    }

    private void shuffleVoxelArray(Voxel[] voxelArray)
    {
        int n = voxelArray.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Voxel value = voxelArray[k];
            voxelArray[k] = voxelArray[n];
            voxelArray[n] = value;
        }
    }

    private void TransferMotesBetweenFLuidVertically(Voxel voxel, Voxel voxelBelow)
    {
        int totalMotes = voxelBelow.motes + voxel.motes;

        if (totalMotes <= MaxMotes)
        {
            voxel.motes = 0;
            voxel.substance = Substance.air;

            voxelBelow.motes = totalMotes;
        }
        else
        {
            voxelBelow.motes = MaxMotes;
            voxel.motes = totalMotes - MaxMotes;
        }
    }


}

