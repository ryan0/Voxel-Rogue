using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemperatureSystem
{
    private enum StateChange
    {
        Melting,
        Freezing,
        Evaporation,
        Condensation
    }

    private class StateChangeInteraction
    {
        public readonly StateChange stateChange;
        public readonly float temperature;
        public readonly int changeToId;

        public StateChangeInteraction(StateChange stateChange, float temperature, int changeToId)
        {
            this.stateChange = stateChange;
            this.temperature = temperature;
            this.changeToId = changeToId;
        }
    }

    private List<StateChangeInteraction>[] stateChangeInteractions = new List<StateChangeInteraction>[Substance.NumberSubstances()];

    public TemperatureSystem()
    {
        stateChangeInteractions[Substance.water.id] = new();
        stateChangeInteractions[Substance.water.id].Add(new StateChangeInteraction(StateChange.Freezing, 5.0f, Substance.ice.id));
        stateChangeInteractions[Substance.water.id].Add(new StateChangeInteraction(StateChange.Evaporation, 100.0f, Substance.steam.id));

        stateChangeInteractions[Substance.steam.id] = new();
        stateChangeInteractions[Substance.steam.id].Add(new StateChangeInteraction(StateChange.Condensation, 100.0f, Substance.water.id));

        stateChangeInteractions[Substance.ice.id] = new();
        stateChangeInteractions[Substance.ice.id].Add(new StateChangeInteraction(StateChange.Melting, 5.0f, Substance.water.id));

    }


    public void UpdateTemperatures(HashSet<Chunk> activeChunks)
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

                        Substance substance = voxels[x, y, z].substance;
                        float voxelTemperature = voxels[x, y, z].temperature;

                        List<StateChangeInteraction> interactionsList = stateChangeInteractions[substance.id];
                        if (interactionsList != null)
                        {
                            foreach (StateChangeInteraction i in interactionsList)
                            {
                                if (i.stateChange == StateChange.Freezing && voxelTemperature < i.temperature)
                                {
                                    voxels[x, y, z].substance = Substance.getById(i.changeToId);
                                    signalMeshRegen = true;
                                }
                                else if (i.stateChange == StateChange.Melting && voxelTemperature > i.temperature)
                                {
                                    voxels[x, y, z].substance = Substance.getById(i.changeToId);
                                    signalMeshRegen = true;
                                }
                                else if (i.stateChange == StateChange.Condensation && voxelTemperature < i.temperature)
                                {
                                    voxels[x, y, z].substance = Substance.getById(i.changeToId);
                                    signalMeshRegen = true;
                                }
                                else if (i.stateChange == StateChange.Evaporation && voxelTemperature > i.temperature)
                                {
                                    voxels[x, y, z].substance = Substance.getById(i.changeToId);
                                    signalMeshRegen = true;
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
