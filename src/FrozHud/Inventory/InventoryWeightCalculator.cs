using Il2Cpp;
using Il2CppTLD.SaveState;
using System;
using System.Collections.Generic;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed class InventoryWeightCalculator
    {
        // Calculates live inventory totals and the worn/carried clothing split from Inventory.m_Items.
        internal InventoryCategoryWeightModel Calculate(MeasurementUnits measurementUnits)
        {
            var weights = new InventoryCategoryWeightModel(CreateEmptyTotals());
            var inventory = GameManager.GetInventoryComponent();
            var liveItems = Il2CppReflection.GetObjectMember(inventory, "m_Items");
            if (liveItems == null)
            {
                return weights;
            }

            // Live source: Inventory.m_Items updates immediately on pickup/drop,
            // while Panel_Inventory.m_FilteredInventoryList only reflects the
            // current UI filter. Zero-weight recipe cards, notes, and similar
            // records may exist in m_Items; they add no weight, so no special
            // exclusion rule is needed for the totals we display.
            foreach (var item in GearItemInterop.EnumerateIndexedList(liveItems))
            {
                var gear = GearItemInterop.GetGearItem(item);
                var categoryValue = Il2CppReflection.GetObjectMember(
                    Il2CppReflection.GetObjectMember(gear, "m_GearItemData"),
                    "m_Type");
                // Preserve the proven order of operations: convert each item into
                // the selected display unit, then add it to category totals. Final
                // truncation remains a presentation concern.
                var weight = GearItemInterop.GetGearWeight(gear, measurementUnits);
                var isClothing = ContainsCategory(GetCategoryKeys(categoryValue), "Clothing");

                // All counts every live inventory item exactly once. Items with
                // multiple categories, such as sticks, still only count once here.
                weights.AddToTotal("Button_FilterAll", weight);

                if (isClothing)
                {
                    if (IsWearingClothing(gear))
                    {
                        weights.ClothingWornWeight += weight;
                    }
                    else
                    {
                        weights.ClothingCarriedWeight += weight;
                    }
                }

                foreach (var categoryKey in GetCategoryKeys(categoryValue))
                {
                    foreach (var mapping in InventoryCategoryWeightModel.CategoryKeysByButtonName)
                    {
                        if (!ContainsCategory(mapping.Value, categoryKey))
                        {
                            continue;
                        }

                        // GearType.Other appears under the game's Tool sidebar
                        // button. Multi-category items contribute to each
                        // matching category, matching the backpack UI behavior.
                        weights.AddToTotal(mapping.Key, weight);
                    }
                }
            }

            return weights;
        }

        // Creates a zeroed entry for every category button the overlay can display.
        private static Dictionary<string, float> CreateEmptyTotals()
        {
            var totalsByButtonName = new Dictionary<string, float>();
            foreach (var buttonName in InventoryCategoryWeightModel.CategoryKeysByButtonName.Keys)
            {
                totalsByButtonName[buttonName] = 0f;
            }

            return totalsByButtonName;
        }

        // Uses the serialized wearing proxy because the live IL2CPP GearItem does not expose a reliable worn flag.
        private static bool IsWearingClothing(GearItem gear)
        {
            if (gear == null)
            {
                return false;
            }

            var serialized = gear.SerializeToString();
            return !string.IsNullOrEmpty(serialized) &&
                   serialized.Contains("\\\"m_WearingProxy\\\":true", StringComparison.Ordinal);
        }

        // Normalizes the reflected GearType value into individual category names.
        private static IEnumerable<string> GetCategoryKeys(object categoryValue)
        {
            if (categoryValue == null)
            {
                yield break;
            }

            var text = categoryValue.ToString();
            var dotIndex = text.LastIndexOf('.');
            if (dotIndex >= 0)
            {
                text = text.Substring(dotIndex + 1);
            }

            foreach (var part in text.Split(','))
            {
                var key = part.Trim();
                if (!string.IsNullOrEmpty(key))
                {
                    yield return key;
                }
            }
        }

        // Performs an exact category-name match without allocating another collection.
        private static bool ContainsCategory(IEnumerable<string> categories, string categoryKey)
        {
            foreach (var category in categories)
            {
                if (string.Equals(category, categoryKey, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
