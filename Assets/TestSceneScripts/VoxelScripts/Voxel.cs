using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public const float size = .5f;

    public int x { get; private set; }
    public int y { get; private set; }
    public int z { get; private set; }

    public Chunk chunk { get; private set; }

    public Substance substance = Substance.air;
    public int mass = 10;
    public float temperature;
    public int motes { get; set; } // new property for motes

    public Voxel(int x, int y, int z, Chunk chunk, Substance substance, float temperature, int motes = 5)
    {
        this.x = x;
        this.y = y;
        this.z = z;
        this.chunk = chunk;
        this.substance = substance;
        this.temperature = temperature;
        this.motes = motes;
    }
}
