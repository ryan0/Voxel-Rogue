using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AStarPathfinder
{
    private World world;
    private const int moveCost = 1;
    private const int maxPathfindingDistance = 50;

    public AStarPathfinder(World world)
    {
        this.world = world;
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target, int maxMoveDiff)
    {
        List<Vector3Int> finalPath = new List<Vector3Int>();

        // Generate intermediary points and add to finalPath
        List<Vector3Int> intermediaries = GenerateIntermediaries(start, target);
        foreach (Vector3Int intermediary in intermediaries)
        {
            List<Vector3Int> pathToIntermediary = FindDirectPath(start, intermediary, maxMoveDiff);
            if (pathToIntermediary != null)
            {
                finalPath.AddRange(pathToIntermediary);
                start = intermediary;
            }
        }

        // Find path from last intermediary to final target
        List<Vector3Int> pathFromLastIntermediary = FindDirectPath(start, target, maxMoveDiff);
        if (pathFromLastIntermediary != null)
        {
            finalPath.AddRange(pathFromLastIntermediary);
        }

        return finalPath;
    }

    private List<Vector3Int> GenerateIntermediaries(Vector3Int start, Vector3Int target)
    {
        List<Vector3Int> intermediaries = new List<Vector3Int>();

        while (Heuristic(start, target) > maxPathfindingDistance)
        {
            Vector3 direction = ((Vector3)(target - start)).normalized;
            Vector3Int intermediary = start + Vector3Int.RoundToInt(direction * maxPathfindingDistance);
            intermediaries.Add(intermediary);
            start = intermediary;
        }

        return intermediaries;
    }

    private List<Vector3Int> FindDirectPath(Vector3Int start, Vector3Int target, int maxMoveDiff)
    {
        // The target is close, use the existing A* implementation
        int maxIterations = 10000; // Maximum number of iterations to prevent infinite loop.
        int currentIteration = 0; // The current iteration count.

        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, int> costSoFar = new Dictionary<Vector3Int, int>();
        SortedList<int, Vector3Int> frontier = new SortedList<int, Vector3Int>();

        frontier.Add(0, start);
        cameFrom[start] = start;
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
            if (currentIteration++ >= maxIterations)
            {
                Debug.LogError("Exceeded maximum iterations. Exiting to avoid infinite loop.");
                return null; // Return null or a fallback path if the loop exceeds max iterations.
            }

            var current = frontier.Values[0];
            frontier.RemoveAt(0);

            if (current == target)
            {
                // We've found a path to the target
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in GetNeighbors(current, maxMoveDiff))
            {
                int newCost = costSoFar[current] + moveCost;
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    int priority = newCost + Heuristic(neighbor, target);
                    while (frontier.ContainsKey(priority))
                    {
                        // increment priority until unique
                        priority += 1;
                    }
                    frontier.Add(priority, neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        // If we get here, no path was found
        return null;
    }

  private int Heuristic(Vector3Int a, Vector3Int b)
    {
    int distance = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    //Vector3Int v = World.WorldCoordToVoxelCoord(a);
    //Chunk c = world.GetChunkAt(a);
    // Add a check to see if the voxel is a road voxel
    Voxel vox = world.GetVoxelAt(a);
    if (vox.substance == Substance.asphalt)
    {
        // Roads are cheaper to traverse, so reduce the heuristic by a factor
        distance /= 4;
    }

    return distance;
    }


    private List<Vector3Int> GetNeighbors(Vector3Int voxel, int maxMoveDiff)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        int maxDiff = maxMoveDiff; 

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                {
                    continue;
                }

                for (int y = 0; y <= maxDiff; y++)
                {
                    int newX = voxel.x + x;
                    int newZ = voxel.z + z;
                    int newY = voxel.y + y;

                    Vector3Int neighborPosition = new Vector3Int(newX, newY, newZ);
                    Vector3Int belowVox = new Vector3Int(newX, newY - 1, newZ);

                    // Add a check to ensure the neighbor voxel is within world bounds
                    if (World.IsVoxelInBounds(neighborPosition))
                    {
                        var neighborVoxelType = world.GetVoxelAt(neighborPosition).substance;
                        var belowVoxType = world.GetVoxelAt(belowVox).substance;

                        if (neighborVoxelType != null && neighborVoxelType.state != State.SOLID && belowVoxType.state == State.SOLID)
                        {
                            neighbors.Add(neighborPosition);
                        }
                    }
                }
            }
        }

        return neighbors;
    }

    private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        path.Add(current);

        while (current != cameFrom[current])
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
