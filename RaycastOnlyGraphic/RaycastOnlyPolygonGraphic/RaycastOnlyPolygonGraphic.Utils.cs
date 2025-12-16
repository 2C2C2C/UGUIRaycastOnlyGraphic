using System.Collections.Generic;

namespace UnityEngine.UI.RaycastOnlyGraphic
{
    public partial class RaycastOnlyPolygonGraphic
    {
        /// <summary>
        /// Only works for sorted clockwised pointts.
        /// </summary>
        /// <param name="points"></param>
        /// <returns>A list of index from input points.</returns>
        private static List<int> GenerateTriangles(IReadOnlyList<Vector2> points)
        {
            List<int> result = new();
            int restPointCount = points.Count;

            // If less than 3 points, no triangulation possible
            if (restPointCount < 3)
            {
                return result;
            }

            // Determine winding: clockwise if signed area is negative
            bool isClockwise = SignedArea(points) < 0f;

            List<int> restPointIndexList = new(restPointCount);
            for (int i = 0; i < restPointCount; i++)
            {
                restPointIndexList.Add(i);
            }

            // Main earcut loop
            int ear = 0;
            int stop = restPointCount;
            while (restPointCount > 2)
            {
                if (--stop <= 0)
                {
                    break;
                }

                // Find an ear
                if (IsEar(ear, points, restPointIndexList, isClockwise))
                {
                    // Cut off the ear
                    int prev = 0 == ear ? restPointCount - 1 : ear - 1;
                    int next = restPointCount - 1 == ear ? 0 : ear + 1;

                    // Add triangle
                    result.Add(restPointIndexList[prev]);
                    result.Add(restPointIndexList[ear]);
                    result.Add(restPointIndexList[next]);

                    // Remove ear vertex
                    restPointIndexList.RemoveAt(ear);
                    stop = --restPointCount;
                }
                else
                {
                    ++ear;
                }

                // Prevent out of bounds
                if (ear >= restPointCount)
                {
                    ear = 0;
                }
            }

            return result;
        }

        private static bool IsEar(int earFromRestPoints, IReadOnlyList<Vector2> points, IReadOnlyList<int> restPointIndices, bool isClockwise)
        {
            int restPointCount = restPointIndices.Count;
            int ear = restPointIndices[earFromRestPoints];
            int prev = 0 == earFromRestPoints ? restPointIndices[restPointCount - 1] : restPointIndices[earFromRestPoints - 1];
            int next = restPointCount - 1 == earFromRestPoints ? restPointIndices[0] : restPointIndices[earFromRestPoints + 1];

            Vector2 a = points[prev];
            Vector2 b = points[ear];
            Vector2 c = points[next];

            // Quick convexity test (depends on polygon winding)
            float eps = float.Epsilon;
            float crossZ = (b.x - a.x) * (c.y - b.y) - (b.y - a.y) * (c.x - b.x);
            bool isConvex = isClockwise ? (crossZ < -eps) : (crossZ > eps);
            if (isConvex)
            {
                // Check if any remaining point is inside the triangle
                for (int i = 0; i < restPointCount; i++)
                {
                    int pointIndex = restPointIndices[i];
                    if (pointIndex != prev &&
                        pointIndex != ear &&
                        pointIndex != next &&
                        PointInTriangle(points[pointIndex], a, b, c))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }

        private static float SignedArea(IReadOnlyList<Vector2> points)
        {
            float area = 0f;
            int n = points.Count;
            for (int i = 0; i < n; ++i)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % n];
                area += a.x * b.y - b.x * a.y;
            }
            return area * 0.5f;
        }

        private static List<Vector2> GenerateConvexHull(IReadOnlyList<Vector2> points)
        {
            List<Vector2> result = new();
            // Find left most point
            int leftMostIndex = 0;
            for (int i = 1, length = points.Count; i < length; i++)
            {
                if (points[leftMostIndex].x > points[i].x)
                {
                    leftMostIndex = i;
                }
            }
            result.Add(points[leftMostIndex]);
            Vector2 leftMostPoint = points[leftMostIndex];

            // Start from leftmost point, then find rest points
            List<Vector2> collinearPoints = new();
            Vector2 current = points[leftMostIndex];
            while (true)
            {
                bool hasTaraget = false;
                Vector2 nextTarget = default;
                for (int i = 0, length = points.Count; i < length; i++)
                {
                    Vector2 tempPoint = points[i];
                    if (V2PointApproximately(tempPoint, current))
                    {
                        continue;
                    }

                    if (hasTaraget)
                    {
                        Vector2 current2Target = nextTarget - current;
                        Vector2 current2Temp = tempPoint - current;
                        Vector3 corssResult = Vector3.Cross(current2Temp, current2Target);
                        float zValue = corssResult.z;
                        if (zValue > 0) // Find a farther point that makes a convex angle
                        {
                            nextTarget = tempPoint;

                            collinearPoints = new List<Vector2>();
                        }
                        else if (Mathf.Approximately(0, zValue)) // Handle collinear points(with current target)
                        {

                            if ((current - nextTarget).sqrMagnitude < (current - tempPoint).sqrMagnitude)
                            {
                                collinearPoints.Add(nextTarget); // Add and move to farther collinear point
                                nextTarget = tempPoint;
                            }
                            else
                            {
                                collinearPoints.Add(tempPoint); // Add a closer collinear point
                            }
                        }
                        else { } // Ah this point would make prev-target point into a concave point, do not add it :(
                    }
                    else
                    {
                        nextTarget = tempPoint;
                        hasTaraget = true;
                    }
                }

                if (hasTaraget)
                {
                    for (int i = 0, length = collinearPoints.Count; i < length; i++)
                    {
                        result.Add(collinearPoints[i]);
                    }

                    if (V2PointApproximately(nextTarget, leftMostPoint))
                    {
                        break;
                    }

                    result.Add(nextTarget);
                    current = nextTarget;
                    continue;
                }

                // If we cant find a next target, we are done
                break;
            }
            return result;
        }

        private static bool V2PointApproximately(Vector2 a, Vector2 b)
        {
            return Mathf.Approximately(a.x, b.x) &&
                Mathf.Approximately(a.y, b.y);
        }

        private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            Vector3 p2a = a - p;
            Vector3 p2b = b - p;
            Vector3 p2c = c - p;

            Vector3 cross1 = Vector3.Cross(p2a, p2b);
            Vector3 cross2 = Vector3.Cross(p2b, p2c);
            Vector3 cross3 = Vector3.Cross(p2c, p2a);

            float dot1 = Vector3.Dot(cross1, cross2);
            float dot2 = Vector3.Dot(cross2, cross3);
            float dot3 = Vector3.Dot(cross3, cross1);

            return dot1 >= 0 &&
                   dot2 >= 0 &&
                   dot3 >= 0;
        }

    }
}