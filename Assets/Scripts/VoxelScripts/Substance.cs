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
    //public readonly int flammablity;
    public readonly bool burnable;
    public readonly int burnTime;

    private static int nextId = 0;
    private static readonly Dictionary<int, Substance> substanceLookUp = new Dictionary<int, Substance>();
    private Substance(string name, State state, int flammablity, bool burnable = false, int burnTime = 0)
    {
        this.name = name;
        this.state = state;
        //this.flammablity = flammablity;
        this.burnable = burnable;
        this.burnTime = burnTime;

        this.id = nextId++;
        substanceLookUp.Add(this.id, this);
    }

    public static Substance getById(int id)
    {
        return substanceLookUp[id];
    }

    public static int NumberSubstances()
    {
        return nextId;
    }

    //Solids
    public static readonly Substance dirt = new Substance("dirt", State.SOLID, 0);
    public static readonly Substance mud = new Substance("Mud", State.SOLID, 0);
    public static readonly Substance stone = new Substance("stone", State.SOLID, 0);
    public static readonly Substance wood = new Substance("wood", State.SOLID, 20, true, 10); 
    public static readonly Substance ice = new Substance("ice", State.SOLID, 0);
    public static readonly Substance leaf = new Substance("leaf", State.SOLID, 30, true, 5);
    public static readonly Substance debug = new Substance("highlight", State.SOLID, 0);
    public static readonly Substance fire = new Substance("fire", State.SOLID, 30, false);



    //Liquids
    public static readonly Substance water = new Substance("water", State.LIQUID, 0);
    public static readonly Substance oil = new Substance("oil", State.LIQUID, 50, true, 8);
    public static readonly Substance lava = new Substance("lava", State.LIQUID, 100);


    //Gases
    public static readonly Substance air = new Substance("air", State.GAS, 0);
    public static readonly Substance steam = new Substance("steam", State.GAS, 0);
    public static readonly Substance smoke = new Substance("smoke", State.GAS, 0);



    /// <summary>
    /// Liquid form dictionary logic
    /// </summary>
    private static readonly Dictionary<Substance, Substance> gasToLiquid = new Dictionary<Substance, Substance>
    {
        { steam, water },
        // add other gas-liquid pairs here
    };

    private static readonly Dictionary<Substance, Substance> liquidToGas = new Dictionary<Substance, Substance>
    {
        { water, steam },
        // add other liquid-gas pairs here
    };

    public Substance GetLiquidForm()
    {
        if (state != State.GAS)
        {
           // Debug.LogError("Only gas substances have a liquid form");
        }

        return gasToLiquid.ContainsKey(this) ? gasToLiquid[this] : null;
    }

    public Substance GetGasForm()
    {
        if (state != State.LIQUID)
        {
           // Debug.LogError("Only liquid substances have a gas form");
        }

        return liquidToGas.ContainsKey(this) ? liquidToGas[this] : null;
    }
}