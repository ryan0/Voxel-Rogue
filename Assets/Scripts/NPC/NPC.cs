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

    public Character(string name, int health, bool isAlive = true)
    {
        Name = name;
        Health = health;
        this.isAlive = isAlive;
    }

    public bool IsAlive()
    {
        return isAlive;
    }
}

public class NPC : Character
{
    // NPC-specific properties go here...

    public IActionBehavior Behavior;

    public NPC(string name, int health, IActionBehavior behavior, bool isAlive = true)
        : base(name, health, isAlive)  // Call base constructor
    {
        Behavior = behavior;
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




