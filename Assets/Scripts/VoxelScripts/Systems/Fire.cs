using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fire
{
    public Voxel sourceVoxel;
    public int burnTimeLeft;
    public bool hasGeneratedSmoke;

    public Fire(Voxel sourceVoxel, int burnTime = 10)
    {
        this.sourceVoxel = sourceVoxel;
        this.burnTimeLeft = burnTime;
        this.hasGeneratedSmoke = false;
    }

    public void Burn()
    {
        burnTimeLeft--;
        sourceVoxel.motes--;
        if (burnTimeLeft <= 0)
        {
            sourceVoxel.ExtinguishFire();
        }
        else if (burnTimeLeft <= burnTimeLeft / 2)
        {
            GenerateSmoke();
            hasGeneratedSmoke = true;
        }

    }

    private void GenerateSmoke()
    {
        // logic to generate a smoke voxel in a random air tile adjacent to the sourceVoxel
    }
}
