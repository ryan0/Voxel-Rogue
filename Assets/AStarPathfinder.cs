using System.Collections.Generic;
using UnityEngine;

public class AStarPathfinder
{
    private World world;
    private int maxMoveDiff;

    public AStarPathfinder(World world, int maxMoveDiff)
    {
        this.world = world;
        this.maxMoveDiff = maxMoveDiff;
    }

    public List<Vector3Int> FindPath(Vector3Int start, Vector3Int target)
    {
        Debug.Log("FindPath called with start: " + start + " target: " + target);

        HashSet<Vector3Int> openSet = new HashSet<Vector3Int>();
        HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>();
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        Dictionary<Vector3Int, float> gScore = new Dictionary<Vector3Int, float>();
        Dictionary<Vector3Int, float> fScore = new Dictionary<Vector3Int, float>();

        openSet.Add(start);
        gScore[start] = 0;
        fScore[start] = HeuristicCostEstimate(start, target);

        while (openSet.Count > 0)
        {
            Debug.Log("Open set size: " + openSet.Count);
            Vector3Int current = GetLowestFScore(openSet, fScore);

            Debug.Log("Current voxel: " + current);
            Debug.DrawLine(current, target, Color.yellow, 0.1f); // Visual debugging

            if (current == target)
            {
                Debug.Log("Current equals target");
                List<Vector3Int> finalPath = ReconstructPath(cameFrom, current);
                foreach (var point in finalPath)
                {
                    Debug.DrawLine(point, point + Vector3.up, Color.green, 5f); // Visual debugging
                }
                Debug.Log("Path Found: " + (finalPath != null));
                return finalPath;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (var neighbor in GetNeighbors(current))
            {
                if (closedSet.Contains(neighbor))
                {
                    Debug.Log("Neighbor is in the closed set: " + neighbor);
                    continue;
                }

                float tentativeGScore = gScore[current] + 1;
                Debug.Log("Tentative G Score: " + tentativeGScore); // Logging tentative G Score

                if (!openSet.Contains(neighbor))
                {
                    openSet.Add(neighbor);
                }
                else if (tentativeGScore >= gScore.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    Debug.Log("Skipping voxel due to high gScore: " + neighbor);
                    continue;
                }

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeGScore;
                fScore[neighbor] = gScore[neighbor] + HeuristicCostEstimate(neighbor, target);
            }
        }

        Debug.Log("Path not found");
        return null;
    }


    private List<Vector3Int> GetNeighbors(Vector3Int voxel)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                if (x == 0 && z == 0)
                {
                    continue;
                }

                int newX = voxel.x + x;
                int newZ = voxel.z + z;

                for (int i = 1; i <= maxMoveDiff; i++)
                {
                    int newY = voxel.y + i;

                    var neighborVoxelType = world.GetVoxelType(new Vector3Int(newX, newY, newZ));
                    if (neighborVoxelType == null)
                    {
                        Debug.Log("Voxel type is null at coordinates: " + newX + ", " + newY + ", " + newZ);
                        continue;
                    }

                    if (neighborVoxelType.state == State.SOLID)
                    {
                        neighbors.Add(new Vector3Int(newX, newY, newZ));
                        break;
                    }
                    //Debug.Log("neighborVoxelType is " + neighborVoxelType.state + " for coordinates: " + newX + ", " + newY + ", " + newZ);
                }
            }
        }
        //Debug.Log("Neighbors for voxel " + voxel + ": " + neighbors.Count);

        return neighbors;
    }


    private Vector3Int GetLowestFScore(HashSet<Vector3Int> openSet, Dictionary<Vector3Int, float> fScore)
    {
        Vector3Int lowest = new Vector3Int();
        float lowestScore = float.MaxValue;

        foreach (var voxel in openSet)
        {
            float score = fScore.GetValueOrDefault(voxel, float.MaxValue);
            if (score < lowestScore)
            {
                lowest = voxel;
                lowestScore = score;
            }
        }

        Debug.Log("Lowest fScore: " + lowestScore + " at voxel " + lowest); // Log the lowest fScore and its voxel

        return lowest;
    }

    private float HeuristicCostEstimate(Vector3Int a, Vector3Int b)
    {
        return Vector3Int.Distance(a, b);
    }

    private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        path.Add(current);

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
