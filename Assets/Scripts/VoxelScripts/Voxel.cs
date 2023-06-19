using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public const float size = .5f;
    public int framesSinceLastChange = 0;
    public int x { get; private set; }
    public int y { get; private set; }
    public int z { get; private set; }
    public int globalX { get; private set; }
    public int globalY { get; private set; }
    public int globalZ { get; private set; }

    public Chunk chunk { get; private set; }

    public Substance substance = Substance.air;
    public int mass = 10;
    public float temperature;
    public int motes { get; set; } // new property for motes
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

        // Calculate global position based on local position and chunk's world position
        this.globalX = x + chunk.xIndex * Chunk.width;
        this.globalY = y + chunk.yIndex * Chunk.height;
        this.globalZ = z + chunk.zIndex * Chunk.depth;
    }

    public void SetOnFire(Fire fire)
    {
        this.fire = fire;
    }

    public void ExtinguishFire()
    {
        this.fire = null;
    }
}
