using HarmonyLib;
using Il2Cpp;

namespace FrozTLDMods.FrozTLDMod
{
    // Remembers the selected ice-fishing hole tool. The normal activity hooks
    // do not see this panel, so this controller watches Panel_IceFishingHoleClear directly.
    internal static class IceFishingToolMemoryController
    {
        private const string IceFishingHoleActionKey = "IceFishingHole";
        private const string BuildHoleItemKey = "Build";
        private const string ClearHoleItemKey = "Clear";
        // Reports whether ice-fishing tool memory is enabled globally and for this activity.
        private static bool IsRememberIceFishingToolEnabled()
        {
            return FrozTLDMod.Settings != null &&
                   FrozTLDMod.Settings.Enabled &&
                   FrozTLDMod.Settings.RememberIceFishingTool;
        }

        // Stores the tool actually selected for building or clearing an ice-fishing hole.
        private static void RememberTool(string reason, Panel_IceFishingHoleClear panel, GearItem tool)
        {
            if (!IsRememberIceFishingToolEnabled() || panel == null)
            {
                return;
            }

            var itemKey = GetItemKey(panel);
            var toolKey = DescribeGear(tool);
            ToolMemoryStore.RememberTool(IceFishingHoleActionKey, itemKey, toolKey, DescribeGearId(tool), -1);
        }

        // Moves the remembered tool to index zero because this panel always defaults to its first entry.
        private static void TryPromoteRememberedTool(string reason, Panel_IceFishingHoleClear panel, bool refreshVisuals)
        {
            if (!IsRememberIceFishingToolEnabled() || panel == null || panel.m_AvailableTools == null)
            {
                return;
            }

            var itemKey = GetItemKey(panel);
            if (ToolMemoryStore.IsNoToolRecord(IceFishingHoleActionKey, itemKey))
            {
                panel.m_ToolUsed = null;
                if (refreshVisuals)
                {
                    panel.RefreshVisuals();
                }

                return;
            }

            var index = ToolMemoryStore.FindToolIndex(IceFishingHoleActionKey, itemKey, panel.m_AvailableTools, DescribeGear, DescribeGearId);
            if (index < 0)
            {
                return;
            }

            var rememberedTool = panel.m_AvailableTools[index];
            if (index > 0)
            {
                panel.m_AvailableTools.RemoveAt(index);
                panel.m_AvailableTools.Insert(0, rememberedTool);
            }

            panel.m_ToolUsed = rememberedTool;
            if (refreshVisuals)
            {
                panel.RefreshVisuals();
            }

        }
        // Separates memory for creating a new hole from clearing an existing one.
        private static string GetItemKey(Panel_IceFishingHoleClear panel)
        {
            return panel != null && panel.m_IsClearingIce ? ClearHoleItemKey : BuildHoleItemKey;
        }


        // Returns a stable human-readable key for an available ice-fishing tool.
        private static string DescribeGear(GearItem gear)
        {
            if (gear == null || gear.gameObject == null)
            {
                return "none";
            }

            return ToolMemoryHelpers.CleanText(gear.gameObject.name);
        }

        // Returns the persistent native instance ID used to prefer the same physical tool.
        private static string DescribeGearId(GearItem gear)
        {
            if (gear == null)
            {
                return string.Empty;
            }

            return gear.m_InstanceID.ToString();
        }
        [HarmonyPatch(typeof(Panel_IceFishingHoleClear), nameof(Panel_IceFishingHoleClear.Launch))]
        // Applies memory when the ice-fishing interaction first launches.
        private static class PanelIceFishingHoleClearLaunchPatch
        {
            private static void Postfix(Panel_IceFishingHoleClear __instance)
            {
                TryPromoteRememberedTool("Launch", __instance, true);
            }
        }

        [HarmonyPatch(typeof(Panel_IceFishingHoleClear), nameof(Panel_IceFishingHoleClear.Enable))]
        // Applies memory whenever the panel becomes enabled.
        private static class PanelIceFishingHoleClearEnablePatch
        {
            private static void Postfix(Panel_IceFishingHoleClear __instance, bool enable)
            {
                if (enable)
                {
                    TryPromoteRememberedTool("Enable(true)", __instance, true);
                }
            }
        }

        [HarmonyPatch(typeof(Panel_IceFishingHoleClear), nameof(Panel_IceFishingHoleClear.UpdateAvailableTools))]
        // Reapplies memory after vanilla rebuilds the available tool list.
        private static class PanelIceFishingHoleClearUpdateAvailableToolsPatch
        {
            private static void Postfix(Panel_IceFishingHoleClear __instance)
            {
                TryPromoteRememberedTool("UpdateAvailableTools", __instance, false);
            }
        }

        [HarmonyPatch(typeof(Panel_IceFishingHoleClear), nameof(Panel_IceFishingHoleClear.UseTool))]
        // Saves the concrete tool instance used by the native panel.
        private static class PanelIceFishingHoleClearUseToolPatch
        {
            private static void Prefix(Panel_IceFishingHoleClear __instance, GearItem gi)
            {
                RememberTool("UseTool", __instance, gi);
            }
        }
    }
}
