using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Voxel
{
    public const float size = .5f;

    public Substance substance = Substance.air;
    public int mass = 10;
    public float temperature;

    public Voxel(Substance substance, float temperature)
    {
        this.substance = substance;
        this.temperature = temperature;
    }
}

