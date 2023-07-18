using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IActionBehavior
{
    void PerformAction();
}

public class PatrolBehavior : IActionBehavior
{
    private NPCController npcController;
    public PatrolBehavior(NPCController npcController)
    {
        this.npcController = npcController;
    }

    public void PerformAction()
    {
        npcController.MoveAlongPath();
        npcController.CheckIfStuck();
    }
}

public class AttackBehavior : IActionBehavior
{
    private NPCController npcController;
    private GameObject target;
    private float chaseRange;
    private float attackRange;
    private MovementScript movementScript;

    public AttackBehavior(NPCController npcController, GameObject target, float chaseRange, float attackRange)
    {
        this.npcController = npcController;
        this.target = target;
        this.chaseRange = chaseRange;
        this.movementScript = npcController.GetComponent<MovementScript>();
        this.attackRange = attackRange;
    }

    public void PerformAction()
    {
        // If target is within chase range, move towards it
        if (Vector3.Distance(npcController.transform.position, target.transform.position) <= chaseRange)
        {
            npcController.SetNewTarget(target.transform.position);
            npcController.MoveAlongPath();
        }

        // Basic attack logic: decrease target health
        NPC targetNpc = target.GetComponent<NPC>();
        if (targetNpc != null && targetNpc.IsAlive() && Vector3.Distance(npcController.transform.position, target.transform.position) <=attackRange)
        {
            Debug.Log("ATTACKING " + targetNpc.Name);
            targetNpc.Health -= 10;
        }
    }
}

