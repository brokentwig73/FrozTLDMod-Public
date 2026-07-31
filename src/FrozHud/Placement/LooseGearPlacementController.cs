using Il2Cpp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace FrozTLDMods.FrozTLDMod
{
    // Vanilla uses broad interaction/box colliders for loose-item placement.
    // That leaves visibly large gaps around round or irregular objects. We
    // preserve the vanilla query for discovery and world collisions, but use
    // cached mesh footprints for loose-gear-versus-loose-gear clearance.
    internal static partial class LooseGearPlacementController
    {
        private sealed class CachedFootprint
        {
            // Stores a gear type's horizontal visual hull and full local bounds for repeated placement tests.
            internal CachedFootprint(Vector2[] localHull, Bounds localBounds)
            {
                LocalHull = localHull;
                LocalBounds = localBounds;
            }

            internal Vector2[] LocalHull { get; }
            internal Bounds LocalBounds { get; }
        }

        internal const float MinimumClearanceMeters = 0.0005f;
        private const float DuplicatePointToleranceSquared = 0.0000000001f;
        private const float VanillaInvalidPreviewNudgeMeters = 0.01f;
        private const float VanillaInvalidPreviewNudgeToleranceMeters = 0.0005f;
        private static readonly Dictionary<string, CachedFootprint> FootprintCache = new(StringComparer.Ordinal);
        private static readonly List<Vector2> HeldWorldHull = new(32);
        private static readonly List<Vector2> BlockerWorldHull = new(32);
        private static readonly List<Collider> TemporarilyDisabledColliders = new(4);
        private static bool _rerunningVanillaCheck;
        private static PlayerManager _activePlacementCheck;
        private static bool _firstOverlapSynchronized;
        private static bool _firstOverlapCompleted;
        private static Vector3 _positionBeforeFirstOverlap;
        private static Vector3 _firstOverlapSurfaceNormal;
        private static Collider _firstOverlapSurface;
        private static Collider _firstOverlapBlocker;

        // Reports whether the global mod and placement-spacing option are enabled.
        private static bool IsEnabled()
        {
            return FrozTLDMod.Settings != null &&
                   FrozTLDMod.Settings.Enabled &&
                   FrozTLDMod.Settings.FixPlacementSpacing;
        }

        // Marks a vanilla loose-gear placement pass so its initial overlap query
        // can use the transform written earlier in the same frame.
        private static void BeginPlacementPhysicsSynchronization(PlayerManager playerManager)
        {
            _activePlacementCheck = null;
            _firstOverlapSynchronized = false;
            _firstOverlapCompleted = false;
            _positionBeforeFirstOverlap = Vector3.zero;
            _firstOverlapSurfaceNormal = Vector3.zero;
            _firstOverlapSurface = null;
            _firstOverlapBlocker = null;

            if (!IsEnabled() ||
                playerManager == null ||
                playerManager.m_ObjectToPlace == null ||
                playerManager.m_ObjectToPlace.GetComponentInParent<GearItem>() == null)
            {
                return;
            }

            _activePlacementCheck = playerManager;
        }

        // Synchronizes only vanilla's first overlap query. Its later raised-position
        // recheck intentionally retains vanilla's placement decision sequence.
        private static void SynchronizeFirstPlacementOverlap(
            PlayerManager playerManager,
            RaycastHit hit)
        {
            if (_firstOverlapSynchronized ||
                playerManager == null ||
                playerManager != _activePlacementCheck)
            {
                return;
            }

            _firstOverlapSynchronized = true;
            _positionBeforeFirstOverlap = playerManager.m_ObjectToPlace.transform.position;
            _firstOverlapSurfaceNormal = hit.normal;
            _firstOverlapSurface = hit.collider;
            Physics.SyncTransforms();
        }

        // Records the blocker returned by vanilla's first overlap query.
        private static void CaptureFirstPlacementBlocker(
            PlayerManager playerManager,
            Collider blocker)
        {
            if (_firstOverlapCompleted ||
                playerManager == null ||
                playerManager != _activePlacementCheck ||
                !_firstOverlapSynchronized)
            {
                return;
            }

            _firstOverlapCompleted = true;
            _firstOverlapBlocker = blocker;
        }

        // Vanilla moves invalid previews 10 mm away from the supporting surface.
        // For a separate non-gear blocker, restore only that visual displacement
        // after validation so the red preview remains aligned with its support.
        private static void RestoreInvalidPreviewAfterVanillaNudge(
            PlayerManager playerManager,
            MeshLocationCategory result)
        {
            if (result != MeshLocationCategory.InvalidTooClose ||
                playerManager == null ||
                playerManager != _activePlacementCheck ||
                playerManager.m_ObjectToPlace == null ||
                !_firstOverlapCompleted ||
                _firstOverlapSurface == null ||
                _firstOverlapBlocker == null ||
                _firstOverlapSurface == _firstOverlapBlocker ||
                _firstOverlapBlocker.GetComponentInParent<GearItem>() != null ||
                _firstOverlapSurfaceNormal.sqrMagnitude <= Mathf.Epsilon)
            {
                return;
            }

            var expectedNudge =
                _firstOverlapSurfaceNormal.normalized *
                VanillaInvalidPreviewNudgeMeters;
            var actualNudge =
                playerManager.m_ObjectToPlace.transform.position -
                _positionBeforeFirstOverlap;
            if (Vector3.Distance(actualNudge, expectedNudge) >
                VanillaInvalidPreviewNudgeToleranceMeters)
            {
                return;
            }

            playerManager.m_ObjectToPlace.transform.position =
                _positionBeforeFirstOverlap;
        }

        // Clears the per-pass state after vanilla finishes evaluating placement.
        private static void EndPlacementPhysicsSynchronization(PlayerManager playerManager)
        {
            if (playerManager != _activePlacementCheck)
            {
                return;
            }

            _activePlacementCheck = null;
            _firstOverlapSynchronized = false;
            _firstOverlapCompleted = false;
            _positionBeforeFirstOverlap = Vector3.zero;
            _firstOverlapSurfaceNormal = Vector3.zero;
            _firstOverlapSurface = null;
            _firstOverlapBlocker = null;
        }

        // Decides whether a vanilla loose-gear blocker is visually clear enough to ignore.
        private static bool ShouldExcludeLooseGearBlocker(PlayerManager playerManager, Collider blocker)
        {
            if (!IsEnabled() ||
                playerManager == null ||
                playerManager.m_ObjectToPlace == null ||
                blocker == null ||
                blocker.gameObject == null)
            {
                return false;
            }

            var heldGear = playerManager.m_ObjectToPlace.GetComponentInParent<GearItem>();
            var blockerGear = blocker.GetComponentInParent<GearItem>();
            if (heldGear == null || blockerGear == null || heldGear == blockerGear)
            {
                return false;
            }

            return TryMeasureVisualClearance(
                       playerManager,
                       blockerGear,
                       out var minimumSeparation,
                       out var overlaps,
                       out _,
                       out _) &&
                   !overlaps &&
                   minimumSeparation >= MinimumClearanceMeters;
        }

        // Measures three-dimensional separation using horizontal visual hulls and vertical mesh bounds.
        internal static bool TryMeasureVisualClearance(
            PlayerManager playerManager,
            GearItem blockerGear,
            out float minimumSeparation,
            out bool overlaps,
            out int heldHullPoints,
            out int blockerHullPoints)
        {
            minimumSeparation = float.PositiveInfinity;
            overlaps = false;
            heldHullPoints = 0;
            blockerHullPoints = 0;

            if (playerManager == null || playerManager.m_ObjectToPlace == null || blockerGear == null)
            {
                return false;
            }

            var heldGear = playerManager.m_ObjectToPlace.GetComponentInParent<GearItem>();
            if (heldGear == null || heldGear == blockerGear)
            {
                return false;
            }

            var heldFootprint = GetOrBuildFootprint(heldGear);
            var blockerFootprint = GetOrBuildFootprint(blockerGear);
            if (heldFootprint == null || blockerFootprint == null)
            {
                return false;
            }

            BuildWorldHull(heldGear.transform, heldFootprint, HeldWorldHull);
            BuildWorldHull(blockerGear.transform, blockerFootprint, BlockerWorldHull);
            heldHullPoints = HeldWorldHull.Count;
            blockerHullPoints = BlockerWorldHull.Count;
            if (heldHullPoints < 3 || blockerHullPoints < 3)
            {
                return false;
            }

            var horizontalOverlap = ConvexPolygonsOverlap(HeldWorldHull, BlockerWorldHull);
            var horizontalSeparation = horizontalOverlap
                ? 0f
                : GetPolygonSeparation(HeldWorldHull, BlockerWorldHull);

            GetWorldVerticalRange(heldGear.transform, heldFootprint.LocalBounds, out var heldMinY, out var heldMaxY);
            GetWorldVerticalRange(blockerGear.transform, blockerFootprint.LocalBounds, out var blockerMinY, out var blockerMaxY);
            var verticalSeparation = GetIntervalSeparation(heldMinY, heldMaxY, blockerMinY, blockerMaxY);

            overlaps = horizontalOverlap && verticalSeparation <= 0f;
            minimumSeparation = Mathf.Sqrt(
                (horizontalSeparation * horizontalSeparation) +
                (verticalSeparation * verticalSeparation));
            return true;
        }

        // Returns a cached mesh footprint, building it once for each gear prefab name.
        private static CachedFootprint GetOrBuildFootprint(GearItem gear)
        {
            var cacheKey = gear != null ? gear.name : null;
            if (string.IsNullOrEmpty(cacheKey))
            {
                return null;
            }

            if (FootprintCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            cached = BuildFootprint(gear);
            FootprintCache[cacheKey] = cached;
            return cached;
        }

        // Projects GPU vertex-buffer positions into a local XZ convex hull and
        // captures vertical bounds. The completed footprint is cached by gear type,
        // so synchronous GPU readback occurs only during its first construction.
        private static CachedFootprint BuildFootprint(GearItem gear)
        {
            var points = new List<Vector2>(256);
            var hasBounds = false;
            var localBounds = new Bounds(Vector3.zero, Vector3.zero);
            var rootWorldToLocal = gear.transform.worldToLocalMatrix;

            var meshFilters = gear.gameObject.GetComponentsInChildren<MeshFilter>(true);
            for (var index = 0; index < meshFilters.Length; index++)
            {
                var meshFilter = meshFilters[index];
                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                var meshToRoot = rootWorldToLocal * meshFilter.transform.localToWorldMatrix;
                AddMeshPointsFromAnyImportType(
                    meshFilter.sharedMesh,
                    meshToRoot,
                    points,
                    ref localBounds,
                    ref hasBounds);
            }

            var skinnedRenderers = gear.gameObject.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (var index = 0; index < skinnedRenderers.Length; index++)
            {
                var renderer = skinnedRenderers[index];
                if (renderer == null || renderer.sharedMesh == null)
                {
                    continue;
                }

                var meshToRoot = rootWorldToLocal * renderer.transform.localToWorldMatrix;
                AddMeshPointsFromAnyImportType(
                    renderer.sharedMesh,
                    meshToRoot,
                    points,
                    ref localBounds,
                    ref hasBounds);
            }

            if (!hasBounds)
            {
                return null;
            }

            var hull = BuildConvexHull(points);
            return hull.Count >= 3
                ? new CachedFootprint(hull.ToArray(), localBounds)
                : null;
        }

        // Reads the exact native vertex-buffer bytes used by Unity's renderer. This
        // works consistently whether or not the imported mesh has Read/Write enabled.
        private static void AddMeshPointsFromAnyImportType(
            Mesh mesh,
            Matrix4x4 meshToRoot,
            List<Vector2> points,
            ref Bounds localBounds,
            ref bool hasBounds)
        {
            if (mesh == null || !mesh.HasVertexAttribute(VertexAttribute.Position))
            {
                return;
            }

            var format = mesh.GetVertexAttributeFormat(VertexAttribute.Position);
            var dimension = mesh.GetVertexAttributeDimension(VertexAttribute.Position);
            var stream = mesh.GetVertexAttributeStream(VertexAttribute.Position);
            var offset = mesh.GetVertexAttributeOffset(VertexAttribute.Position);
            var stride = mesh.GetVertexBufferStride(stream);
            if (format != VertexAttributeFormat.Float32 || dimension < 3 || stride <= 0)
            {
                return;
            }

            GraphicsBuffer vertexBuffer = null;
            try
            {
                vertexBuffer = mesh.GetVertexBuffer(stream);
                if (vertexBuffer == null)
                {
                    return;
                }

                var vertexCount = mesh.vertexCount;
                var requestedByteCount = checked(vertexCount * stride);
                if (vertexBuffer.stride != stride || vertexBuffer.count < vertexCount)
                {
                    return;
                }

                var request = AsyncGPUReadback.Request(vertexBuffer, requestedByteCount, 0);
                request.WaitForCompletion();
                if (!request.done || request.hasError)
                {
                    return;
                }

                var availableByteCount = request.layerDataSize;
                var rawData = request.GetDataRaw(0);
                if (rawData == IntPtr.Zero || availableByteCount < requestedByteCount)
                {
                    return;
                }

                for (var index = 0; index < vertexCount; index++)
                {
                    var vertexOffset = checked((index * stride) + offset);
                    if (vertexOffset < 0 || vertexOffset + 12 > availableByteCount)
                    {
                        break;
                    }

                    var vertex = new Vector3(
                        ReadSingle(rawData, vertexOffset),
                        ReadSingle(rawData, vertexOffset + 4),
                        ReadSingle(rawData, vertexOffset + 8));
                    if (!IsFinite(vertex))
                    {
                        continue;
                    }

                    AddTransformedMeshPoint(
                        meshToRoot.MultiplyPoint3x4(vertex),
                        points,
                        ref localBounds,
                        ref hasBounds);
                }
            }
            finally
            {
                vertexBuffer?.Dispose();
            }

        }

        // Reads one native vertex component without allocating a managed byte array.
        private static float ReadSingle(IntPtr bytes, int offset)
        {
            return BitConverter.Int32BitsToSingle(Marshal.ReadInt32(bytes, offset));
        }

        // Rejects malformed GPU vertex data before it can corrupt the placement hull.
        private static bool IsFinite(Vector3 value)
        {
            return !float.IsNaN(value.x) &&
                   !float.IsInfinity(value.x) &&
                   !float.IsNaN(value.y) &&
                   !float.IsInfinity(value.y) &&
                   !float.IsNaN(value.z) &&
                   !float.IsInfinity(value.z);
        }

        // Adds the horizontal footprint point and expands the corresponding local bounds.
        private static void AddTransformedMeshPoint(
            Vector3 transformed,
            List<Vector2> points,
            ref Bounds localBounds,
            ref bool hasBounds)
        {
            points.Add(new Vector2(transformed.x, transformed.z));
            if (!hasBounds)
            {
                localBounds = new Bounds(transformed, Vector3.zero);
                hasBounds = true;
                return;
            }

            localBounds.Encapsulate(transformed);
        }

        // Builds a counter-clockwise convex hull with the monotonic-chain algorithm.
        private static List<Vector2> BuildConvexHull(List<Vector2> points)
        {
            points.Sort((left, right) =>
            {
                var xComparison = left.x.CompareTo(right.x);
                return xComparison != 0 ? xComparison : left.y.CompareTo(right.y);
            });

            var uniquePoints = new List<Vector2>(points.Count);
            for (var index = 0; index < points.Count; index++)
            {
                if (uniquePoints.Count == 0 ||
                    (points[index] - uniquePoints[uniquePoints.Count - 1]).sqrMagnitude > DuplicatePointToleranceSquared)
                {
                    uniquePoints.Add(points[index]);
                }
            }

            if (uniquePoints.Count < 3)
            {
                return uniquePoints;
            }

            var hull = new List<Vector2>(uniquePoints.Count * 2);
            for (var index = 0; index < uniquePoints.Count; index++)
            {
                while (hull.Count >= 2 &&
                       Cross(hull[hull.Count - 2], hull[hull.Count - 1], uniquePoints[index]) <= 0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(uniquePoints[index]);
            }

            var lowerCount = hull.Count;
            for (var index = uniquePoints.Count - 2; index >= 0; index--)
            {
                while (hull.Count > lowerCount &&
                       Cross(hull[hull.Count - 2], hull[hull.Count - 1], uniquePoints[index]) <= 0f)
                {
                    hull.RemoveAt(hull.Count - 1);
                }

                hull.Add(uniquePoints[index]);
            }

            hull.RemoveAt(hull.Count - 1);
            return hull;
        }

        // Reruns vanilla's placement query while temporarily skipping only visually clear loose-gear blockers.
        private static void FilterLooseGearBlockers(
            PlayerManager playerManager,
            Vector3 worldPos,
            Vector3 localExtents,
            Quaternion rotation,
            RaycastHit targetHit,
            int mask,
            ref Collider result)
        {
            if (_rerunningVanillaCheck || !IsEnabled() || result == null)
            {
                return;
            }

            TemporarilyDisabledColliders.Clear();
            try
            {
                while (result != null &&
                       result.enabled &&
                       !TemporarilyDisabledColliders.Contains(result) &&
                       ShouldExcludeLooseGearBlocker(playerManager, result))
                {
                    result.enabled = false;
                    TemporarilyDisabledColliders.Add(result);

                    _rerunningVanillaCheck = true;
                    try
                    {
                        result = playerManager.CheckBoundsAgainstObjectsThatBlockPlacement(
                            worldPos,
                            localExtents,
                            rotation,
                            targetHit,
                            mask);
                    }
                    finally
                    {
                        _rerunningVanillaCheck = false;
                    }
                }
            }
            finally
            {
                for (var index = TemporarilyDisabledColliders.Count - 1; index >= 0; index--)
                {
                    var collider = TemporarilyDisabledColliders[index];
                    if (collider != null)
                    {
                        collider.enabled = true;
                    }
                }

                TemporarilyDisabledColliders.Clear();
            }
        }

    }
}
