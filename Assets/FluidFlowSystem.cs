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

            // Instead of shuffling the voxelCoordinates list, create it in the desired order:
            for (int y = 0; y < Chunk.height; y++) // Bottom to top
            {
                for (int z = Chunk.depth - 1; z >= 0; z--) // Back to front
                {
                    for (int x = 0; x < Chunk.width; x++) // Left to right
                    {

                        // Check if voxel is water
                        Voxel voxel = voxels[x, y, z];
                        if (voxel.substance.id == Substance.water.id)
                        {
                            //flow 
                            Flow(voxel, chunk, voxels, x, y, z, Substance.water);
                        }
                        if (voxel.substance.id == Substance.lava.id)
                        {
                            Flow(voxel, chunk, voxels, x, y, z, Substance.lava);

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

    public bool Flow(Voxel voxel, Chunk chunk, Voxel[,,] voxels, int x, int y, int z, Substance fluidType)
    {
        System.Random rng = new System.Random(123);

        bool signalMeshRegen = false;
        Voxel[] adjacentVoxels = chunk.GetVoxelsAdjacentTo(x, y, z);
        // Manually filter adjacent voxels
        List<Voxel> filteredAdjacentVoxels = new List<Voxel>();
        for (int i = 0; i < adjacentVoxels.Length; i++)
        {
            Voxel v = adjacentVoxels[i];
            if (v != null)
            {
                if (v.substance.id == fluidType.id || v.substance.id == Substance.air.id)
                {
                    filteredAdjacentVoxels.Add(v);
                }
            }

        }
        // Drain functionality for when there's only one mote of water left
        if (voxel.motes == 1)
        {
            List<Voxel> waterVoxelsAnySize = filteredAdjacentVoxels.FindAll(v => v.substance.id == fluidType.id);
            if (waterVoxelsAnySize.Count == 0)//when there is NO water of any mote quantity
            {
                Voxel voxelBelow;
                if (y > 0)
                {
                    voxelBelow = voxels[x, y - 1, z];
                }
                else
                {
                    voxelBelow = adjacentVoxels[4];//voxel.chunk.bottomNeighbour.getVoxels()[x, voxel.chunk.heightPub - 1, z];//adjacentVoxels.Find(v => v.chunk.yIndex < chunk.yIndex); // Neighbor below has a lower y-coordinate
                }
                // If there is air directly below
                if (voxelBelow != null && voxelBelow.substance.id == Substance.air.id)
                {
                    voxelBelow.substance = fluidType;
                    voxelBelow.motes = 1;
                    voxel.substance = Substance.air;
                    voxel.motes = 0;
                    signalMeshRegen = true;
                }
            }
            else
            {

                // Look for adjacent water voxels to draw from that have MORE than 1 mote
                List<Voxel> waterVoxels = filteredAdjacentVoxels.FindAll(v => v.substance.id == fluidType.id && v.motes > 1);

                if (waterVoxels.Count > 0)
                {
                    // Draw from a random water voxel
                    Voxel waterVoxel = waterVoxels[rng.Next(waterVoxels.Count)];
                    waterVoxel.motes--;
                    voxel.motes++;
                    signalMeshRegen = false;

                }
                else
                {
                    // If there are no water voxels to draw from, move into the empty space
                    // Find adjacent air voxels that are not above the current voxel
                    List<Voxel> airVoxels = filteredAdjacentVoxels.FindAll(v => v.substance.id == Substance.air.id && v.y <= y);

                    if (airVoxels.Count > 0)
                    {
                        // Choose a random air voxel and move into it
                        Voxel airVoxel = airVoxels[rng.Next(airVoxels.Count)];
                        airVoxel.substance = fluidType;
                        airVoxel.motes = 1;

                        // Leave air behind
                        voxel.substance = Substance.air;
                        voxel.motes = 0;
                        signalMeshRegen = true;

                    }


                }

            }
        }
        else if (voxel.motes > 1) // Original fluid flow for water with more than one mote
        {
            // Remove voxels that are above the current voxel
            filteredAdjacentVoxels.RemoveAll(v => v.y > y);

            if (filteredAdjacentVoxels.Count > 0)
            {
                Voxel targetVoxel;

                // Check if the voxel below is eligible
                Voxel voxelBelow;
                if (y > 0)
                {
                    voxelBelow = voxels[x, y - 1, z];
                }
                else
                {
                    voxelBelow = adjacentVoxels[4];//voxel.chunk.bottomNeighbour.getVoxels()[x, voxel.chunk.heightPub - 1, z];//adjacentVoxels.Find(v => v.chunk.yIndex < chunk.yIndex); // Neighbor below has a lower y-coordinate
                }
                if (voxelBelow != null && (voxelBelow.substance.id == Substance.air.id || (voxelBelow.substance.id == fluidType.id)))// && voxelBelow.motes < voxel.motes))
                {
                    targetVoxel = voxelBelow;
                }
                else
                {
                    // If the voxel below is not eligible, randomly select an adjacent voxel to receive the mote
                    targetVoxel = adjacentVoxels[rng.Next(filteredAdjacentVoxels.Count)];
                }

                voxel.motes--;

                if (targetVoxel != null)
                {
                    //targetVoxel.motes++;

                    // If the target voxel was air, change it to water and set motes to 1
                    if (targetVoxel.substance.id == Substance.air.id)
                    {
                        targetVoxel.substance = fluidType;
                        targetVoxel.motes = 1;
                    }
                    else if(targetVoxel.substance.id == fluidType.id)
                    {
                        targetVoxel.motes++;

                    }
                    signalMeshRegen = true;

                }


            }
        }
        return signalMeshRegen;
    }


    //private Substance Hybridize(Substance a, Substance b)
    //{
    // Implement your fluid hybridization logic here
    // For simplicity, we will just return a new Substance
    //return new Substance(/*Your fluid hybridization parameters here*/);
    //}
}

