using HarmonyLib;
using Il2Cpp;
using System;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // Remembers the exact tool used for each breakdown target, including explicit bare-hand choices.
    internal static class BreakdownToolMemoryController
    {
        private const string BreakdownMemoryActionKey = "Breakdown";
        private const string BreakdownWoodItemKey = "Wood";

        private static int _lastBreakdownTargetInstanceId = -1;
        private static string _lastBreakdownTargetHoverText;

        // Captures the hover identity before the breakdown panel replaces it with a generic target object.
        internal static void RecordTargetCandidate(GameObject targetObject, string hoverText)
        {
            if (targetObject == null || string.IsNullOrEmpty(hoverText))
            {
                return;
            }

            _lastBreakdownTargetInstanceId = targetObject.GetInstanceID();
            _lastBreakdownTargetHoverText = hoverText;
        }

        // Reports whether breakdown memory is enabled globally and for this activity.
        private static bool IsRememberBreakdownToolEnabled()
        {
            return FrozTLDMod.Settings != null &&
                   FrozTLDMod.Settings.Enabled &&
                   FrozTLDMod.Settings.RememberBreakdownTool;
        }


        // Builds the per-target memory key, grouping all wood limbs under the game's shared Wood activity.
        private static string GetItemKey(Panel_BreakDown panel)
        {
            if (panel == null ||
                panel.m_BreakDown == null ||
                panel.m_BreakDown.gameObject == null)
            {
                return string.Empty;
            }

            var targetObject = panel.m_BreakDown.gameObject;
            if (targetObject.tag == "Wood")
            {
                return BreakdownWoodItemKey;
            }

            if (_lastBreakdownTargetInstanceId == targetObject.GetInstanceID() &&
                !string.IsNullOrEmpty(_lastBreakdownTargetHoverText))
            {
                return _lastBreakdownTargetHoverText;
            }

            return CleanTargetName(targetObject.name);
        }

        // Removes placement suffixes from native object names before they become memory keys.
        private static string CleanTargetName(string targetName)
        {
            var cleanName = ToolMemoryHelpers.CleanText(targetName);
            if (cleanName.EndsWith(" (PLACED)", StringComparison.Ordinal))
            {
                cleanName = cleanName.Substring(0, cleanName.Length - " (PLACED)".Length);
            }

            return cleanName;
        }

        // Saves the tool actually selected when the player commits to the breakdown action.
        private static void RememberTool(string reason, Panel_BreakDown panel)
        {
            if (!IsRememberBreakdownToolEnabled())
            {
                return;
            }

            var itemKey = GetItemKey(panel);
            if (string.IsNullOrEmpty(itemKey))
            {
                return;
            }

            var selectedTool = panel.GetSelectedTool();
            var toolKey = ToolMemoryHelpers.DescribeGear(selectedTool);
            ToolMemoryStore.RememberTool(
                BreakdownMemoryActionKey,
                itemKey,
                toolKey,
                ToolMemoryHelpers.GetGearToolId(selectedTool),
                panel.m_SelectedToolItemIndex);
        }

        // Restores the exact tool, a same-type replacement, or the remembered no-tool index.
        private static void TryApplyRememberedTool(string reason, Panel_BreakDown panel)
        {
            if (!IsRememberBreakdownToolEnabled() || panel == null || panel.m_Tools == null)
            {
                return;
            }

            var itemKey = GetItemKey(panel);
            if (string.IsNullOrEmpty(itemKey))
            {
                return;
            }

            var record = ToolMemoryStore.FindRecord(BreakdownMemoryActionKey, itemKey);
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
            panel.UpdateToolLabel();
            panel.UpdateDurationLabel();
            panel.UpdateEstimatedCaloriesBurnedLabel();
            panel.UpdateIcons();
        }

        [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.Enable))]
        // Restores the remembered selection after vanilla populates the breakdown panel.
        private static class PanelBreakDownEnablePatch
        {
            private static void Postfix(Panel_BreakDown __instance, bool enable)
            {
                if (enable)
                {
                    TryApplyRememberedTool("Panel_BreakDown.Enable(true)", __instance);
                }
            }
        }

        [HarmonyPatch(typeof(Panel_BreakDown), nameof(Panel_BreakDown.OnBreakDown))]
        // Records the final selected tool immediately before breakdown begins.
        private static class PanelBreakDownOnBreakDownPatch
        {
            private static void Prefix(Panel_BreakDown __instance)
            {
                RememberTool("Panel_BreakDown.OnBreakDown", __instance);
            }
        }
    }
}
