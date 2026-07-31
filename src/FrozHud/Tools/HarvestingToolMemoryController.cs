using HarmonyLib;
using Il2Cpp;
using System;

namespace FrozTLDMods.FrozTLDMod
{
    // Remembers one carcass-harvesting tool choice across harvesting and quartering panels.
    internal static class HarvestingToolMemoryController
    {
        private const string AnimalHarvestingMemoryActionKey = "AnimalHarvesting";
        private const string AnimalHarvestingMemoryItemKey = "Carcass";

        // Reports whether harvesting memory is enabled globally and for this activity.
        private static bool IsRememberHarvestingToolEnabled()
        {
            return FrozTLDMod.Settings != null &&
                   FrozTLDMod.Settings.Enabled &&
                   FrozTLDMod.Settings.RememberAnimalHarvestingTool;
        }


        // Stores the tool selected when harvesting or quartering begins.
        private static void RememberTool(string reason, Panel_BodyHarvest panel)
        {
            if (!IsRememberHarvestingToolEnabled() || panel == null)
            {
                return;
            }

            var selectedTool = panel.GetSelectedTool();
            var toolKey = ToolMemoryHelpers.DescribeGear(selectedTool);
            ToolMemoryStore.RememberTool(
                AnimalHarvestingMemoryActionKey,
                AnimalHarvestingMemoryItemKey,
                toolKey,
                ToolMemoryHelpers.GetGearToolId(selectedTool),
                panel.m_SelectedToolItemIndex);
        }

        // Restores the exact tool, a same-type replacement, or the remembered no-tool choice.
        private static void TryApplyRememberedTool(string reason, Panel_BodyHarvest panel)
        {
            if (!IsRememberHarvestingToolEnabled() || panel == null || panel.m_Tools == null)
            {
                return;
            }

            var record = ToolMemoryStore.FindRecord(AnimalHarvestingMemoryActionKey, AnimalHarvestingMemoryItemKey);
            if (record == null)
            {
                return;
            }

            var noToolRecord = ToolMemoryStore.IsNoToolRecord(record);
            var toolIndex = noToolRecord
                ? record.ToolIndex
                : ToolMemoryStore.FindToolIndex(record, panel.m_Tools, ToolMemoryHelpers.DescribeGear, ToolMemoryHelpers.GetGearToolId);
            if (!noToolRecord && toolIndex < 0)
            {
                return;
            }

            if (panel.m_SelectedToolItemIndex == toolIndex)
            {
                return;
            }

            panel.m_SelectedToolItemIndex = toolIndex;
            panel.RefreshToolSelection();
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), nameof(Panel_BodyHarvest.Enable), new Type[] { typeof(bool) })]
        // Restores the remembered selection after the standard panel opening.
        private static class PanelBodyHarvestEnablePatch
        {
            private static void Postfix(Panel_BodyHarvest __instance, bool enable)
            {
                if (enable)
                {
                    TryApplyRememberedTool("Panel_BodyHarvest.Enable(true)", __instance);
                }
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), nameof(Panel_BodyHarvest.Enable), new Type[] { typeof(bool), typeof(BodyHarvest), typeof(bool), typeof(ComingFromScreenCategory) })]
        // Restores the remembered selection after the body-specific panel opening.
        private static class PanelBodyHarvestEnableWithBodyPatch
        {
            private static void Postfix(Panel_BodyHarvest __instance, bool enable)
            {
                if (enable)
                {
                    TryApplyRememberedTool("Panel_BodyHarvest.Enable(true, ...)", __instance);
                }
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), nameof(Panel_BodyHarvest.RefreshTools))]
        // Reapplies memory when vanilla rebuilds the available tool list.
        private static class PanelBodyHarvestRefreshToolsPatch
        {
            private static void Postfix(Panel_BodyHarvest __instance)
            {
                TryApplyRememberedTool("Panel_BodyHarvest.RefreshTools", __instance);
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), nameof(Panel_BodyHarvest.MakeDefaultSelections))]
        // Overrides vanilla defaults after they have been selected.
        private static class PanelBodyHarvestMakeDefaultSelectionsPatch
        {
            private static void Postfix(Panel_BodyHarvest __instance)
            {
                TryApplyRememberedTool("Panel_BodyHarvest.MakeDefaultSelections", __instance);
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnHarvest))]
        // Saves the final tool before a normal harvest operation starts.
        private static class PanelBodyHarvestOnHarvestPatch
        {
            private static void Prefix(Panel_BodyHarvest __instance)
            {
                RememberTool("Panel_BodyHarvest.OnHarvest", __instance);
            }
        }

        [HarmonyPatch(typeof(Panel_BodyHarvest), nameof(Panel_BodyHarvest.OnQuarter))]
        // Saves the final tool before a quartering operation starts.
        private static class PanelBodyHarvestOnQuarterPatch
        {
            private static void Prefix(Panel_BodyHarvest __instance)
            {
                RememberTool("Panel_BodyHarvest.OnQuarter", __instance);
            }
        }
    }
}
