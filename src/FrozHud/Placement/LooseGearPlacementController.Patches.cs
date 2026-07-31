using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal static partial class LooseGearPlacementController
    {
        [HarmonyPatch(typeof(PlayerManager), "DoPositionCheck")]
        // Scopes the targeted physics synchronization to one vanilla loose-gear placement pass.
        private static class PlayerManagerDoPositionCheckPatch
        {
            private static void Prefix(PlayerManager __instance)
            {
                BeginPlacementPhysicsSynchronization(__instance);
            }

            private static void Postfix(
                PlayerManager __instance,
                MeshLocationCategory __result)
            {
                RestoreInvalidPreviewAfterVanillaNudge(__instance, __result);
                EndPlacementPhysicsSynchronization(__instance);
            }

            private static void Finalizer(PlayerManager __instance)
            {
                EndPlacementPhysicsSynchronization(__instance);
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "ObjectToPlaceOverlapsWithObjectsThatBlockPlacement")]
        // Makes vanilla's initial overlap query observe the position assigned earlier in the frame.
        private static class PlayerManagerObjectToPlaceOverlapsWithObjectsThatBlockPlacementPatch
        {
            private static void Prefix(PlayerManager __instance, RaycastHit hit)
            {
                SynchronizeFirstPlacementOverlap(__instance, hit);
            }

            private static void Postfix(PlayerManager __instance, Collider __result)
            {
                CaptureFirstPlacementBlocker(__instance, __result);
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "CheckBoundsAgainstObjectsThatBlockPlacement")]
        // Refines vanilla's blocker result without replacing its world, wall, or support-surface checks.
        private static class PlayerManagerCheckBoundsAgainstObjectsThatBlockPlacementPatch
        {
            private static void Postfix(
                PlayerManager __instance,
                Vector3 worldPos,
                Vector3 localExtents,
                Quaternion rotation,
                RaycastHit targetHit,
                int mask,
                ref Collider __result)
            {
                FilterLooseGearBlockers(
                    __instance,
                    worldPos,
                    localExtents,
                    rotation,
                    targetHit,
                    mask,
                    ref __result);
            }
        }
    }
}
