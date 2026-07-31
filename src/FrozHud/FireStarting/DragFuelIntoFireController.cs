using Il2Cpp;
using Il2CppTLD.Interactions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // Lets vanilla own fire targeting and hover visibility while a loose fuel
    // item is being positioned. The mod only narrows vanilla's interaction to
    // a feedable fireplace, rewrites the native fire hover text, hides the held
    // fuel, and consumes the interaction as an AddFuel action.
    internal static partial class DragFuelIntoFireController
    {
        private const float PromptValueRefreshSeconds = 0.25f;

        private static readonly List<ColliderState> InteractionQueryColliderStates =
            new List<ColliderState>();
        private static readonly List<RendererState> HiddenRendererStates =
            new List<RendererState>();
        private static readonly HashSet<int> HiddenRendererIds = new HashSet<int>();

        private static Collider[] _interactionQueryColliders = Array.Empty<Collider>();
        private static GearItem _interactionQueryColliderFuel;
        private static GearItem _sessionFuel;
        private static GearItem _targetFuel;
        private static GearItem _hiddenFuel;
        private static Panel_FeedFire _feedFirePanel;
        private static int _lastConsumedGearId;
        private static int _lastConsumedFrame = -1;
        private static bool _suppressingPlacementPutBackAudio;
        private static bool _feedingFuel;
        private static bool _runningNativeInteractionProcess;

        private static GearItem _promptValueFuel;
        private static Fire _promptValueFire;
        private static float _nextPromptValueRefreshTime;
        private static string _promptTitleText = string.Empty;
        private static string _promptTimeText = string.Empty;
        private static string _promptTempText = string.Empty;

        // Maintains the active loose-fuel session and mirrors vanilla's current fireplace target.
        internal static void Update()
        {
            var playerManager = GameManager.GetPlayerManagerComponent();
            var fuelGear = GetLooseFuelBeingPlaced(playerManager);
            if (!IsEnabled() || fuelGear == null)
            {
                EndFuelAddSession();
                return;
            }

            if (_sessionFuel != fuelGear)
            {
                BeginFuelAddSession(fuelGear);
            }

            RefreshTargetFromNativeInteraction(playerManager, fuelGear);
        }

        // Re-hides the preview after vanilla's late-frame renderer refresh while the prompt is active.
        internal static void LateUpdate()
        {
            if (_targetFuel == null || _targetFuel != _sessionFuel)
            {
                return;
            }

            var playerManager = GameManager.GetPlayerManagerComponent();
            var fuelGear = GetLooseFuelBeingPlaced(playerManager);
            if (fuelGear == _targetFuel)
            {
                // Vanilla placement refreshes its preview renderers late in the
                // frame. Reapply the cached hidden state after that refresh.
                HideFuelForPrompt(playerManager, fuelGear);
            }
        }

        // Reports whether the global mod and drag-fuel option are both enabled.
        private static bool IsEnabled()
        {
            return FrozTLDMod.Settings != null &&
                   FrozTLDMod.Settings.Enabled &&
                   FrozTLDMod.Settings.DragFuelIntoFire;
        }

        // Returns the current loose fuel only when this feature should participate in native interaction.
        private static bool TryGetFeatureFuel(PlayerManager playerManager, out GearItem fuelGear)
        {
            fuelGear = IsEnabled() ? GetLooseFuelBeingPlaced(playerManager) : null;
            return fuelGear != null;
        }

        // Identifies a non-inventory fuel item currently being moved through mesh placement.
        private static GearItem GetLooseFuelBeingPlaced(PlayerManager playerManager)
        {
            if (playerManager == null || playerManager.m_ObjectToPlace == null)
            {
                return null;
            }

            var gear = playerManager.m_ObjectToPlace.GetComponentInParent<GearItem>();
            return gear != null &&
                   !gear.m_InPlayerInventory &&
                   gear.m_FuelSourceItem != null
                ? gear
                : null;
        }

        // Starts tracking a newly held fuel item after restoring any previous session state.
        private static void BeginFuelAddSession(GearItem fuelGear)
        {
            EndFuelAddSession();
            _sessionFuel = fuelGear;
        }

        // Restores temporary state and clears all references associated with the current fuel gesture.
        private static void EndFuelAddSession()
        {
            ClearTarget();
            _sessionFuel = null;
            _interactionQueryColliderFuel = null;
            _interactionQueryColliders = Array.Empty<Collider>();
            ClearPromptValueCache();
        }

        // Temporarily removes the held fuel's colliders only while vanilla searches for an interaction target.
        private static void BeginNativeInteractionProcess(PlayerManager playerManager)
        {
            if (_runningNativeInteractionProcess ||
                !TryGetFeatureFuel(playerManager, out var fuelGear))
            {
                return;
            }

            _runningNativeInteractionProcess = true;
            CacheInteractionQueryColliders(fuelGear);
            for (var index = 0; index < _interactionQueryColliders.Length; index++)
            {
                DisableColliderForInteractionQuery(_interactionQueryColliders[index]);
            }
        }

        // Caches the held fuel's collider array once per fuel instance.
        private static void CacheInteractionQueryColliders(GearItem fuelGear)
        {
            if (_interactionQueryColliderFuel == fuelGear)
            {
                return;
            }

            _interactionQueryColliderFuel = fuelGear;
            _interactionQueryColliders = fuelGear.gameObject.GetComponentsInChildren<Collider>(true);
            if (InteractionQueryColliderStates.Capacity < _interactionQueryColliders.Length)
            {
                InteractionQueryColliderStates.Capacity = _interactionQueryColliders.Length;
            }
        }

        // Records and disables one collider so it cannot block the native fireplace query.
        private static void DisableColliderForInteractionQuery(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            InteractionQueryColliderStates.Add(
                new ColliderState(collider, collider.enabled));
            collider.enabled = false;
        }

        // Restores every collider changed for the native interaction query.
        private static void EndNativeInteractionProcess()
        {
            for (var index = 0; index < InteractionQueryColliderStates.Count; index++)
            {
                var state = InteractionQueryColliderStates[index];
                if (state.Collider != null)
                {
                    state.Collider.enabled = state.Enabled;
                }
            }

            InteractionQueryColliderStates.Clear();
            _runningNativeInteractionProcess = false;
        }

        // Updates the targeted fuel state from vanilla's active interaction and exact hover boundary.
        private static void RefreshTargetFromNativeInteraction(
            PlayerManager playerManager,
            GearItem fuelGear)
        {
            if (!TryResolveActiveFireTarget(
                    playerManager,
                    fuelGear,
                    out _,
                    out _))
            {
                ClearTarget();
                return;
            }

            _targetFuel = fuelGear;
            HideFuelForPrompt(playerManager, fuelGear);
        }

        // Resolves vanilla's current crosshair interaction only when it is an enabled, feedable fireplace.
        private static bool TryResolveActiveFireTarget(
            PlayerManager playerManager,
            GearItem fuelGear,
            out FireplaceInteraction fireplace,
            out Fire fire)
        {
            fireplace = null;
            fire = null;

            var interaction = playerManager != null ? playerManager.ActiveInteraction : null;
            if (interaction == null ||
                !playerManager.IsInteractionNearCrosshair ||
                !interaction.IsEnabled ||
                !interaction.CanInteract)
            {
                return false;
            }

            var interactiveObject = interaction.GetInteractiveObject();
            return TryResolveFeedableFire(interactiveObject, fuelGear, out fireplace, out fire);
        }

        // Converts an interactive object into its fireplace and fire components and validates the fuel.
        private static bool TryResolveFeedableFire(
            GameObject interactiveObject,
            GearItem fuelGear,
            out FireplaceInteraction fireplace,
            out Fire fire)
        {
            fireplace = interactiveObject != null
                ? interactiveObject.GetComponentInParent<FireplaceInteraction>()
                : null;
            fire = fireplace != null ? fireplace.Fire : null;

            return fireplace != null &&
                   fireplace.IsEnabled &&
                   fireplace.CanInteract &&
                   CanFeedFuel(fire, fuelGear);
        }

        // Applies the same wetness, tinder, fire-age, and feedability rules used by the fire panel.
        private static bool CanFeedFuel(Fire fire, GearItem gear)
        {
            if (fire == null || gear == null || !fire.CanFeedFire())
            {
                return false;
            }

            var fuel = gear.m_FuelSourceItem;
            return fuel != null &&
                   !fuel.m_IsTinder &&
                   !fuel.m_IsWet &&
                   fire.GetMinutesBurning() >= fuel.m_FireAgeMinutesBeforeAdding;
        }

        // Exits placement, adds the selected fuel through Fire.AddFuel, plays native audio, and consumes the item.
        private static bool TryFeedTargetedFire(PlayerManager playerManager)
        {
            if (!IsEnabled() || _feedingFuel)
            {
                return false;
            }

            var fuelGear = GetLooseFuelBeingPlaced(playerManager);
            if (fuelGear == null ||
                fuelGear != _sessionFuel ||
                !TryResolveActiveFireTarget(
                    playerManager,
                    fuelGear,
                    out var fireplace,
                    out var fire))
            {
                ClearTarget();
                return false;
            }

            _targetFuel = fuelGear;

            var gearId = fuelGear.GetInstanceID();
            if (_lastConsumedGearId == gearId && _lastConsumedFrame == Time.frameCount)
            {
                return true;
            }

            var inForge = fireplace.Forge != null;
            _lastConsumedGearId = gearId;
            _lastConsumedFrame = Time.frameCount;
            _feedingFuel = true;

            try
            {
                _suppressingPlacementPutBackAudio = true;
                try
                {
                    playerManager.ExitMeshPlacement();
                }
                finally
                {
                    _suppressingPlacementPutBackAudio = false;
                }

                fire.AddFuel(fuelGear, inForge);
                PlayFeedFireAudio();
                GearManager.DestroyGearObject(fuelGear);
                return true;
            }
            catch (Exception ex)
            {
                FrozTLDMod.Log?.Warning(
                    "Could not add loose fuel to fire: " +
                    ex.GetType().Name + ": " + ex.Message);
                return true;
            }
            finally
            {
                _feedingFuel = false;
                EndFuelAddSession();
            }
        }

        // Plays the same feed-fire sound configured on the native Panel_FeedFire instance.
        private static void PlayFeedFireAudio()
        {
            _feedFirePanel = PanelCache.Get(_feedFirePanel);
            if (_feedFirePanel == null ||
                _feedFirePanel.gameObject == null ||
                string.IsNullOrEmpty(_feedFirePanel.m_FeedFireAudio))
            {
                return;
            }

            GameAudioManager.PlaySound(
                _feedFirePanel.m_FeedFireAudio,
                _feedFirePanel.gameObject);
        }

        private readonly struct ColliderState
        {
            // Captures a collider and the enabled state that must be restored after native targeting.
            internal ColliderState(Collider collider, bool enabled)
            {
                Collider = collider;
                Enabled = enabled;
            }

            internal Collider Collider { get; }
            internal bool Enabled { get; }
        }

        private readonly struct RendererState
        {
            // Captures a renderer and the enabled state that must be restored after the prompt closes.
            internal RendererState(Renderer renderer, bool enabled)
            {
                Renderer = renderer;
                Enabled = enabled;
            }

            internal Renderer Renderer { get; }
            internal bool Enabled { get; }
        }

    }
}
