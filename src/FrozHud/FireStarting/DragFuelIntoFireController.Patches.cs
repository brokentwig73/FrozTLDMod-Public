using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.Interactions;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal static partial class DragFuelIntoFireController
    {
        [HarmonyPatch(typeof(PlayerManager), "ShouldSuppressCrosshairs")]
        // Keeps vanilla interaction processing alive while loose fuel is being positioned.
        private static class PlayerManagerShouldSuppressCrosshairsPatch
        {
            private static void Postfix(PlayerManager __instance, ref bool __result)
            {
                if (__result && TryGetFeatureFuel(__instance, out _))
                {
                    // InteractiveObjectsProcess is gated by this result before
                    // its own prefix runs. Permit vanilla interaction for the
                    // complete loose-fuel placement session; collider changes
                    // remain scoped to InteractiveObjectsProcess itself.
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(GearPlacePoint), nameof(GearPlacePoint.Update))]
        // Prevents generated cooking placement geometry from changing the normal fireplace hover boundary.
        private static class GearPlacePointUpdatePatch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(GearPlacePoint __instance)
            {
                var playerManager = GameManager.GetPlayerManagerComponent();
                if (!TryGetFeatureFuel(playerManager, out _) ||
                    __instance == null ||
                    __instance.m_ColliderObject == null ||
                    !__instance.m_ColliderObject.activeSelf)
                {
                    return;
                }

                // Vanilla GearPlacePoint.Update enables this generated cooking
                // collider only during mesh placement. A normal Fire Barrel
                // hover sees it inactive, so preserve that exact vanilla state
                // while a loose fuel item uses the interaction pipeline.
                Utils.SetActive(__instance.m_ColliderObject, false);
            }
        }

        [HarmonyPatch(typeof(Campfire), "Update")]
        // Reproduces the campfire's ordinary-hover collider state during the loose-fuel gesture.
        private static class CampfireUpdatePlacementBlockPatch
        {
            [HarmonyPriority(Priority.Last)]
            private static void Postfix(Campfire __instance)
            {
                var playerManager = GameManager.GetPlayerManagerComponent();
                if (!TryGetFeatureFuel(playerManager, out _) ||
                    __instance == null ||
                    __instance.m_PlacementBlockColliderObj == null)
                {
                    return;
                }

                // Native Campfire.Update activates this object when the player
                // is placing any mesh and this campfire is not itself being
                // placed. Its ordinary-hover branch sets the object inactive.
                // The add-fuel gesture deliberately runs native interaction
                // while fuel remains in mesh placement, so reproduce that
                // exact ordinary-hover result after Campfire.Update completes.
                Utils.SetActive(__instance.m_PlacementBlockColliderObj, false);
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "InteractiveObjectsProcess")]
        // Scopes held-fuel collider suppression to vanilla's interaction query itself.
        private static class PlayerManagerInteractiveObjectsProcessPatch
        {
            private static void Prefix(PlayerManager __instance)
            {
                BeginNativeInteractionProcess(__instance);
            }

            private static void Postfix()
            {
                EndNativeInteractionProcess();
            }
        }

        [HarmonyPatch(
            typeof(PlayerManager),
            "GetInteractiveObjectWithConstraints",
            new[] { typeof(GameObject) })]
        // Rejects non-fire interaction results while the player is dragging valid fuel.
        private static class PlayerManagerGetInteractiveObjectWithConstraintsPatch
        {
            private static void Postfix(PlayerManager __instance, ref GameObject __result)
            {
                if (!TryGetFeatureFuel(__instance, out var fuelGear) || __result == null)
                {
                    return;
                }

                if (!TryResolveFeedableFire(__result, fuelGear, out _, out _))
                {
                    // During this narrowly scoped placement gesture, only a
                    // feedable fireplace may become the active interaction.
                    __result = null;
                }
            }
        }

        [HarmonyPatch(typeof(Panel_HUD), nameof(Panel_HUD.SetHoverText))]
        // Lets vanilla decide prompt visibility and geometry, then substitutes the add-fuel presentation.
        private static class PanelHudSetHoverTextPatch
        {
            private static bool Prefix(
                ref string hoverText,
                GameObject itemUnderCrosshairs,
                out bool __state)
            {
                __state = false;
                var playerManager = GameManager.GetPlayerManagerComponent();
                if (!TryGetFeatureFuel(playerManager, out var fuelGear) ||
                    !TryResolveFeedableFire(
                        itemUnderCrosshairs,
                        fuelGear,
                        out _,
                        out var fire))
                {
                    return true;
                }

                UpdatePromptValueCache(fuelGear, fire);
                hoverText = _promptTitleText;
                __state = true;
                return true;
            }

            private static void Postfix(
                Panel_HUD __instance,
                GameObject itemUnderCrosshairs,
                bool __state)
            {
                if (!__state)
                {
                    return;
                }

                var playerManager = GameManager.GetPlayerManagerComponent();
                if (!TryGetFeatureFuel(playerManager, out var fuelGear) ||
                    !TryResolveFeedableFire(
                        itemUnderCrosshairs,
                        fuelGear,
                        out _,
                        out var fire))
                {
                    return;
                }

                _targetFuel = fuelGear;
                ApplyNativeHoverText(__instance, fuelGear, fire);
                HideFuelForPrompt(playerManager, fuelGear);
            }
        }

        [HarmonyPatch(typeof(PlayerManager), nameof(PlayerManager.InteractiveObjectsProcessInteraction))]
        // Converts a click on the targeted fireplace into the custom direct-feed operation.
        private static class PlayerManagerInteractiveObjectsProcessInteractionPatch
        {
            private static bool Prefix(PlayerManager __instance, ref bool __result)
            {
                if (!TryFeedTargetedFire(__instance))
                {
                    return true;
                }

                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "TintPreviewRenderers")]
        // Re-hides preview renderers after vanilla recolors and re-enables them.
        private static class PlayerManagerTintPreviewRenderersPatch
        {
            private static void Postfix(PlayerManager __instance)
            {
                if (_targetFuel == null || _targetFuel != _sessionFuel)
                {
                    return;
                }

                var fuelGear = GetLooseFuelBeingPlaced(__instance);
                if (fuelGear == _targetFuel)
                {
                    HideFuelForPrompt(__instance, fuelGear);
                }
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "AttemptToPlaceMesh")]
        // Consumes placement input as an add-fuel action when a valid fireplace is targeted.
        private static class PlayerManagerAttemptToPlaceMeshPatch
        {
            private static bool Prefix(PlayerManager __instance)
            {
                return !TryFeedTargetedFire(__instance);
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "PlayPutBackAudio")]
        // Suppresses only the normal put-back sound emitted while fuel is consumed by a fire.
        private static class PlayerManagerPlayPutBackAudioPatch
        {
            private static bool Prefix()
            {
                return !_suppressingPlacementPutBackAudio;
            }
        }

        [HarmonyPatch(typeof(PlayerManager), "CleanUpPlaceMesh")]
        // Ends abandoned placement sessions while preserving state during the feed operation itself.
        private static class PlayerManagerCleanUpPlaceMeshPatch
        {
            private static void Postfix()
            {
                if (!_feedingFuel)
                {
                    EndFuelAddSession();
                }
            }
        }
    }
}
