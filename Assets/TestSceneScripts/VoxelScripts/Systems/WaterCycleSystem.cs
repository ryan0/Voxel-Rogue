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
        foreach (Chunk chunk in activeChunks)
        {
            Voxel[,,] voxels = chunk.getVoxels();
            for (int x = 0; x < Chunk.width; x++)
            {
                for (int z = 0; z < Chunk.depth; z++)
                {
                    for (int y = Chunk.height - 1; y >= 0; y--) // Start from the top and go down
                    {
                        Voxel voxel = voxels[x, y, z];
                        if (voxel.substance.state == State.LIQUID)
                        {
                            Voxel voxelAbove = y < Chunk.height - 1 ? voxels[x, y + 1, z] : null;
                            if (voxelAbove != null && voxelAbove.substance.GetGasForm() == voxel.substance)
                            {
                                // Transfer 1 mote to the gas above
                                voxel.motes -= 1;
                                voxelAbove.motes += 1;

                                // If the water voxel reaches 0, it should disappear
                                if (voxel.motes == 0)
                                {
                                    voxel.substance = Substance.air;
                                }

                                chunk.SignalMeshRegen();
                            }
                        }
                    }
                }
            }
        }
    }
}
