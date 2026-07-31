using Il2Cpp;
using System;

namespace FrozTLDMods.FrozTLDMod
{
    // Vanilla defaults to no tinder once tinder becomes optional, but it may
    // select Birch Bark at any skill level. Apply both preferences after the
    // native panel has rebuilt its list so they work for every fireplace type.
    internal static class FireStartTinderSelector
    {
        private const string BirchBarkGearName = "GEAR_BarkTinder";

        // Applies optional-tinder and Birch Bark preferences to the freshly rebuilt native list.
        internal static void Apply(Panel_FireStart panel)
        {
            var settings = FrozTLDMod.Settings;
            if (panel == null ||
                settings == null ||
                !settings.Enabled ||
                (!settings.DefaultTinderAfterFireStartingLevel3 && !settings.ExcludeBirchBarkTinder))
            {
                return;
            }

            try
            {
                var tinderList = panel.m_TinderList;
                if (tinderList == null || tinderList.Count == 0)
                {
                    return;
                }

                var currentIndex = panel.m_SelectedTinderIndex;
                var currentTinder = GetGearAt(tinderList, currentIndex);
                var preferredIndex = -1;

                if (settings.ExcludeBirchBarkTinder && IsBirchBark(currentTinder))
                {
                    preferredIndex = FindPreferredTinderIndex(tinderList, excludeBirchBark: true);
                    if (preferredIndex < 0)
                    {
                        preferredIndex = FindOrAddNoTinderIndex(tinderList);
                    }
                }
                else if (!IsRealGear(currentTinder) && settings.DefaultTinderAfterFireStartingLevel3)
                {
                    preferredIndex = FindPreferredTinderIndex(
                        tinderList,
                        settings.ExcludeBirchBarkTinder);
                }

                if (preferredIndex < 0 || preferredIndex == currentIndex)
                {
                    return;
                }

                SelectTinder(panel, tinderList, preferredIndex);
            }
            catch (Exception ex)
            {
                FrozTLDMod.Log?.Warning("Default tinder selection failed: " + ex.Message);
            }
        }

        // Finds the first real tinder entry allowed by the current Birch Bark preference.
        private static int FindPreferredTinderIndex(
            Il2CppSystem.Collections.Generic.List<GearItem> tinderList,
            bool excludeBirchBark)
        {
            for (var index = 0; index < tinderList.Count; index++)
            {
                var tinder = tinderList[index];
                if (IsRealGear(tinder) && (!excludeBirchBark || !IsBirchBark(tinder)))
                {
                    return index;
                }
            }

            return -1;
        }

        // Reuses or temporarily appends the null entry representing an explicit No Tinder selection.
        private static int FindOrAddNoTinderIndex(
            Il2CppSystem.Collections.Generic.List<GearItem> tinderList)
        {
            for (var index = 0; index < tinderList.Count; index++)
            {
                if (tinderList[index] == null)
                {
                    return index;
                }
            }

            var noTinderIndex = tinderList.Count;
            tinderList.Add(null);
            return noTinderIndex;
        }

        // Safely reads a tinder list entry by index.
        private static GearItem GetGearAt(
            Il2CppSystem.Collections.Generic.List<GearItem> tinderList,
            int index)
        {
            return index >= 0 && index < tinderList.Count ? tinderList[index] : null;
        }

        // Updates the selected index and row without rebuilding away a temporary null entry.
        private static void SelectTinder(
            Panel_FireStart panel,
            Il2CppSystem.Collections.Generic.List<GearItem> tinderList,
            int index)
        {
            // Match Panel_FireStart.IncreaseTinder/DecreaseTinder exactly:
            // update the index and visible row, then let the native Update()
            // refresh dependent labels through m_DirtyLabels. Calling Refresh()
            // here would rebuild m_TinderList and discard our temporary null.
            panel.m_SelectedTinderIndex = index;
            panel.m_SelectTinder?.SetGearItem(GetTinderComponent(tinderList, index));
            panel.m_DirtyLabels = true;
        }

        // Returns the FuelSourceItem consumed by the native tinder selection row.
        private static FuelSourceItem GetTinderComponent(
            Il2CppSystem.Collections.Generic.List<GearItem> tinderList,
            int index)
        {
            var tinder = GetGearAt(tinderList, index);
            return tinder != null ? tinder.GetComponent<FuelSourceItem>() : null;
        }

        // Distinguishes actual tinder gear from null or synthetic No Tinder entries.
        private static bool IsRealGear(GearItem gear)
        {
            return gear != null &&
                   gear.gameObject != null &&
                   !string.IsNullOrEmpty(gear.name) &&
                   gear.name.IndexOf("none", StringComparison.OrdinalIgnoreCase) < 0;
        }

        // Identifies Birch Bark by its canonical gear name.
        private static bool IsBirchBark(GearItem gear)
        {
            return gear != null &&
                   string.Equals(gear.name, BirchBarkGearName, StringComparison.OrdinalIgnoreCase);
        }
    }
}
