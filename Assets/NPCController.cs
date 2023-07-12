using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour
{
    public Vector3[] patrolPoints;
    public float stoppingDistance = 0.1f;

    private MovementScript movementScript;
    private AStarPathfinder pathfinder;  // Changed this line
    private List<Vector3Int> path;
    private int currentPathIndex;
    private int currentPatrolPointIndex = 0;

    private void Start()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        movementScript = GetComponent<MovementScript>();
        World world = FindObjectOfType<World>();
        pathfinder = new AStarPathfinder(world);  // Changed this line

        SetNewTarget(patrolPoints[currentPatrolPointIndex]);
    }

    private void Update()
    {
        MoveAlongPath();
    }

    private void MoveAlongPath()
    {
        if (path == null || currentPathIndex >= path.Count)
        {
            currentPatrolPointIndex = (currentPatrolPointIndex + 1) % patrolPoints.Length;
            SetNewTarget(patrolPoints[currentPatrolPointIndex]);
            return;
        }

        Vector3Int targetVoxel = path[currentPathIndex];
        Vector3 worldTarget = World.VoxelCoordToWorldCoord(targetVoxel);

        Vector3 directionToTarget = worldTarget - transform.position;
        directionToTarget.y = 0;

        if (directionToTarget.magnitude < stoppingDistance)
        {
            //Debug.Log("Reached destination");
            currentPathIndex++;
            return;
        }

        Vector3 moveDirection = directionToTarget.normalized;
        //Debug.Log("Move direction: " + moveDirection);
        movementScript.MoveCharacter(moveDirection);
    }

private const int maxAttempts = 10;  // Maximum attempts to calculate a path
private int currentAttempt = 0;  // Current attempt count

private void SetNewTarget(Vector3 targetPosition)
{
    Vector3Int start = World.WorldCoordToVoxelCoord(transform.position);
    Vector3Int target = World.WorldCoordToVoxelCoord(targetPosition);

    path = pathfinder.FindPath(start, target, GetComponent<MovementScript>().maxMoveDiff);  // Using BFS
    currentPathIndex = 0;

    if (path == null)
    {
        Debug.Log("Path is null");
        if (++currentAttempt <= maxAttempts)
        {
            Debug.Log("Recalculating path");
            SetNewTarget(targetPosition);
        }
        else
        {
            Debug.Log("Exceeded maximum attempts to calculate path");
            currentAttempt = 0;  // Reset the count for the next time
        }
    }
    else
    {
        Debug.Log("Path found, length: " + path.Count);
        currentAttempt = 0;  // Reset the count for the next time
    }
}


   /* void OnDrawGizmos()
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
        //Gizmos.DrawCube(World.VoxelCoordToWorldCoord(start), new Vector3(0.5f, 0.5f, 0.5f));
        Gizmos.DrawCube(World.VoxelCoordToWorldCoord(target), new Vector3(1f, 1f, 1f));
    }*/

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
        Gizmos.DrawCube(World.VoxelCoordToWorldCoord(target), new Vector3(1f, 1f, 1f));

        // Draw invalid voxels
        MovementScript movementScript = GetComponent<MovementScript>();
        if (movementScript != null)
        {
            Gizmos.color = Color.blue;
            foreach (Vector3Int invalidVoxel in movementScript.InvalidVoxels)
            {
                Gizmos.DrawCube(World.VoxelCoordToWorldCoord(invalidVoxel), Vector3.one/2);
            }
        }
    }

}
