using System;
using System.Collections.Generic;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed class InventoryCategoryWeightModel
    {
        private readonly Dictionary<string, float> _totalsByButtonName;

        internal static readonly IReadOnlyDictionary<string, string[]> CategoryKeysByButtonName = new Dictionary<string, string[]>
        {
            { "Button_FilterAll", Array.Empty<string>() },
            { "Button_FilterFirestarting", new[] { "Firestarting" } },
            { "Button_FilterFirstAid", new[] { "FirstAid" } },
            { "Button_FilterClothing", new[] { "Clothing" } },
            { "Button_FilterFoodAndDrink", new[] { "Food" } },
            { "Button_FilterTool", new[] { "Tool", "Other" } },
            { "Button_FilterMaterial", new[] { "Material" } },
            { "Button_FilterDecor", new[] { "Decor", "Decoration" } },
        };

        // Wraps one calculated inventory snapshot and its clothing split totals.
        internal InventoryCategoryWeightModel(Dictionary<string, float> totalsByButtonName)
        {
            _totalsByButtonName = totalsByButtonName;
        }

        internal IReadOnlyDictionary<string, float> TotalsByButtonName => _totalsByButtonName;
        internal float ClothingCarriedWeight { get; set; }
        internal float ClothingWornWeight { get; set; }

        // Returns the total associated with a sidebar button, or zero when the button is unknown.
        internal float GetTotal(string buttonName)
        {
            return TotalsByButtonName.TryGetValue(buttonName, out var total) ? total : 0f;
        }

        // Adds weight only to categories represented by the inventory sidebar.
        internal void AddToTotal(string buttonName, float weight)
        {
            if (_totalsByButtonName.ContainsKey(buttonName))
            {
                _totalsByButtonName[buttonName] += weight;
            }
        }

    }
}
