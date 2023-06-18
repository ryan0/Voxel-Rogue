using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterCycleSystem
{
    private HashSet<Chunk> activeChunks;
    public int maxMotes = 10;

    public WaterCycleSystem()
    {
        activeChunks = new HashSet<Chunk>();
    }

    public void UpdateWaterCycle(HashSet<Chunk> _activeChunks)
    {
        this.activeChunks = _activeChunks;
        CondenseGas();
        EvaporateLiquid();
    }

    private void CondenseGas()
    {
        foreach (Chunk chunk in activeChunks)
        {
            Voxel[,,] voxels = chunk.getVoxels();
            for (int x = 0; x < Chunk.width; x++)
            {
                for (int y = 0; y < Chunk.height; y++)
                {
                    for (int z = 0; z < Chunk.depth; z++)
                    {
                        Voxel voxel = voxels[x, y, z];
                        if (voxel.substance.state == State.GAS && voxel.motes >= 10) // You need to implement isGas() method
                        {
                            Debug.Log("Gas has more than 10 motes");
                            Substance liquidForm = voxel.substance.GetLiquidForm();
                            if (liquidForm != null)
                            {
                                voxel.substance = liquidForm;
                                chunk.SignalMeshRegen();
                            }
                        }
                    }
                }
            }
        }
    }

    private void EvaporateLiquid()
    {
        foreach (Chunk currentChunk in activeChunks)//current chunk
        {
            //get above chunks
            Chunk[] aboveChunks;
            int count = 0;
            foreach (Chunk c in activeChunks)
            {
                if (c.yIndex >= currentChunk.yIndex && c.xIndex == currentChunk.xIndex && c.zIndex == currentChunk.zIndex)
                {
                    count++;
                }
            }
            //Debug.Log(count);
            aboveChunks = new Chunk[count];
            count = 0;
            foreach (Chunk c in activeChunks)
            {
                if (c.yIndex >= currentChunk.yIndex && c.xIndex == currentChunk.xIndex && c.zIndex == currentChunk.zIndex)
                {
                    aboveChunks[count] = c;
                    count++;
                }
            }
            System.Array.Sort(aboveChunks, new ChunkComparer());
  
            Voxel[,,] voxels = currentChunk.getVoxels();
            for (int x = 0; x < Chunk.width; x++)
            {
                for (int z = 0; z < Chunk.depth; z++)
                {
                    // Start from the top of the CURRENT chunk and go down
                    for (int y = Chunk.height - 1; y >= 0; y--)
                    {
                        //Chunk currentChunk = currentChunk.world.getChunks()[chunk.xIndex, chunk.yIndex, chunk.zIndex];
                        if (currentChunk != null)
                        {
                            Voxel voxel = currentChunk.GetVoxel(x, y, z);
                            if (voxel.substance.state == State.LIQUID)
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
                                            voxelAbove = aboveChunk.GetVoxel(x, upperY, z);
                                            int compareUpperY = upperY + (Chunk.height* aboveChunk.yIndex);
                                            int compareCurrentY = y + (Chunk.height * currentChunk.yIndex);
                                            if ((aboveChunk == currentChunk && compareUpperY <= compareCurrentY))
                                            {
                                                //do nothing, this is below in the current chunk
                                            }
                                            else if (!(voxelAbove.substance == Substance.air || voxelAbove.substance == voxel.substance.GetGasForm()))
                                            {
                                                //Debug.Log("Not clear path " +  voxelAbove.substance.name + " blocking " + voxel.substance.name);
                                                isClearPath = false;
                                                break;
                                            }
                                            else if(voxelAbove.substance == voxel.substance.GetGasForm() && compareUpperY <= GasFlowSystem.MAX_GAS_HEIGHT) //TO DO!! later check for both air and gasform
                                            {
                                                Debug.Log("break: voxelAbove.substance.GetLiquidForm " + voxelAbove.substance.GetLiquidForm().name + "voxel sub " + voxel.substance);
                                                break;
                                            }
                                            //else if voxelAbove is air and AT EXACTLY CLOUD HEIGHT, e.g. 128 
                                        }
                                    }

                                }
                                //Debug.Log("clearPath " + isClearPath);
                                //Debug.Log("voxelAbove.substance.GetLiquidForm " + voxelAbove.substance.GetLiquidForm().name + "voxel sub "+ voxel.substance);

                                if (isClearPath && voxelAbove != null && voxelAbove.substance.GetLiquidForm() == voxel.substance)
                                {
                                    // Transfer 1 mote to the gas above
                                    voxel.motes -= 3;
                                    voxelAbove.motes += 3;
                                    Debug.Log("transfer mote");


                                    // If the water voxel reaches 0, it should disappear
                                    if (voxel.motes == 0)
                                    {
                                        voxel.substance = Substance.air;
                                        Debug.Log("evaporate");

                                    }

                                    currentChunk.SignalMeshRegen();
                                }
                                else Debug.Log("cant transfer mote");


                            }
                        }
                    }
                }
            }
        }
    }

}
