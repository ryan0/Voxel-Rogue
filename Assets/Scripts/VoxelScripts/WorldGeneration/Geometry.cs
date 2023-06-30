using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//geometry helper class
public class Geometry
{
    public  Vector3Int CalculateClusterCenter(List<Vector3Int> cluster)
    {
        Vector3Int sum = Vector3Int.zero;
        foreach (var tower in cluster)
        {
            sum += tower;
        }
        return new Vector3Int(sum.x / cluster.Count, sum.y / cluster.Count, sum.z / cluster.Count);
    }

    //Find clusters within minimum distnace
    public  List<List<Vector3Int>> FindClusters(List<Vector3Int> towerLocations, float minDistance)
    {
        List<List<Vector3Int>> clusters = new List<List<Vector3Int>>();
        foreach (var tower in towerLocations)
        {
            bool addedToCluster = false;
            foreach (var cluster in clusters)
            {
                foreach (var existingTower in cluster)
                {
                    if (Vector3Int.Distance(existingTower, tower) < minDistance)
                    {
                        cluster.Add(tower);
                        addedToCluster = true;
                        break;
                    }
                }
                if (addedToCluster) break;
            }
            if (!addedToCluster)
            {
                clusters.Add(new List<Vector3Int> { tower });
            }
        }
        return clusters;
    }

    public  List<Vector3Int> FindConvexHull(List<Vector3Int> points)
    {
        if (points.Count < 3) return points;

        // Finding the leftmost point
        Vector3Int leftMost = points[0];
        foreach (var p in points)
        {
            if (p.x < leftMost.x) leftMost = p;
        }

        // Gift wrapping algorithm (Jarvis March)
        List<Vector3Int> hull = new List<Vector3Int>();
        Vector3Int current = leftMost;
        Vector3Int next;
        do
        {
            hull.Add(current);
            next = points[0];
            foreach (var p in points)
            {
                // If p is to the left of the line from current to next, update next
                int crossProduct = (next.x - current.x) * (p.z - current.z) - (next.z - current.z) * (p.x - current.x);
                if (crossProduct > 0 || (crossProduct == 0 && Vector3Int.Distance(current, p) > Vector3Int.Distance(current, next)))
                {
                    next = p;
                }
            }
            current = next;
        } while (current != leftMost);

        return hull;
    }


    public  bool IsPointInPolygonWithExtension(Vector3Int point, List<Vector3Int> convexHull, int padding)
    {
        // First check if the point is inside the polygon
        if (IsPointInPolygon(point, convexHull))
        {
            return true;
        }

        // Check if the point is within the padding area around the polygon
        for (int i = 0; i < convexHull.Count; i++)
        {
            Vector3Int vertex1 = convexHull[i];
            Vector3Int vertex2 = convexHull[(i + 1) % convexHull.Count]; // Next vertex, wrap around to 0 for the last one

            float distance = DistancePointToLineSegment(new Vector3(point.x, 0, point.z), new Vector3(vertex1.x, 0, vertex1.z), new Vector3(vertex2.x, 0, vertex2.z));
            if (distance <= padding)
            {
                return true;
            }
        }

        return false;
    }

    public  bool IsPointInPolygon(Vector3Int point, List<Vector3Int> polygon)
    {
        int count = 0;
        for (int i = 0; i < polygon.Count; i++)
        {
            Vector3Int a = polygon[i];
            Vector3Int b = polygon[(i + 1) % polygon.Count];

            if ((a.z <= point.z && b.z > point.z) || (b.z <= point.z && a.z > point.z))
            {
                float t = (float)(point.z - a.z) / (b.z - a.z);
                if (a.x + t * (b.x - a.x) < point.x)
                {
                    count++;
                }
            }
        }
        return count % 2 == 1;
    }

    public  float DistancePointToLineSegment(Vector3 point, Vector3 vertex1, Vector3 vertex2)
    {
        Vector3 line = vertex2 - vertex1;
        Vector3 pointToVertex1 = point - vertex1;

        float t = Mathf.Max(0, Mathf.Min(1, Vector3.Dot(pointToVertex1, line) / line.sqrMagnitude));
        Vector3 projection = vertex1 + t * line; // This is the projection of the point onto the line segment

        return Vector3.Distance(point, projection);
    }

    public  List<(Vector3Int, Vector3Int)> GenerateMinimumSpanningTree(List<Vector3Int> towerLocations)
    {
        List<(Vector3Int, Vector3Int)> mstEdges = new List<(Vector3Int, Vector3Int)>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        if (towerLocations.Count > 0)
        {
            Vector3Int current = towerLocations[0];
            visited.Add(current);

            while (visited.Count < towerLocations.Count)
            {
                float minDistance = float.MaxValue;
                Vector3Int nearestNeighbor = default;
                Vector3Int nearestVisited = default;

                foreach (Vector3Int tower in towerLocations)
                {
                    if (visited.Contains(tower))
                    {
                        continue;
                    }

                    foreach (Vector3Int visitedTower in visited)
                    {
                        float distance = Vector3Int.Distance(visitedTower, tower);

                        if (distance < minDistance)
                        {
                            minDistance = distance;
                            nearestNeighbor = tower;
                            nearestVisited = visitedTower;
                        }
                    }
                }

                mstEdges.Add((nearestVisited, nearestNeighbor));
                visited.Add(nearestNeighbor);
            }
        }

        return mstEdges;
    }


}

public class GrahamScan
{
    private static int CCW(Vector3Int a, Vector3Int b, Vector3Int c)
    {
        int area = (b.x - a.x) * (c.z - a.z) - (b.z - a.z) * (c.x - a.x);
        if (area < 0) return -1; // clockwise
        if (area > 0) return 1; // counter-clockwise
        return 0; // collinear
    }

    private static int DistanceSquared(Vector3Int a, Vector3Int b)
    {
        return (a.x - b.x) * (a.x - b.x) + (a.z - b.z) * (a.z - b.z);
    }

    private static int Compare(Vector3Int a, Vector3Int b, Vector3Int reference)
    {
        int order = CCW(reference, a, b);
        if (order == 0)
            return DistanceSquared(reference, a) < DistanceSquared(reference, b) ? -1 : 1;
        return order == -1 ? 1 : -1;
    }

    public static List<Vector3Int> ConvexHull(List<Vector3Int> points)
    {
        if (points.Count < 3)
            return null; // Convex hull not possible with less than 3 points

        Vector3Int reference = points[0];

        // Find the lowest point
        for (int i = 1; i < points.Count; i++)
        {
            if (points[i].z < reference.z || (points[i].z == reference.z && points[i].x < reference.x))
                reference = points[i];
        }

        // Sort points by polar angle with respect to reference point
        points.Sort((a, b) => Compare(a, b, reference));

        Stack<Vector3Int> hull = new Stack<Vector3Int>();
        hull.Push(points[0]);
        hull.Push(points[1]);

        for (int i = 2; i < points.Count; i++)
        {
            Vector3Int top = hull.Pop();
            while (CCW(hull.Peek(), top, points[i]) <= 0)
                top = hull.Pop();
            hull.Push(top);
            hull.Push(points[i]);
        }

        return new List<Vector3Int>(hull);
    }
}

