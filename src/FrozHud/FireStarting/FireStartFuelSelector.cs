using HarmonyLib;
using Il2Cpp;
using System;

namespace FrozTLDMods.FrozTLDMod
{
    // Vanilla may choose any available fuel when the fire-start panel opens.
    // Panel_FireStart.Enable(true) rebuilds the native lists, resets the fuel
    // index to zero, and refreshes the panel for every Campfire and WoodStove.
    // Apply after that shared lifecycle so the actual panel instance selects
    // its existing stick entry without changing the list or its ordering.
    internal static class FireStartFuelSelector
    {
        private const string StickGearName = "GEAR_Stick";

        // Selects the existing stick entry after the native panel has rebuilt its fuel list.
        internal static void Apply(Panel_FireStart panel)
        {
            if (panel == null ||
                FrozTLDMod.Settings == null ||
                !FrozTLDMod.Settings.Enabled ||
                !FrozTLDMod.Settings.DefaultStickForFireStarting)
            {
                return;
            }

            try
            {
                var fuelList = panel.m_FuelList;
                if (fuelList == null || fuelList.Count == 0)
                {
                    return;
                }

                var stickIndex = FindStickIndex(fuelList);
                if (stickIndex < 0)
                {
                    return;
                }

                if (panel.m_SelectedFuelIndex != stickIndex)
                {
                    panel.m_SelectedFuelIndex = stickIndex;
                    panel.Refresh();
                }
            }
            catch (Exception ex)
            {
                FrozTLDMod.Log?.Warning("Default stick selection failed: " + ex.Message);
            }
        }

        // Finds the first stick in the native fuel list without reordering any entries.
        private static int FindStickIndex(Il2CppSystem.Collections.Generic.List<GearItem> fuelList)
        {
            for (var index = 0; index < fuelList.Count; index++)
            {
                var gear = fuelList[index];
                if (gear != null &&
                    string.Equals(gear.name, StickGearName, StringComparison.OrdinalIgnoreCase))
                {
                    return index;
                }
            }

            return -1;
        }
    }

    [HarmonyPatch(typeof(Panel_FireStart), nameof(Panel_FireStart.Enable), new Type[] { typeof(bool) })]
    // Applies all default fire-start selections after every native panel opening.
    internal static class PanelFireStartEnableDefaultsPatch
    {
        private static void Postfix(Panel_FireStart __instance, bool enable)
        {
            if (enable)
            {
                FireStartFuelSelector.Apply(__instance);
                FireStartTinderSelector.Apply(__instance);
            }
        }
    }
}
