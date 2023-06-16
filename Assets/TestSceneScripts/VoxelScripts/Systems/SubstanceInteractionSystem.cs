using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubstanceInteractionSystem
{
    private class TransmuteInteraction
    {
        public readonly int triggerSubstanceId;
        public readonly int transmuteToSubstanceId;

        public TransmuteInteraction(int triggerSubstanceId, int transmuteToSubstanceId)
        {
            this.triggerSubstanceId = triggerSubstanceId;
            this.transmuteToSubstanceId = transmuteToSubstanceId;
        }
    }

    private List<TransmuteInteraction>[] transmuteInteractions = new List<TransmuteInteraction>[Substance.NumberSubstances()];

    public SubstanceInteractionSystem()
    {
        transmuteInteractions[Substance.dirt.id] = new();
        transmuteInteractions[Substance.dirt.id].Add(new TransmuteInteraction(Substance.water.id, Substance.mud.id));


        transmuteInteractions[Substance.wood.id] = new();
        transmuteInteractions[Substance.wood.id].Add(new TransmuteInteraction(Substance.lava.id, Substance.smoke.id));

        transmuteInteractions[Substance.lava.id] = new ();
        transmuteInteractions[Substance.lava.id].Add(new TransmuteInteraction(Substance.water.id, Substance.stone.id));

        transmuteInteractions[Substance.water.id] = new ();
        transmuteInteractions[Substance.water.id].Add(new TransmuteInteraction(Substance.lava.id, Substance.steam.id));


    }




    public void UpdateSubstances(List<Chunk> activeChunks)
    {
        foreach(Chunk chunk in activeChunks)
        {
            Voxel[,,] voxels = chunk.getVoxels();
            bool signalMeshRegen = false;


            for (int x = 0; x < Chunk.width; x++)
            {
                for(int y = 0; y < Chunk.height; y++)
                {
                    for(int z = 0; z < Chunk.depth; z++)
                    {
                        //For Each voxel in chunk


                        Substance substance = voxels[x, y, z].substance;

                        List<TransmuteInteraction> interactionsList = transmuteInteractions[substance.id];
                        if(interactionsList != null)
                        {
                            foreach(TransmuteInteraction i in interactionsList)
                            {
                                List<Voxel> adjacentVoxels = chunk.GetVoxelsAdjacentTo(x, y, z);

                                foreach(Voxel v in adjacentVoxels)
                                {
                                    if(v.substance.id == i.triggerSubstanceId)
                                    {
                                        voxels[x, y, z].substance = Substance.getById(i.transmuteToSubstanceId);
                                        signalMeshRegen = true;
                                        break;
                                    }
                                }
                            }
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
}
