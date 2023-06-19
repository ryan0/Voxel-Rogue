using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire
{
    public Voxel sourceVoxel;
    public int burnTimeLeft;
    public Substance originalSubstance;

    public Fire(Voxel sourceVoxel)
    {
        int burnTime = sourceVoxel.substance.burnTime;
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
    }
}
