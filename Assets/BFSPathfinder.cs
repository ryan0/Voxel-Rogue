using System.Collections.Generic;
using UnityEngine;

public class BFSPathfinder
{
    private World world;

    public BFSPathfinder(World world)
    {
        this.world = world;
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target, int maxMoveDiff)
    {
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        Queue<Vector3Int> frontier = new Queue<Vector3Int>();

        frontier.Enqueue(start);
        cameFrom[start] = start;

        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();

            if (current == target)
            {
                // We've found a path to the target
                return ReconstructPath(cameFrom, current);
            }

            foreach (var neighbor in GetNeighbors(current, maxMoveDiff))
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    frontier.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        // If we get here, no path was found
        return null;
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

                for (int y = -maxDiff; y <= maxDiff; y++)
                {
                    int newX = voxel.x + x;
                    int newZ = voxel.z + z;
                    int newY = voxel.y + y;

                    Vector3Int neighborPosition = new Vector3Int(newX, newY, newZ);

                    // Add a check to ensure the neighbor voxel is within world bounds
                    if (World.IsVoxelInBounds(neighborPosition))
                    {
                        var neighborVoxelType = world.GetVoxelType(neighborPosition);

                        if (neighborVoxelType != null && neighborVoxelType.state != State.SOLID)
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
