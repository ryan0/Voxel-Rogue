using System.Collections.Generic;
using UnityEngine;

public class GasFlowSystem
{
    private HashSet<Chunk> activeChunks;
    private HashSet<Voxel> staticVoxels;
    private Dictionary<Chunk, int> chunkUpdateIndices = new Dictionary<Chunk, int>();//cycling index update for optimization
    public static int MAX_GAS_HEIGHT = 104; // Set this to your desired maximum gas height

    public GasFlowSystem()
    {
        activeChunks = new HashSet<Chunk>();
        staticVoxels = new HashSet<Voxel>();
    }

    public void UpdateGasFlow(HashSet<Chunk> activeChunks)
    {
        // Similar to fluid flow, update a subset of the voxels in each active chunk
        foreach (Chunk chunk in activeChunks)
        {
            // Get the current update index for this chunk, default to 0 if not present
            chunkUpdateIndices.TryGetValue(chunk, out int yIndex);
            Voxel[,,] voxels = chunk.getVoxels();
            for (int x = 0; x < Chunk.width; x++)
            {
                for (int z = 0; z < Chunk.depth; z++)
                {
                    //for (int y = Chunk.height - 1; y >= 0; y--) // Start from the top and go down
                    Voxel voxel = voxels[x, yIndex, z];
                    {
                        //Voxel voxel = voxels[x, y, z];
                        if (voxel.substance.state == State.GAS && voxel.substance.id != Substance.air.id)
                        {
                            if (Flow(voxel, chunk, voxels, x, yIndex, z, voxel.substance))
                            {
                                chunk.SignalMeshRegen();
                            }
                        }
                    }
                }
            }
            // Increment the index for the next cycle, or reset it if we've reached the top
            yIndex = (yIndex + 1) % Chunk.height;
            chunkUpdateIndices[chunk] = yIndex;
        }
    }

    public bool Flow(Voxel voxel, Chunk chunk, Voxel[,,] voxels, int x, int y, int z, Substance gasType)
    {
        // Gas will "fall up", but will not exceed MAX_GAS_HEIGHT
        if (voxel.globalY < MAX_GAS_HEIGHT)
        {
            Voxel voxelAbove = chunk.GetVoxelsAdjacentTo(x,y,z)[1];//top neighbor
            if (voxelAbove != null && voxelAbove.substance.id == Substance.air.id)
            {
                // The gas moves into the voxel above
                voxelAbove.substance = gasType;
                voxelAbove.motes = voxel.motes;

                // The voxel that the gas came from becomes air
                voxel.substance = Substance.air;
                voxel.motes = 0;
                return true;
            }
            else if (voxelAbove.substance.id == gasType.id)
            {
                // The gas combines with the gas above
                voxelAbove.motes += voxel.motes;
                voxel.substance = Substance.air;
                voxel.motes = 0;
                return true;
            }
            else if (voxelAbove.substance.state == State.LIQUID)
            {
                // The gas moves up through the liquid, exchanging places
                Substance tempSubstance = voxel.substance;
                int tempMotes = voxel.motes;

                voxel.substance = voxelAbove.substance;
                voxel.motes = voxelAbove.motes;

                voxelAbove.substance = tempSubstance;
                voxelAbove.motes = tempMotes;

                return true;
            }
        }
        return false;
    }
}
