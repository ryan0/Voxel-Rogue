using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
    private World world;
    private const int moveCost = 1;
    public AStarPathfinder(World world)
    {
        this.world = world;
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target, int maxMoveDiff)
    {
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, int> costSoFar = new Dictionary<Vector3Int, int>();
        SortedList<int, Vector3Int> frontier = new SortedList<int, Vector3Int>();

        frontier.Add(0, start);
        cameFrom[start] = start;
        costSoFar[start] = 0;

        while (frontier.Count > 0)
        {
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
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) + Mathf.Abs(a.z - b.z);
    }
    private List<Vector3Int> GetNeighbors(Vector3Int voxel, int maxMoveDiff)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();
        int maxDiff = maxMoveDiff; // you need to reference to the NPC's MovementScript to get maxMoveDiff

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                {
                    continue;
                }

                for (int y = -maxDiff*3; y <= maxDiff; y++)
                {
                    int newX = voxel.x + x;
                    int newZ = voxel.z + z;
                    int newY = voxel.y + y;

                    Vector3Int neighborPosition = new Vector3Int(newX, newY, newZ);
                    Vector3Int belowVox = new Vector3Int(newX, newY -1, newZ);


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