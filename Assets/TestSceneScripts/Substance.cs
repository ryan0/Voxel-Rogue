using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum State
{
    SOLID,
    LIQUID,
    GAS
}

public class Substance
{
    public readonly int id;
    public readonly string name;
    public readonly State state;
    public readonly int flammablity;
    
    private static int nextId = 0;
    private Substance(string name, State state, int flammablity)
    {
        this.name = name;
        this.state = state;
        this.flammablity = flammablity;

        this.id = nextId++;
    }


    //Solids
    public static readonly Substance dirt = new Substance("dirt", State.SOLID, 0);
    public static readonly Substance stone = new Substance("stone", State.SOLID, 0);
    public static readonly Substance wood = new Substance("wood", State.SOLID, 20);


    //Liquids
    public static readonly Substance water = new Substance("water", State.LIQUID, 0);
    public static readonly Substance oil = new Substance("oil", State.LIQUID, 50);


    //Gases
    public static readonly Substance air = new Substance("air", State.GAS, 0);
    public static readonly Substance steam = new Substance("steam", State.GAS, 0);
    public static readonly Substance smoke = new Substance("smoke", State.GAS, 0);

}