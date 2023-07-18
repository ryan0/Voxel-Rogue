using UnityEngine;
using System.Collections.Generic;


public class NPCGenerator : MonoBehaviour
{
    private List<NPC> npcs;

    public NPCGenerator()
    {
        npcs = new List<NPC>();
    }

    public NPC GenerateNPC(string name, int health, IActionBehavior behavior)
    {
        NPC newNPC = new NPC(name, health, behavior);
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
