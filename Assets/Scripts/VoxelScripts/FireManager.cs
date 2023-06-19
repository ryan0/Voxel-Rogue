using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireManager
{
    HashSet<Chunk> activeChunks;
    List<Fire> fires = new List<Fire>();
    
    public void UpdateFires(HashSet<Chunk> _activeChunks)//use activeChunks to optimize
    {
        activeChunks = _activeChunks;
        foreach (Chunk chunk in activeChunks)
        {
            for (int i = fires.Count - 1; i >= 0; i--)
            {
                Fire fire = fires[i];
                if (fire.sourceVoxel.chunk == chunk)
                {
                    fire.Burn();
                    Debug.Log("burning");
                    if (fire.sourceVoxel.motes <= 0)
                    {
                        fire.sourceVoxel.substance = Substance.air; // change the voxel to air when it is completely burned
                        fire.sourceVoxel.fire = null;
                        fires.RemoveAt(i); // remove the fire from the list when it's extinguished
                    }
                    else if (fire.burnTimeLeft <= 0)
                    {
                        fire.sourceVoxel.ExtinguishFire();
                        fires.RemoveAt(i); // remove the fire from the list when it's extinguished
                    }
                    else
                    {
                        SpreadFire(fire);
                        if (fire.burnTimeLeft <= fire.burnTime *.75f)
                        {
                            fire.GenerateSmoke();
                        }

                    }
                }
                chunk.SignalMeshRegen();

            }
        }
    }

    private void SpreadFire(Fire fire)
    {
        // logic to spread fire to burnable voxels within range
        // you should add a check here to prevent the fire from spreading to a voxel that is already on fire
        Voxel sourceVoxel = fire.sourceVoxel;
        foreach (Voxel neighbor in sourceVoxel.chunk.GetVoxelsAdjacentTo(sourceVoxel.x, sourceVoxel.y, sourceVoxel.z))
        {
            if (neighbor != null)
            {
                if (neighbor.substance.burnable == true && neighbor.fire == null)
                {
                    // If the neighboring voxel is flammable and not already on fire, start a new fire.
                    StartFire(neighbor);
                }
            }
        }
    }


    public void StartFire(Voxel voxel)
    {
        if (voxel.substance.burnable && voxel.fire == null)
        {
            Fire newFire = new Fire(voxel);
            voxel.SetOnFire(newFire);
            fires.Add(newFire);
        }
    }

    public void ExtinguishFire(Voxel voxel)
    {
        if (voxel.fire != null)
        {
            voxel.SetOnFire(null);
            fires.Remove(voxel.fire);         
        }
    }
}
