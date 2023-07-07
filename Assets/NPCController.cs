using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    public Vector3[] patrolPoints;
    public float stoppingDistance = 0.1f;

    private MovementScript movementScript;
    private AStarPathfinder pathfinder;
    private List<Vector3Int> path;
    private int currentPathIndex;
    private int currentPatrolPointIndex = 0;

    private void Start()
    {
        //Debug.Log("NPC Controller Started");
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            //Debug.LogError("No patrol points set for NPC.");
            return;
        }

        movementScript = GetComponent<MovementScript>();
        World world = FindObjectOfType<World>();
        pathfinder = new AStarPathfinder(world, movementScript.maxMoveDiff);

        SetNewTarget(patrolPoints[currentPatrolPointIndex]);
    }

    private void Update()
    {
        //Debug.Log("Update called"); // Adding this line
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        //Debug.Log("MoveAlongPath called"); // Adding this line
        if (path == null || currentPathIndex >= path.Count)
        {
            //Debug.Log("Setting new target"); // Adding this line
            currentPatrolPointIndex = (currentPatrolPointIndex + 1) % patrolPoints.Length;
            SetNewTarget(patrolPoints[currentPatrolPointIndex]);
            return;
        }

        Vector3Int targetVoxel = path[currentPathIndex];
        Vector3 worldTarget = World.VoxelCoordToWorldCoord(targetVoxel);

        Vector3 directionToTarget = worldTarget - transform.position;
        directionToTarget.y = 0; // Ignore y component

        if (directionToTarget.magnitude < stoppingDistance)
        {
            Debug.Log("Reached destination");
            currentPathIndex++;
            return;
        }

        Vector3 moveDirection = directionToTarget.normalized;
        Debug.Log("Move direction: " + moveDirection);
        movementScript.MoveCharacter(moveDirection);
    }

    private void SetNewTarget(Vector3 targetPosition)
    {
        //Debug.Log("SetNewTarget called"); // Adding this line
        Vector3Int start = World.WorldCoordToVoxelCoord(transform.position);
        Vector3Int target = World.WorldCoordToVoxelCoord(targetPosition);

        path = pathfinder.FindPath(start, target);
        currentPathIndex = 0;

        // Log the path
        if (path == null)
        {
            Debug.Log("Path is null");
        }
        else
        {
            Debug.Log("Path found, length: " + path.Count);
        }
    }

    void OnDrawGizmos()
    {
        if (path != null)
        {
            Gizmos.color = Color.red;
            foreach (Vector3Int point in path)
            {
                Gizmos.DrawCube(World.VoxelCoordToWorldCoord(point), new Vector3(0.5f, 0.5f, 0.5f));
            }
        }

        Vector3Int start = World.WorldCoordToVoxelCoord(transform.position);
        Vector3Int target = World.WorldCoordToVoxelCoord(patrolPoints[currentPatrolPointIndex]);
        Gizmos.DrawCube(World.VoxelCoordToWorldCoord(start), new Vector3(0.5f, 0.5f, 0.5f));
        Gizmos.DrawCube(World.VoxelCoordToWorldCoord(target), new Vector3(1f, 1f, 1f));

    }

}
