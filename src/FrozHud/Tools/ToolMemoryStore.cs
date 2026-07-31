using Il2Cpp;
using System;
using System.Collections.Generic;

namespace FrozTLDMods.FrozTLDMod
{
    // Stores one remembered tool selection for a specific activity and target item.
    internal sealed class ActivityToolMemoryRecord
    {
        // Captures both the exact tool ID and its type/index so controllers can restore the best match.
        internal ActivityToolMemoryRecord(string actionKey, string itemKey, string toolKey, string toolId, int toolIndex)
        {
            ActionKey = actionKey;
            ItemKey = itemKey;
            ToolKey = toolKey;
            ToolId = toolId;
            ToolIndex = toolIndex;
        }

        internal string ActionKey { get; }
        internal string ItemKey { get; }
        internal string ToolKey { get; }
        internal string ToolId { get; }
        internal int ToolIndex { get; }
    }

    // Shared in-memory record store used by each activity-specific tool controller.
    internal static class ToolMemoryStore
    {
        private static readonly List<ActivityToolMemoryRecord> ActivityToolMemoryRecords = new List<ActivityToolMemoryRecord>();

        // Finds the remembered selection for one activity/item pair.
        internal static ActivityToolMemoryRecord FindRecord(string actionKey, string itemKey)
        {
            for (var index = 0; index < ActivityToolMemoryRecords.Count; index++)
            {
                var record = ActivityToolMemoryRecords[index];
                if (record.ActionKey == actionKey && record.ItemKey == itemKey)
                {
                    return record;
                }
            }

            return null;
        }

        // Finds the remembered tool in a native list using the stored activity/item record.
        internal static int FindToolIndex(
            string actionKey,
            string itemKey,
            Il2CppSystem.Collections.Generic.List<GearItem> tools,
            Func<GearItem, string> describeTool,
            Func<GearItem, string> getToolId)
        {
            return FindToolIndex(FindRecord(actionKey, itemKey), tools, describeTool, getToolId);
        }

        // Prefers the exact tool instance, then matches another item of the same tool type.
        internal static int FindToolIndex(
            ActivityToolMemoryRecord record,
            Il2CppSystem.Collections.Generic.List<GearItem> tools,
            Func<GearItem, string> describeTool,
            Func<GearItem, string> getToolId)
        {
            if (record == null || tools == null)
            {
                return -1;
            }

            if (!string.IsNullOrEmpty(record.ToolId))
            {
                for (var index = 0; index < tools.Count; index++)
                {
                    if (getToolId(tools[index]) == record.ToolId)
                    {
                        return index;
                    }
                }
            }

            for (var index = 0; index < tools.Count; index++)
            {
                if (describeTool(tools[index]) == record.ToolKey)
                {
                    return index;
                }
            }

            return -1;
        }

        // Reports whether a record intentionally represents using no tool.
        internal static bool IsNoToolRecord(ActivityToolMemoryRecord record)
        {
            return record != null && record.ToolKey == "none";
        }

        // Reports whether an activity/item pair intentionally remembers no tool.
        internal static bool IsNoToolRecord(string actionKey, string itemKey)
        {
            return IsNoToolRecord(FindRecord(actionKey, itemKey));
        }

        // Replaces the selection remembered for an activity/item pair.
        internal static void RememberTool(string actionKey, string itemKey, string toolKey, string toolId, int selectedIndex)
        {
            if (string.IsNullOrEmpty(actionKey) || string.IsNullOrEmpty(itemKey))
            {
                return;
            }

            RemoveRecord(actionKey, itemKey);

            if (string.IsNullOrEmpty(toolKey))
            {
                return;
            }

            ActivityToolMemoryRecords.Add(new ActivityToolMemoryRecord(actionKey, itemKey, toolKey, toolId, selectedIndex));
        }

        // Removes every stale record for an activity/item pair before a new value is stored.
        private static void RemoveRecord(string actionKey, string itemKey)
        {
            for (var index = ActivityToolMemoryRecords.Count - 1; index >= 0; index--)
            {
                var record = ActivityToolMemoryRecords[index];
                if (record.ActionKey == actionKey && record.ItemKey == itemKey)
                {
                    ActivityToolMemoryRecords.RemoveAt(index);
                }
            }
        }
    }
}
