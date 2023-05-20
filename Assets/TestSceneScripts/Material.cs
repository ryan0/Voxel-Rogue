using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum State
{
    SOLID,
    LIQUID,
    GAS
}

public class Material
{
    public readonly int id;
    public readonly string name;
    public readonly State state;
    public readonly int flammablity;
    
    private static int nextId = 0;
    private Material(string name, State state, int flammablity)
    {
        this.name = name;
        this.state = state;
        this.flammablity = flammablity;

        this.id = nextId++;
    }


    //Solids
    public static readonly Material dirt = new Material("dirt", State.SOLID, 0);
    public static readonly Material stone = new Material("stone", State.SOLID, 0);
    public static readonly Material wood = new Material("wood", State.SOLID, 20);


    //Liquids
    public static readonly Material water = new Material("water", State.LIQUID, 0);
    public static readonly Material oil = new Material("oil", State.LIQUID, 50);


    //Gases
    public static readonly Material air = new Material("air", State.GAS, 0);
    public static readonly Material steam = new Material("steam", State.GAS, 0);
    public static readonly Material smoke = new Material("smoke", State.GAS, 0);

}