using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPC
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
    public HouseData home;

    public IActionBehavior Behavior;

    public NPC(string name, int health, IActionBehavior behavior, bool isAlive = true,  HouseData _home = null)
    {
        Name = name;
        Health = health;
        this.isAlive = isAlive;
        NPC_ID = nextId;
        Behavior = behavior;
        home = _home;
        nextId++;    
    }

    public bool IsAlive()
    {
        return isAlive;
    }
       public void PerformAction()
    {
        Behavior.PerformAction();
    }
}





