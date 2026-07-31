using Il2Cpp;
using System;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // The vanilla fire-start panel already includes a held lit torch/flare in
    // m_StarterList, but it still defaults to matches. We leave the game's list
    // order intact and only move the visible selection to the existing held item.
    internal sealed class FireStartHeldStarterSelector
    {
        private Panel_FireStart _panel;
        private int _lastPanelInstanceId;
        private string _lastPreferredGearName;
        private bool _selectedForCurrentOpen;

        // Watches the open fire-start panel and applies the held starter once per panel state.
        public void Update()
        {
            if (FrozTLDMod.Settings == null ||
                !FrozTLDMod.Settings.Enabled ||
                !FrozTLDMod.Settings.PreferHeldFirestarter)
            {
                ResetOpenState();
                return;
            }

            _panel = PanelCache.Get(_panel);
            if (!FireStartPanelInterop.IsPanelActive(_panel))
            {
                ResetOpenState();
                return;
            }

            var panelInstanceId = _panel.gameObject.GetInstanceID();
            if (panelInstanceId != _lastPanelInstanceId)
            {
                _lastPanelInstanceId = panelInstanceId;
                _selectedForCurrentOpen = false;
                _lastPreferredGearName = null;
            }

            TrySelectHeldStarter(_panel);
        }

        // Selects a held lit torch or flare already present in the native starter list.
        private void TrySelectHeldStarter(Panel_FireStart panel)
        {
            try
            {
                var heldGear = GetHeldGear();
                if (!IsHeldTorchOrFlareStarter(heldGear))
                {
                    _selectedForCurrentOpen = false;
                    _lastPreferredGearName = null;
                    return;
                }

                var heldName = heldGear.name;
                if (_selectedForCurrentOpen && _lastPreferredGearName == heldName)
                {
                    return;
                }

                var starterList = panel.m_StarterList;
                var preferredIndex = FindGearIndex(starterList, heldGear);
                if (preferredIndex < 0)
                {
                    return;
                }

                var currentIndex = panel.m_SelectedStarterIndex;
                if (currentIndex == preferredIndex)
                {
                    _selectedForCurrentOpen = true;
                    _lastPreferredGearName = heldName;
                    return;
                }

                SelectStarter(panel, preferredIndex, heldGear);
                _selectedForCurrentOpen = true;
                _lastPreferredGearName = heldName;
            }
            catch (Exception ex)
            {
                FrozTLDMod.Log?.Warning("Held firestarter selection failed: " + ex.Message);
            }
        }

        // Returns the gear instance currently held by the player.
        private static GearItem GetHeldGear()
        {
            var playerManager = GameManager.GetPlayerManagerComponent();
            return Il2CppReflection.GetObjectMember(playerManager, "m_ItemInHands") as GearItem;
        }

        // Restricts this preference to held torch/flare gear that can actually start a fire.
        private static bool IsHeldTorchOrFlareStarter(GearItem gear)
        {
            if (gear == null || gear.GetComponent<FireStarterItem>() == null)
            {
                return false;
            }

            return gear.GetComponent<TorchItem>() != null ||
                   gear.GetComponent<FlareItem>() != null;
        }

        // Locates the held starter by identity first, then by its matching gear representation.
        private static int FindGearIndex(
            Il2CppSystem.Collections.Generic.List<GearItem> list,
            GearItem target)
        {
            if (list == null || target == null)
            {
                return -1;
            }

            for (var index = 0; index < list.Count; index++)
            {
                if (IsSameGear(list[index], target))
                {
                    return index;
                }
            }

            return -1;
        }

        // Compares native list entries to the held item without relying solely on wrapper identity.
        private static bool IsSameGear(GearItem candidate, GearItem target)
        {
            if (candidate == null || target == null)
            {
                return false;
            }

            if (candidate == target)
            {
                return true;
            }

            return candidate.name == target.name &&
                   candidate.GetComponent<FireStarterItem>() != null &&
                   target.GetComponent<FireStarterItem>() != null;
        }

        // Updates the starter row through the same state changes used by the native selector.
        private static void SelectStarter(Panel_FireStart panel, int index, GearItem starter)
        {
            panel.m_SelectedStarterIndex = index;
            panel.m_SelectStarter?.SetGearItem(starter.GetComponent<FireStarterItem>());
            panel.m_DirtyLabels = true;
        }

        // Clears per-opening state after the panel closes or the feature is disabled.
        private void ResetOpenState()
        {
            _lastPanelInstanceId = 0;
            _selectedForCurrentOpen = false;
            _lastPreferredGearName = null;
        }
    }
}
