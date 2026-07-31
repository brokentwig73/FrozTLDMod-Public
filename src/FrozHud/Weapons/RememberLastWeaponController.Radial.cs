using Il2Cpp;
using System.Collections.Generic;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class RememberLastWeaponController
    {
        // Applies the exact remembered instance to every currently visible radial panel.
        private static void ApplyRememberedWeaponToRadialMenu(List<Panel_ActionsRadial> visibleRadialPanels)
        {
            var playerManager = GameManager.GetPlayerManagerComponent();
            if (playerManager == null)
            {
                return;
            }

            var rememberedGear = GetRememberedGear(playerManager);
            if (rememberedGear == null)
            {
                return;
            }

            foreach (var panel in visibleRadialPanels)
            {
                ApplyRememberedWeaponToRadialPanel(panel, rememberedGear);
            }
        }

        // Rewrites matching radial arms and refreshes condition text for the hovered arm.
        private static void ApplyRememberedWeaponToRadialPanel(Panel_ActionsRadial panel, GearItem rememberedGear)
        {
            var arms = Il2CppReflection.GetObjectMember(panel, "m_RadialArms");
            foreach (var arm in Il2CppList.Enumerate(arms))
            {
                var armGear = Il2CppReflection.GetObjectMember(arm, "m_GearItem") as GearItem;
                if (armGear == null ||
                    SafeGearName(armGear) != _rememberedWeapon.GearName ||
                    IsSameInstance(armGear, rememberedGear))
                {
                    continue;
                }

                if (!Il2CppReflection.SetObjectMember(arm, "m_GearItem", rememberedGear))
                {
                    continue;
                }

                if (IsArmHovered(arm))
                {
                    UpdateVisibleRadialGearStats(panel, rememberedGear);
                }
            }
        }

        // Filters the cached radial panels down to the instances currently visible to the player.
        private static void CollectVisibleRadialPanels(List<Panel_ActionsRadial> visiblePanels)
        {
            visiblePanels.Clear();
            for (var index = _knownRadialPanels.Count - 1; index >= 0; index--)
            {
                var panel = _knownRadialPanels[index];
                if (panel == null || panel.gameObject == null)
                {
                    _knownRadialPanels.RemoveAt(index);
                    continue;
                }

                if (IsPanelVisible(panel))
                {
                    visiblePanels.Add(panel);
                }
            }

            RefreshKnownRadialPanelsIfNeeded();
            if (visiblePanels.Count > 0)
            {
                return;
            }

            foreach (var panel in _knownRadialPanels)
            {
                if (IsPanelVisible(panel))
                {
                    visiblePanels.Add(panel);
                }
            }
        }

        // Discovers radial panels only while the cache is empty; valid instances remain cached.
        private static void RefreshKnownRadialPanelsIfNeeded()
        {
            if (_knownRadialPanels.Count > 0)
            {
                return;
            }

            if (Time.realtimeSinceStartup < _nextRadialPanelScanTime)
            {
                return;
            }

            _nextRadialPanelScanTime = Time.realtimeSinceStartup + RadialPanelScanIntervalSeconds;
            var panels = Resources.FindObjectsOfTypeAll<Panel_ActionsRadial>();
            foreach (var panel in panels)
            {
                if (panel != null && panel.gameObject != null)
                {
                    _knownRadialPanels.Add(panel);
                }
            }
        }

        // Distinguishes radial selections from hotkey selections so only the hotkey uses a same-type replacement.
        private static bool IsRadialWeaponSelectionRecent()
        {
            return Time.realtimeSinceStartup - _lastRadialWeaponUiTime <= RadialRecentWindowSeconds;
        }

        // Safely tests whether a radial panel is active and enabled.
        private static bool IsPanelVisible(Panel_ActionsRadial panel)
        {
            if (panel == null || panel.gameObject == null || !panel.gameObject.activeInHierarchy)
            {
                return false;
            }

            try
            {
                return panel.IsEnabled();
            }
            catch
            {
                return false;
            }
        }

        // Reads the native radial arm's hover flag.
        private static bool IsArmHovered(object arm)
        {
            return Il2CppReflection.GetObjectMember(arm, "m_IsHoveredOver") is bool value && value;
        }

        // Refreshes the radial condition labels after replacing the hovered weapon instance.
        private static void UpdateVisibleRadialGearStats(Panel_ActionsRadial panel, GearItem gear)
        {
            var condition = Mathf.RoundToInt(GetCondition(gear) * 100f).ToString();
            var conditionText = "[FFFFFF]" + condition + "%";
            SetLabelText(Il2CppReflection.GetObjectMember(panel, "m_GearConditionLabel"), conditionText);
            SetLabelText(Il2CppReflection.GetObjectMember(panel, "m_GearConditionCenteredLabel"), conditionText);
        }
    }
}
