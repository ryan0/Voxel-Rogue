using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire
{
    public Voxel sourceVoxel;
    public int burnTimeLeft;
    public int burnTime;
    public Substance originalSubstance;

    public Fire(Voxel sourceVoxel)
    {
        burnTime = sourceVoxel.substance.burnTime;
        this.sourceVoxel = sourceVoxel;
        this.burnTimeLeft = burnTime;
        originalSubstance = sourceVoxel.substance;
    }

    public void Burn()
    {
        burnTimeLeft--;
        sourceVoxel.motes--;
    }

    public void GenerateSmoke()
    {
        // logic to generate a smoke voxel in a random air tile adjacent to the sourceVoxel
        Voxel[] neighbors = sourceVoxel.getNeighbors();
        for (int i=0; i< neighbors.Length; i++)
        {
            Voxel v = neighbors[i];
            if (v != null)
            {
                if (i != 4 && v.substance == Substance.air)//dont spawn smoke below fire
                {
                    v.substance = Substance.smoke;

                }
            }
        }
    }
}
