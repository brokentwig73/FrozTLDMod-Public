using HarmonyLib;
using Il2Cpp;
using Il2CppTLD.UI.Scroll;
using System;

namespace FrozTLDMods.FrozTLDMod
{
    // Remembers the exact tool used for each blueprint and reapplies it whenever crafting rebuilds its selector.
    internal static class CraftingToolMemoryController
    {
        private const string CraftingMemoryActionKey = "Crafting";

        // Reports whether crafting memory is enabled globally and for this activity.
        private static bool IsRememberCraftingToolEnabled()
        {
            return FrozTLDMod.Settings != null &&
                   FrozTLDMod.Settings.Enabled &&
                   FrozTLDMod.Settings.RememberCraftingTool;
        }

        // Builds a stable memory key from crafted gear, decoration, or the blueprint itself.
        private static string GetItemKey(BlueprintData blueprintData)
        {
            if (blueprintData == null)
            {
                return string.Empty;
            }

            var craftedGear = ToolMemoryHelpers.DescribeGear(blueprintData.m_CraftedResultGear);
            if (craftedGear != "none")
            {
                return craftedGear;
            }

            var craftedDecoration = DescribeDecoration(blueprintData.m_CraftedResultDecoration);
            if (craftedDecoration != "none")
            {
                return craftedDecoration;
            }

            return ToolMemoryHelpers.CleanText(blueprintData.name);
        }

        // Returns a stable key for a decoration crafting result.
        private static string DescribeDecoration(DecorationItem decoration)
        {
            if (decoration == null || decoration.gameObject == null)
            {
                return "none";
            }

            return ToolMemoryHelpers.CleanText(decoration.gameObject.name);
        }

        // Saves a crafting tool using its native identity and normalized type key.
        private static void RememberTool(string actionKey, string itemKey, GearItem tool)
        {
            RememberTool(actionKey, itemKey, ToolMemoryHelpers.DescribeGear(tool), ToolMemoryHelpers.GetGearToolId(tool), -1);
        }

        // Forwards a fully described crafting selection to the shared memory store.
        private static void RememberTool(string actionKey, string itemKey, string toolKey, string toolId, int selectedIndex)
        {
            ToolMemoryStore.RememberTool(actionKey, itemKey, toolKey, toolId, selectedIndex);
        }
        // Searches the active tool selectors for the exact remembered tool, then a same-type replacement.
        private static void TryApplyRememberedTool(string reason, Panel_Crafting panel)
        {
            if (!IsRememberCraftingToolEnabled() || panel == null || panel.SelectedBPI == null)
            {
                return;
            }

            var itemKey = GetItemKey(panel.SelectedBPI);
            var record = ToolMemoryStore.FindRecord(CraftingMemoryActionKey, itemKey);
            if (record == null)
            {
                return;
            }

            var selectors = panel.GetComponentsInChildren<CraftingRequirementMultiTool>(true);
            foreach (var selector in selectors)
            {
                if (selector == null || !selector.IsEnabled() || selector.m_ToolOptions == null)
                {
                    continue;
                }

                if (ToolMemoryStore.IsNoToolRecord(record))
                {
                    for (var optionIndex = 0; optionIndex < selector.m_ToolOptions.Count; optionIndex++)
                    {
                        var option = selector.m_ToolOptions[optionIndex];
                        if (option.m_GearItem != null)
                        {
                            continue;
                        }

                        ApplyRememberedTool(reason, panel, selector, record, optionIndex);
                        return;
                    }

                    continue;
                }

                if (!string.IsNullOrEmpty(record.ToolId))
                {
                    for (var optionIndex = 0; optionIndex < selector.m_ToolOptions.Count; optionIndex++)
                    {
                        var option = selector.m_ToolOptions[optionIndex];
                        if (ToolMemoryHelpers.GetGearToolId(option.m_GearItem) != record.ToolId)
                        {
                            continue;
                        }

                        ApplyRememberedTool(reason, panel, selector, record, optionIndex);
                        return;
                    }
                }

                for (var optionIndex = 0; optionIndex < selector.m_ToolOptions.Count; optionIndex++)
                {
                    var option = selector.m_ToolOptions[optionIndex];
                    var optionToolKey = ToolMemoryHelpers.DescribeGear(option.m_GearItem);
                    if (optionToolKey != record.ToolKey)
                    {
                        continue;
                    }

                    ApplyRememberedTool(reason, panel, selector, record, optionIndex);
                    return;
                }
            }
        }

        // Updates the native selector and notifies the panel so dependent text and timing refresh correctly.
        private static void ApplyRememberedTool(
            string reason,
            Panel_Crafting panel,
            CraftingRequirementMultiTool selector,
            ActivityToolMemoryRecord record,
            int optionIndex)
        {
            if (selector.m_SelectedIndex == optionIndex)
            {
                return;
            }

            selector.m_SelectedIndex = optionIndex;
            selector.RefreshDisplayed();
            panel.OnSelectedToolChanged();
        }
        // Records the tool that actually reaches CraftingOperation.StartCrafting, not merely a browsed selection.
        private static void RememberCraftingOperationTool(
            BlueprintData blueprintData,
            GearItem tool)
        {
            if (IsRememberCraftingToolEnabled())
            {
                RememberTool(CraftingMemoryActionKey, GetItemKey(blueprintData), tool);
            }
        }
        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.Enable), new Type[] { typeof(bool) })]
        // Reapplies tool memory after the standard crafting panel opening.
        private static class PanelCraftingEnablePatch
        {
            private static void Postfix(Panel_Crafting __instance, bool enable)
            {
                if (enable)
                {
                    TryApplyRememberedTool("Panel_Crafting.Enable(true)", __instance);
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.Enable), new Type[] { typeof(bool), typeof(bool) })]
        // Reapplies tool memory after the crafting panel opens with restore-state arguments.
        private static class PanelCraftingEnableWithRestorePatch
        {
            private static void Postfix(Panel_Crafting __instance, bool enable)
            {
                if (enable)
                {
                    TryApplyRememberedTool("Panel_Crafting.Enable(true, ...)", __instance);
                }
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.UpdateSelected))]
        // Reapplies memory when the selected blueprint changes.
        private static class PanelCraftingUpdateSelectedPatch
        {
            private static void Postfix(Panel_Crafting __instance, ScrollBehaviour scrollBehaviour, ScrollBehaviourItem selectedItem, int selectedIndex)
            {
                TryApplyRememberedTool("Panel_Crafting.UpdateSelected", __instance);
            }
        }

        [HarmonyPatch(typeof(CraftingOperation), nameof(CraftingOperation.StartCrafting))]
        // Saves the tool that the game actually passes into the crafting operation.
        private static class CraftingOperationStartCraftingPatch
        {
            private static void Prefix(
                BlueprintData blueprintData,
                GearItem tool)
            {
                RememberCraftingOperationTool(blueprintData, tool);
            }
        }

        [HarmonyPatch(typeof(Panel_Crafting), nameof(Panel_Crafting.CraftingEnd))]
        // Reapplies the remembered tool after crafting returns to the blueprint screen.
        private static class PanelCraftingEndPatch
        {
            private static void Postfix(Panel_Crafting __instance)
            {
                TryApplyRememberedTool("Panel_Crafting.CraftingEnd", __instance);
            }
        }
    }
}
