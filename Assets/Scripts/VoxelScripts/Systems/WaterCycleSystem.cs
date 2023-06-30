using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterCycleSystem
{
    private HashSet<Chunk> activeChunks;
    public static int PRECIPITATION_THRESHOLD = 100;// MAX STEAM BEFORE PRECIPITATION
    public static int PRECIPITATION_AMOUNT = 100;//
    ChunkComparer chunkCompare;
    private Dictionary<Chunk, int> chunkUpdateIndices = new Dictionary<Chunk, int>();//cycling index update for optimization


    public WaterCycleSystem()
    {
        activeChunks = new HashSet<Chunk>();
        chunkCompare = new ChunkComparer();
    }

    private int updateCounter = 0;

    public void UpdateWaterCycle(HashSet<Chunk> _activeChunks)
    {
        this.activeChunks = _activeChunks;
        if (updateCounter % 5 == 0)//call Evaporate Xfraction as much as condense
        {
            EvaporateLiquid();
        }
        else CondenseGas();
        updateCounter++;
    }


    private void CondenseGas()
    {
        //Debug.Log("Condense");
        foreach (Chunk chunk in activeChunks)
        {
            //////////////////////////////
            /// ////////TO DO OPTIMIZATION CYLCING; DONE
            /// 
            ///
            chunkUpdateIndices.TryGetValue(chunk, out int yIndex);
            Voxel[,,] voxels = chunk.getVoxels();
            for (int x = 0; x < Chunk.width; x++)
            {
                //for (int y = 0; y < Chunk.height; y++)
                {
                    for (int z = 0; z < Chunk.depth; z++)
                    {
                        Voxel voxel = voxels[x, yIndex, z];
                        if (voxel.substance.state == State.GAS && voxel.motes >= PRECIPITATION_THRESHOLD) // You need to implement isGas() method
                        {
                            ///////Debug.Log("Gas has more than PRECIPITATION_THRESHOLD motes");
                            Substance liquidForm = voxel.substance.GetLiquidForm();
                            if (liquidForm != null)
                            {
                                //voxel.substance = liquidForm;
                                Voxel belowV = chunk.GetVoxelsAdjacentTo(x, yIndex, z)[4] ;//bottom voxel
                                belowV.substance = liquidForm;
                                belowV.motes = PRECIPITATION_AMOUNT;
                                voxel.motes -= PRECIPITATION_AMOUNT;
                                chunk.SignalMeshRegen();
                                //Debug.Log("precipitating " + PRECIPITATION_AMOUNT);
                                // If the water voxel reaches 0, it should disappear
                                if (voxel.motes == 0)
                                {
                                    voxel.substance = Substance.air;
                                    //Debug.Log("evaporate");

                                }
                            }
                        }
                    }
                }
            }
            yIndex = (yIndex + 1) % Chunk.height;
            chunkUpdateIndices[chunk] = yIndex;
        }
    }

    private void EvaporateLiquid()//only evaporate if not falling
    {
        //Debug.Log("Evaporate");
        foreach (Chunk currentChunk in activeChunks)//current chunk
        {
            chunkUpdateIndices.TryGetValue(currentChunk, out int yIndex);
            //get above chunks
            Chunk[] aboveChunks;
            int count = 0;
            foreach (Chunk c in activeChunks)
            {
                if (c.index.y >= currentChunk.index.y && c.index.x == currentChunk.index.x && c.index.z == currentChunk.index.z)
                {
                    count++;
                }
            }
            //Debug.Log(count);
            aboveChunks = new Chunk[count];
            count = 0;
            foreach (Chunk c in activeChunks)
            {
                if (c.index.y >= currentChunk.index.y && c.index.x == currentChunk.index.x && c.index.z == currentChunk.index.z)
                {
                    aboveChunks[count] = c;
                    count++;
                }
            }
            System.Array.Sort(aboveChunks, chunkCompare);
  
            Voxel[,,] voxels = currentChunk.getVoxels();
            for (int x = 0; x < Chunk.width; x++)
            {
                for (int z = 0; z < Chunk.depth; z++)
                {
                    // Start from the top of the CURRENT chunk and go down
                    //for (int y = Chunk.height - 1; y >= 0; y--)
                    {
                        //Chunk currentChunk = currentChunk.world.getChunks()[chunk.xIndex, chunk.yIndex, chunk.zIndex];
                        if (currentChunk != null)
                        {
                            Voxel voxel = currentChunk.GetVoxelAt(x, yIndex, z);
                            if (voxel.substance.state == State.LIQUID && voxel.getNeighbors()[4].substance.id != Substance.air.id && voxel.substance.GetGasForm()!=null)//don't evap falling blocks
                            {
                                bool isClearPath = true;
                                Voxel voxelAbove = null;

                                // Check the voxels above in the chunks including current and above current for a clear path
                                for(int chunkCounter = 0; chunkCounter<count; chunkCounter++)
                                {
                                    Chunk aboveChunk = aboveChunks[chunkCounter];
                                    if (aboveChunk != null)
                                    {
                                        //Debug.Log("above chunk");
                                        for (int upperY = 0; upperY < Chunk.height; upperY++)
                                        {
                                            voxelAbove = aboveChunk.GetVoxelAt(x, upperY, z);
                                            int compareUpperY = upperY + (Chunk.height* aboveChunk.index.y);
                                            int compareCurrentY = yIndex + (Chunk.height * currentChunk.index.y);
                                            if ((aboveChunk.index.y <= currentChunk.index.y && compareUpperY <= compareCurrentY))
                                            {
                                                //do nothing, this is below in the current chunk
                                            }
                                            else if (!(voxelAbove.substance == Substance.air || voxelAbove.substance == voxel.substance.GetGasForm()))
                                            {
                                                //Debug.Log("Not clear path " +  voxelAbove.substance.name + " blocking " + voxel.substance.name);
                                                isClearPath = false;
                                                break;
                                            }
                                            else if (voxelAbove.substance == voxel.substance.GetGasForm() && compareUpperY <= GasFlowSystem.MAX_GAS_HEIGHT) //TO DO!! later check for both air and gasform
                                            {
                                                //Debug.Log("break: voxelAbove.substance.GetLiquidForm " + voxelAbove.substance.GetLiquidForm().name + "voxel sub " + voxel.substance);
                                                break;
                                            }
                                            else if (isClearPath && voxelAbove.substance == Substance.air && compareUpperY == GasFlowSystem.MAX_GAS_HEIGHT){
                                                //else if voxelAbove is air and AT EXACTLY CLOUD HEIGHT, e.g. 128 
                                                voxelAbove.substance = Substance.steam;
                                                voxelAbove.motes = 1;
                                                aboveChunk.SignalMeshRegen();
                                                //Debug.Log("Making cloud");
                                                isClearPath = false;    
                                                break;
                                            }
                                        }
                                    }

                                }
                                //Debug.Log("clearPath " + isClearPath);
                                //Debug.Log("voxelAbove.substance.GetLiquidForm " + voxelAbove.substance.GetLiquidForm().name + "voxel sub "+ voxel.substance);

                                if (isClearPath && voxelAbove != null && voxelAbove.substance.GetLiquidForm() == voxel.substance)
                                {
                                    // Transfer 1 mote to the gas above
                                    voxel.motes -= 1;
                                    voxelAbove.motes += 1;
                                    ////////Debug.Log("transfer mote");


                                    // If the water voxel reaches 0, it should disappear
                                    if (voxel.motes == 0)
                                    {
                                        voxel.substance = Substance.air;
                                        //Debug.Log("evaporate");

                                    }

                                    currentChunk.SignalMeshRegen();
                                }
                                //else Debug.Log("cant transfer mote");


                            }
                        }
                    }
                }
            }
            yIndex = (yIndex + 1) % Chunk.height;
            chunkUpdateIndices[currentChunk] = yIndex;
        }
    }

}
