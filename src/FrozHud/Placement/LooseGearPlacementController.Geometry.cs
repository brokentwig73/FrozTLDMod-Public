using System.Collections.Generic;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal static partial class LooseGearPlacementController
    {
        // Returns the signed two-dimensional cross product used by hull construction.
        private static float Cross(Vector2 origin, Vector2 first, Vector2 second)
        {
            return ((first.x - origin.x) * (second.y - origin.y)) -
                   ((first.y - origin.y) * (second.x - origin.x));
        }

        // Transforms a cached local footprint into world XZ coordinates for the current placement pose.
        private static void BuildWorldHull(Transform transform, CachedFootprint footprint, List<Vector2> output)
        {
            output.Clear();
            for (var index = 0; index < footprint.LocalHull.Length; index++)
            {
                var localPoint = footprint.LocalHull[index];
                var worldPoint = transform.TransformPoint(new Vector3(localPoint.x, 0f, localPoint.y));
                output.Add(new Vector2(worldPoint.x, worldPoint.z));
            }
        }

        // Tests convex hull overlap with the separating-axis theorem.
        private static bool ConvexPolygonsOverlap(List<Vector2> first, List<Vector2> second)
        {
            return !HasSeparatingAxis(first, second) && !HasSeparatingAxis(second, first);
        }

        // Reports whether any edge normal from one polygon separates the two hulls.
        private static bool HasSeparatingAxis(List<Vector2> axesFrom, List<Vector2> other)
        {
            for (var edgeIndex = 0; edgeIndex < axesFrom.Count; edgeIndex++)
            {
                var start = axesFrom[edgeIndex];
                var end = axesFrom[(edgeIndex + 1) % axesFrom.Count];
                var edge = end - start;
                var axis = new Vector2(-edge.y, edge.x);
                var axisLength = axis.magnitude;
                if (axisLength <= Mathf.Epsilon)
                {
                    continue;
                }

                axis /= axisLength;
                ProjectPolygon(axesFrom, axis, out var firstMin, out var firstMax);
                ProjectPolygon(other, axis, out var secondMin, out var secondMax);
                if (firstMax < secondMin || secondMax < firstMin)
                {
                    return true;
                }
            }

            return false;
        }

        // Projects every polygon vertex onto an axis and returns its scalar interval.
        private static void ProjectPolygon(List<Vector2> polygon, Vector2 axis, out float minimum, out float maximum)
        {
            minimum = Vector2.Dot(polygon[0], axis);
            maximum = minimum;
            for (var index = 1; index < polygon.Count; index++)
            {
                var projection = Vector2.Dot(polygon[index], axis);
                minimum = Mathf.Min(minimum, projection);
                maximum = Mathf.Max(maximum, projection);
            }
        }

        // Finds the shortest edge-to-edge distance between two non-overlapping convex polygons.
        private static float GetPolygonSeparation(List<Vector2> first, List<Vector2> second)
        {
            var minimumSquared = float.PositiveInfinity;
            for (var firstIndex = 0; firstIndex < first.Count; firstIndex++)
            {
                var firstStart = first[firstIndex];
                var firstEnd = first[(firstIndex + 1) % first.Count];
                for (var secondIndex = 0; secondIndex < second.Count; secondIndex++)
                {
                    var secondStart = second[secondIndex];
                    var secondEnd = second[(secondIndex + 1) % second.Count];
                    minimumSquared = Mathf.Min(minimumSquared, PointSegmentDistanceSquared(firstStart, secondStart, secondEnd));
                    minimumSquared = Mathf.Min(minimumSquared, PointSegmentDistanceSquared(firstEnd, secondStart, secondEnd));
                    minimumSquared = Mathf.Min(minimumSquared, PointSegmentDistanceSquared(secondStart, firstStart, firstEnd));
                    minimumSquared = Mathf.Min(minimumSquared, PointSegmentDistanceSquared(secondEnd, firstStart, firstEnd));
                }
            }

            return Mathf.Sqrt(minimumSquared);
        }

        // Returns squared distance from a point to the closest point on a line segment.
        private static float PointSegmentDistanceSquared(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= Mathf.Epsilon)
            {
                return (point - start).sqrMagnitude;
            }

            var amount = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return (point - (start + (segment * amount))).sqrMagnitude;
        }

        // Transforms all local bound corners to obtain the object's current world-space vertical range.
        private static void GetWorldVerticalRange(
            Transform transform,
            Bounds localBounds,
            out float minimum,
            out float maximum)
        {
            minimum = float.PositiveInfinity;
            maximum = float.NegativeInfinity;
            var center = localBounds.center;
            var extents = localBounds.extents;

            for (var x = -1; x <= 1; x += 2)
            {
                for (var y = -1; y <= 1; y += 2)
                {
                    for (var z = -1; z <= 1; z += 2)
                    {
                        var localCorner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        var worldCorner = transform.TransformPoint(localCorner);
                        minimum = Mathf.Min(minimum, worldCorner.y);
                        maximum = Mathf.Max(maximum, worldCorner.y);
                    }
                }
            }
        }

        // Returns the gap between two scalar intervals, or zero when they overlap.
        private static float GetIntervalSeparation(float firstMin, float firstMax, float secondMin, float secondMax)
        {
            return Mathf.Max(0f, Mathf.Max(firstMin - secondMax, secondMin - firstMax));
        }
    }
}
