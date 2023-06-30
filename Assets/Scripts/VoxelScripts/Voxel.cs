using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public const float size = .5f;


    public int x;
    public int y;
    public int z;

    public Chunk chunk { get; private set; }

    public Substance substance = Substance.air;
    public float temperature;
    public int motes;
    public Fire fire;


    public Voxel(int x, int y, int z, Chunk chunk, Substance substance, float temperature = 30f, int motes = 5)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.chunk = chunk;
        this.substance = substance;
        this.temperature = temperature;
        this.motes = motes;
    }

    public void SetOnFire(Fire fire)
    {
        this.fire = fire;
        fire.sourceVoxel.substance = Substance.fire;

    }

    public void ExtinguishFire()
    {
        fire.sourceVoxel.substance = fire.originalSubstance;
        this.fire = null;

    }

    public Voxel[] getNeighbors()
    {
        return chunk.GetVoxelsAdjacentTo(x, y, z);
    }
}
