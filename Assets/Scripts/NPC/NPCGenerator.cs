using UnityEngine;
using System.Collections.Generic;


public class NPCGenerator : MonoBehaviour
{
    private List<NPC> npcs;

    public NPCGenerator()
    {
        npcs = new List<NPC>();
    }

    public NPC GenerateNPC(string name, int health, IActionBehavior behavior, HouseData home)
    {
        Debug.Log("Creating npc " + name + " " + health + "");
        NPC newNPC = new NPC(name, health, behavior, true, home);
        npcs.Add(newNPC);
        return newNPC;
    }

    public void PerformAllNPCActions()
    {
        foreach(NPC npc in npcs)
        {
            npc.PerformAction();
        }
    }
}
