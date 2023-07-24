using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character
{
    //affinity
    //Job
    //Species
    //aggregate stats from limbs
    //Reference to faciton/species relationship dictionary for attacking

    public string Name;
    public int Health;

    public bool isAlive;

    private static int nextId = 0;
    public int NPC_ID;

    public Character(string name, int health, bool isAlive = true)
    {
        Name = name;
        Health = health;
        this.isAlive = isAlive;
        NPC_ID = nextId;
        nextId++;    
    }

    public bool IsAlive()
    {
        return isAlive;
    }
}

public class NPC : Character
{
    // NPC-specific properties go here...
    public HouseData home;

    public IActionBehavior Behavior;

    public NPC(string name, int health, IActionBehavior behavior, bool isAlive = true, HouseData _home = null)
        : base(name, health, isAlive)  // Call base constructor
    {
        Behavior = behavior;
        home = _home;
    }

    public void PerformAction()
    {
        Behavior.PerformAction();
    }
    
}

public class Player : Character
{
    // Player-specific properties go here...

    public Player(string name, int health, bool isAlive = true)
        : base(name, health, isAlive)  // Call base constructor
    {
    }

    // Player-specific methods go here...
}




