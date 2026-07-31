using Il2Cpp;
using Il2CppTLD.SaveState;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class FrozTLDModOverlay
    {
        // Draws the backpack icon with current and maximum carry weights.
        private void DrawBackpackWeightGauge(Rect itemRect, float hudAlpha)
        {
            EnsureStyles();

            var tuning = GetHudTuning();
            var texture = GetDefaultBackpackTexture();
            if (texture == null)
            {
                return;
            }

            var oldColor = GUI.color;
            var iconSize = Mathf.Min(itemRect.width, itemRect.height) * tuning.BackpackWeightIconScalePercent * 0.01f;
            var iconRect = new Rect(
                itemRect.center.x - iconSize * 0.5f,
                itemRect.center.y - iconSize * 0.5f,
                iconSize,
                iconSize);

            GUI.color = GetHudColor(tuning.BackpackWeightIconColorHex, tuning.BackpackWeightIconOpacity * hudAlpha);
            GUI.DrawTexture(iconRect, texture, ScaleMode.ScaleToFit, true);

            var textHeight = Mathf.Max(12f, tuning.BackpackWeightFontSize * 1.35f);
            var textRect = new Rect(
                iconRect.center.x - itemRect.width * 0.7f + tuning.BackpackWeightHorizontalOffset,
                iconRect.center.y - textHeight * 0.5f + tuning.BackpackWeightVerticalOffset,
                itemRect.width * 1.4f,
                textHeight);
            _lowerHudValueStyle.fontSize = tuning.BackpackWeightFontSize;
            var isOverweight = GearItemInterop.TruncateToTwoDecimals(_cachedInventoryWeight) >
                               GearItemInterop.TruncateToTwoDecimals(_cachedMaxCarryCapacity);
            _lowerHudValueStyle.normal.textColor = GetBackpackWeightFontColor(tuning, tuning.BackpackWeightFontOpacity * hudAlpha, isOverweight);
            GUI.color = Color.white;
            GUI.Label(textRect, FormatOneDecimalWeight(_cachedInventoryWeight), _lowerHudValueStyle);

            var maxTextRect = new Rect(
                iconRect.center.x - itemRect.width * 0.7f + tuning.BackpackWeightHorizontalOffset,
                iconRect.center.y - textHeight * 0.5f + tuning.BackpackMaxVerticalOffset,
                itemRect.width * 1.4f,
                textHeight);
            _lowerHudValueStyle.normal.textColor = GetHudColor(tuning.BackpackMaxFontColor, tuning.BackpackWeightFontOpacity * hudAlpha);
            GUI.Label(maxTextRect, FormatOneDecimalWeight(_cachedMaxCarryCapacity), _lowerHudValueStyle);

            GUI.color = oldColor;
        }

        // Chooses normal or overweight text color while preserving the configured opacity.
        private static Color GetBackpackWeightFontColor(HudTuning tuning, float alpha, bool isOverweight)
        {
            var colorText = isOverweight ? tuning.BackpackOverweightFontColor : tuning.BackpackWeightFontColorHex;
            if (!string.IsNullOrEmpty(colorText) && colorText[0] != '#')
            {
                colorText = "#" + colorText;
            }

            if (ColorUtility.TryParseHtmlString(colorText, out var color))
            {
                color.a = alpha;
                return color;
            }

            return new Color(0f, 0f, 0f, alpha);
        }

        // Refreshes live carried weight and capacity at a throttled interval only while needed.
        private void UpdateInventoryWeightCacheIfNeeded()
        {
            if (Time.unscaledTime < _nextInventoryWeightRefreshTime)
            {
                return;
            }

            _nextInventoryWeightRefreshTime = Time.unscaledTime + InventoryWeightRefreshIntervalSeconds;
            if (!ShouldRefreshInventoryWeightCache())
            {
                return;
            }

            if (!MeasurementUnitProvider.TryGetCurrent(out var measurementUnits))
            {
                return;
            }

            _cachedInventoryWeight = CalculateInventoryWeight(measurementUnits);
            _cachedMaxCarryCapacity = CalculateEffectiveCarryCapacity(measurementUnits);
        }

        // Reports whether an enabled HUD or inventory overlay currently consumes weight data.
        private bool ShouldRefreshInventoryWeightCache()
        {
            if (FrozTLDMod.Settings == null || !FrozTLDMod.Settings.Enabled)
            {
                return false;
            }

            if (FrozTLDMod.ShouldRenderBackpackWeight(_stickyDesired))
            {
                return true;
            }

            return FrozTLDMod.Settings.ShowCategoryWeights && IsInventoryPanelActive();
        }

        // Reports whether the native inventory panel is open.
        private static bool IsInventoryPanelActive()
        {
            _cachedPanelInventory = PanelCache.Get(_cachedPanelInventory);
            return _cachedPanelInventory != null &&
                   _cachedPanelInventory.gameObject != null &&
                   _cachedPanelInventory.gameObject.activeInHierarchy &&
                   _cachedPanelInventory.IsEnabled();
        }

        // Returns total live inventory weight from the shared category calculator.
        private static float CalculateInventoryWeight(MeasurementUnits measurementUnits)
        {
            var inventory = GameManager.GetInventoryComponent();
            var liveItems = Il2CppReflection.GetObjectMember(inventory, "m_Items");
            if (liveItems == null)
            {
                return 0f;
            }

            var total = 0f;
            foreach (var item in GearItemInterop.EnumerateIndexedList(liveItems))
            {
                total += GearItemInterop.GetGearWeight(GearItemInterop.GetGearItem(item), measurementUnits);
            }

            return total;
        }

        // Returns effective carry capacity in the player's selected display units.
        private static float CalculateEffectiveCarryCapacity(MeasurementUnits measurementUnits)
        {
            var encumber = GameManager.GetEncumberComponent();
            if (encumber == null)
            {
                return 0f;
            }

            return GearItemInterop.TryGetItemWeight(encumber.GetEffectiveCarryCapacityKG(), measurementUnits, out var weight) ? weight : 0f;
        }

        // Formats backpack weight to the configured whole-number display form.
        private static string FormatWeight(float weight)
        {
            return GearItemInterop.TruncateToTwoDecimals(weight).ToString("0.00");
        }

        // Formats carry values with one decimal place for normal HUD display.
        private static string FormatOneDecimalWeight(float weight)
        {
            return weight.ToString("0.0");
        }

        // Lazily loads the embedded backpack artwork used by the gauge.
        private Texture2D GetDefaultBackpackTexture()
        {
            if (_defaultBackpackTexture != null)
            {
                return _defaultBackpackTexture;
            }

            _defaultBackpackTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.ico_Radial_pack.png");
            return _defaultBackpackTexture;
        }
    }
}
