using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FluidFlowSystem
{
    public void UpdateFluidFlow(List<Chunk> activeChunks)
    {
        foreach (Chunk chunk in activeChunks)
        {
            Voxel[,,] voxels = chunk.getVoxels();
            bool signalMeshRegen = false;
            for (int x = 0; x < Chunk.width; x++)
            {
                for (int y = 0; y < Chunk.height; y++)
                {
                    for (int z = 0; z < Chunk.depth; z++)
                    {
                        //For Each voxel in chunk

                        Voxel voxel = voxels[x, y, z];
                        Substance substance = voxels[x, y, z].substance;
                        if (voxel.substance.state == State.LIQUID && voxel.motes > 1)
                        {
                            Flow(voxel);
                        }
                    }
                }
            }
        }
    }

    private void Flow(Voxel voxel)
    {
        var adjacentVoxels = voxel.chunk.GetVoxelsAdjacentTo(voxel.x, voxel.y, voxel.z);

        foreach (var adjacentVoxel in adjacentVoxels)
        {
            if (adjacentVoxel.substance == Substance.air || adjacentVoxel.substance.state == State.LIQUID)
            {
                // Move a mote to the adjacent voxel
                voxel.motes--;
                adjacentVoxel.motes++;

                if (adjacentVoxel.substance == Substance.air)
                {
                    // Change substance of adjacent voxel to match the current voxel
                    adjacentVoxel.substance = voxel.substance;
                }

                voxel.chunk.SignalMeshRegen(); // Update the voxel mesh
                if (adjacentVoxel.chunk != voxel.chunk)
                {
                    adjacentVoxel.chunk.SignalMeshRegen(); // Update adjacent chunk's voxel mesh
                }

                if (voxel.motes <= 1)
                {
                    break; // Stop transferring motes if this voxel only has 1 left
                }
            }
        }
    }
}
