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

            System.Random rng = new System.Random();
            // Shuffle the list
            /*
            int n = voxelCoordinates.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                Vector3Int value = voxelCoordinates[k];
                voxelCoordinates[k] = voxelCoordinates[n];
                voxelCoordinates[n] = value;
            }*/

            // Instead of shuffling the voxelCoordinates list, create it in the desired order:
            for (int y = 0; y < Chunk.height; y++) // Bottom to top
            {
                for (int z = Chunk.depth - 1; z >= 0; z--) // Back to front
                {
                    for (int x = 0; x < Chunk.width; x++) // Left to right
                    {
                        voxelCoordinates.Add(new Vector3Int(x, y, z));
                    }
                }
            }


            // Loop over the shuffled list
            foreach (Vector3Int coord in voxelCoordinates)
            {
                int x = coord.x;
                int y = coord.y;
                int z = coord.z;

                // Check if voxel is water
                Voxel voxel = voxels[x, y, z];
                if (voxel.substance.id == Substance.water.id)
                {
                    List<Voxel> adjacentVoxels = chunk.GetVoxelsAdjacentTo(x, y, z);
                    adjacentVoxels = adjacentVoxels.FindAll(v => v.substance.id == Substance.water.id || v.substance.id == Substance.air.id);

                    // Drain functionality for when there's only one mote of water left
                    if (voxel.motes == 1)
                    {
                        List<Voxel> waterVoxelsAnySize = adjacentVoxels.FindAll(v => v.substance.id == Substance.water.id);
                        if (waterVoxelsAnySize.Count == 0)//when there is NO water of any mote quantity
                        {
                            // If there is air directly below
                            if (y > 0 && voxels[x, y - 1, z].substance.id == Substance.air.id)
                            {
                                voxels[x, y - 1, z].substance = Substance.water;
                                voxels[x, y - 1, z].motes = 1;
                                voxel.substance = Substance.air;
                                voxel.motes = 0;
                                signalMeshRegen = true;
                            }
                        }
                        else
                        {

                            // Look for adjacent water voxels to draw from that have MORE than 1 mote
                            List<Voxel> waterVoxels = adjacentVoxels.FindAll(v => v.substance.id == Substance.water.id && v.motes > 1);

                            if (waterVoxels.Count > 0)
                            {
                                // Draw from a random water voxel
                                Voxel waterVoxel = waterVoxels[rng.Next(waterVoxels.Count)];
                                waterVoxel.motes--;
                                voxel.motes++;
                            }
                            else
                            {
                                // If there are no water voxels to draw from, move into the empty space
                                // Find adjacent air voxels that are not above the current voxel
                                List<Voxel> airVoxels = adjacentVoxels.FindAll(v => v.substance.id == Substance.air.id && v.y <= y);

                                if (airVoxels.Count > 0)
                                {
                                    // Choose a random air voxel and move into it
                                    Voxel airVoxel = airVoxels[rng.Next(airVoxels.Count)];
                                    airVoxel.substance = Substance.water;
                                    airVoxel.motes = 1;

                                    // Leave air behind
                                    voxel.substance = Substance.air;
                                    voxel.motes = 0;
                                }

                            }

                            signalMeshRegen = true;
                        }
                    }
                    else if (voxel.motes > 1) // Original fluid flow for water with more than one mote
                    {
                        // Remove voxels that are above the current voxel
                        adjacentVoxels.RemoveAll(v => v.y > y);

                        if (adjacentVoxels.Count > 0)
                        {
                            Voxel targetVoxel;

                            // Check if the voxel below is eligible
                            if (y > 0 && (voxels[x, y - 1, z].substance.id == Substance.air.id || (voxels[x, y - 1, z].substance.id == Substance.water.id && voxels[x, y - 1, z].motes < voxel.motes)))
                            {
                                targetVoxel = voxels[x, y - 1, z];
                            }
                            else
                            {
                                // If the voxel below is not eligible, randomly select an adjacent voxel to receive the mote
                                targetVoxel = adjacentVoxels[rng.Next(adjacentVoxels.Count)];
                            }

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
