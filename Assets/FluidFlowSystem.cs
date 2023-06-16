using System.Collections.Generic;
using UnityEngine;

public class FluidFlowSystem
{
    private class FluidFlowInteraction
    {
        public readonly int triggerSubstanceId;
        public readonly int createdSubstanceId;

        public FluidFlowInteraction(int triggerSubstanceId, int createdSubstanceId)
        {
            this.triggerSubstanceId = triggerSubstanceId;
            this.createdSubstanceId = createdSubstanceId;
        }
    }

    private List<FluidFlowInteraction>[] flowInteractions = new List<FluidFlowInteraction>[Substance.NumberSubstances()];

    public FluidFlowSystem()
    {
        // Configure fluid flow interactions
        flowInteractions[Substance.water.id] = new List<FluidFlowInteraction>
        {
            new FluidFlowInteraction(Substance.air.id, Substance.water.id)
        };
        // Add more interactions if required
    }

    public void UpdateFluidFlow(List<Chunk> activeChunks)
    {
        foreach (Chunk chunk in activeChunks)
        {
            Voxel[,,] voxels = chunk.getVoxels();
            bool signalMeshRegen = false;

            // Create a list of all voxel coordinates
            List<Vector3Int> voxelCoordinates = new List<Vector3Int>();
            for (int x = 0; x < Chunk.width; x++)
            {
                for (int y = 0; y < Chunk.height; y++)
                {
                    for (int z = 0; z < Chunk.depth; z++)
                    {
                        voxelCoordinates.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            // Shuffle the list
            System.Random rng = new System.Random();
            int n = voxelCoordinates.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Vector3Int value = voxelCoordinates[k];
                voxelCoordinates[k] = voxelCoordinates[n];
                voxelCoordinates[n] = value;
            }

            // Loop over the shuffled list
            foreach (Vector3Int coord in voxelCoordinates)
            {
                int x = coord.x;
                int y = coord.y;
                int z = coord.z;

                // Check if voxel is water and has more than one mote
                Voxel voxel = voxels[x, y, z];
                if (voxel.substance.id == Substance.water.id && voxel.motes > 1)
                {
                    List<Voxel> adjacentVoxels = chunk.GetVoxelsAdjacentTo(x, y, z);
                    adjacentVoxels = adjacentVoxels.FindAll(v => v.substance.id == Substance.water.id || v.substance.id == Substance.air.id);
                    // Remove voxels that are above the current voxel
                    adjacentVoxels.RemoveAll(v => v.y > y);

                    if (adjacentVoxels.Count > 0)
                    {
                        // Randomly select an adjacent voxel to receive the mote
                        Voxel targetVoxel = adjacentVoxels[rng.Next(adjacentVoxels.Count)];
                        voxel.motes--;
                        targetVoxel.motes++;

                        // If the target voxel was air, change it to water and set motes to 1
                        if (targetVoxel.substance.id == Substance.air.id)
                        {
                            targetVoxel.substance = Substance.water;
                            targetVoxel.motes = 1;
                        }

                        signalMeshRegen = true;
                    }
                }
            }

            if (signalMeshRegen)
            {
                chunk.SignalMeshRegen();
            }
        }
    }

    //private Substance Hybridize(Substance a, Substance b)
    //{
    // Implement your fluid hybridization logic here
    // For simplicity, we will just return a new Substance
    //return new Substance(/*Your fluid hybridization parameters here*/);
    //}
}
