using Il2Cpp;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // Adds compact weight totals to the native backpack category buttons.
    // The labels are parented to the buttons themselves, so they follow the
    // game's existing inventory layout and only need local offsets for tuning.
    internal sealed class BackpackCategoryWeightDisplay
    {
        private const float RefreshIntervalSeconds = 0.1f;

        private readonly Dictionary<string, CategoryWeightBinding> _bindingsByButtonName = new Dictionary<string, CategoryWeightBinding>();
        private readonly InventoryWeightCalculator _weightCalculator = new InventoryWeightCalculator();
        private Panel_Inventory _panel;
        private string _lastFailureMessage;
        private float _nextRefreshTime;

        // Periodically creates, updates, or hides the native inventory category labels.
        public void Update()
        {
            if (Time.unscaledTime < _nextRefreshTime)
            {
                return;
            }

            _nextRefreshTime = Time.unscaledTime + RefreshIntervalSeconds;

            try
            {
                _panel = PanelCache.Get(_panel);
                if (!ShouldShow(_panel))
                {
                    SetLabelsVisible(false);
                    return;
                }

                EnsureLabels(_panel);
                RefreshWeights();
                _lastFailureMessage = null;
            }
            catch (Exception ex)
            {
                var failureMessage = ex.GetType().Name + ": " + ex.Message;
                if (!string.Equals(_lastFailureMessage, failureMessage, StringComparison.Ordinal))
                {
                    FrozTLDMod.Log?.Warning("Backpack category weight display failed: " + failureMessage);
                    _lastFailureMessage = failureMessage;
                }
            }
        }

        // Determines whether the inventory panel and category-weight feature are currently active.
        private static bool ShouldShow(Panel_Inventory panel)
        {
            return FrozTLDMod.Settings != null &&
                   FrozTLDMod.Settings.Enabled &&
                   FrozTLDMod.Settings.ShowCategoryWeights &&
                   panel != null &&
                   panel.gameObject != null &&
                   panel.gameObject.activeInHierarchy &&
                   panel.IsEnabled();
        }

        // Creates one persistent set of overlay labels for each recognized native category button.
        private void EnsureLabels(Panel_Inventory panel)
        {
            var filterButtons = Il2CppReflection.GetObjectMember(panel, "m_FilterButtons");
            var templateLabel = Il2CppReflection.GetObjectMember(panel, "m_CategoryWeightLabel") as UILabel;
            if (filterButtons == null || templateLabel == null)
            {
                SetLabelsVisible(false);
                return;
            }

            foreach (var item in GearItemInterop.EnumerateIndexedList(filterButtons))
            {
                var button = item as UIButton;
                if (button == null || button.gameObject == null)
                {
                    continue;
                }

                if (!InventoryCategoryWeightModel.CategoryKeysByButtonName.ContainsKey(button.gameObject.name))
                {
                    continue;
                }

                if (_bindingsByButtonName.ContainsKey(button.gameObject.name))
                {
                    continue;
                }

                var labelParent = button.gameObject;
                _bindingsByButtonName[button.gameObject.name] = new CategoryWeightBinding(
                    button.gameObject,
                    labelParent,
                    CreateWeightLabel(labelParent, templateLabel, "FrozTLDMod_CategoryWeight", UIWidget.Pivot.TopRight, NGUIText.Alignment.Right),
                    button.gameObject.name == "Button_FilterAll"
                        ? null
                        : CreateWeightLabel(labelParent, templateLabel, "FrozTLDMod_CategoryWeightPercent", UIWidget.Pivot.TopLeft, NGUIText.Alignment.Left),
                    button.gameObject.name == "Button_FilterClothing"
                        ? CreateWeightLabel(labelParent, templateLabel, "FrozTLDMod_ClothingCarriedWeight", UIWidget.Pivot.BottomLeft, NGUIText.Alignment.Left)
                        : null,
                    button.gameObject.name == "Button_FilterClothing"
                        ? CreateWeightLabel(labelParent, templateLabel, "FrozTLDMod_ClothingWornWeight", UIWidget.Pivot.BottomRight, NGUIText.Alignment.Right)
                        : null);
            }
        }

        // Clones the native category label's font into a lightweight child label with the requested alignment.
        private static UILabel CreateWeightLabel(
            GameObject labelParent,
            UILabel templateLabel,
            string objectName,
            UIWidget.Pivot pivot,
            NGUIText.Alignment alignment)
        {
            var labelObject = new GameObject(objectName);
            labelObject.layer = labelParent.layer;
            labelObject.transform.parent = labelParent.transform;
            labelObject.transform.localScale = Vector3.one;
            labelObject.transform.localRotation = Quaternion.identity;
            labelObject.transform.localPosition = Vector3.zero;

            var label = labelObject.AddComponent<UILabel>();
            label.bitmapFont = templateLabel.bitmapFont;
            label.pivot = pivot;
            label.alignment = alignment;
            label.width = 64;
            label.height = 18;
            label.fontSize = templateLabel.fontSize;
            label.depth = templateLabel.depth + 20;
            label.text = string.Empty;
            return label;
        }

        // Recalculates live inventory totals and writes each category's weight, percent, and clothing split.
        private void RefreshWeights()
        {
            if (!MeasurementUnitProvider.TryGetCurrent(out var measurementUnits))
            {
                SetLabelsVisible(false);
                return;
            }

            var weights = _weightCalculator.Calculate(measurementUnits);
            if (weights.TotalsByButtonName.Count == 0)
            {
                SetLabelsVisible(false);
                return;
            }

            var allWeight = weights.GetTotal("Button_FilterAll");
            foreach (var entry in InventoryCategoryWeightModel.CategoryKeysByButtonName)
            {
                if (!_bindingsByButtonName.TryGetValue(entry.Key, out var binding) ||
                    binding.Label == null ||
                    binding.ButtonObject == null)
                {
                    continue;
                }

                binding.Label.gameObject.SetActive(true);
                ApplyTuning(binding);
                var weight = weights.GetTotal(entry.Key);
                binding.Label.text = FormatWeight(weight);

                RefreshPercentLabel(binding, entry.Key, weight, allWeight);

                if (entry.Key == "Button_FilterClothing")
                {
                    RefreshClothingSplitLabels(binding, weights.ClothingCarriedWeight, weights.ClothingWornWeight);
                }
            }
        }

        // Applies compiled font, color, opacity, and upper-right offsets to a category label pair.
        private static void ApplyTuning(CategoryWeightBinding binding)
        {
            var label = binding.Label;
            if (label == null || label.gameObject == null)
            {
                return;
            }

            var tuning = FrozTLDMod.HudTuning.Values;
            label.fontSize = tuning.CategoryWeightFontSize;
            label.height = Mathf.CeilToInt(tuning.CategoryWeightFontSize * 1.6f);
            label.color = new Color(0.98f, 0.98f, 0.98f, tuning.BackpackCategoryWeightOpacity);
            var upperRight = GetButtonUpperRightPosition(binding.ButtonObject);
            label.transform.localPosition = upperRight + new Vector3(
                tuning.CategoryWeightHorizontalOffsetFromUpperRight,
                tuning.CategoryWeightVerticalOffsetFromUpperRight,
                0f);

            var percentLabel = binding.PercentLabel;
            if (percentLabel != null && percentLabel.gameObject != null)
            {
                percentLabel.fontSize = tuning.CategoryWeightFontSize;
                percentLabel.height = label.height;
                percentLabel.color = label.color;
                percentLabel.transform.localPosition = label.transform.localPosition + new Vector3(
                    tuning.CategoryWeightPercentOffset.X,
                    tuning.CategoryWeightPercentOffset.Y,
                    0f);
            }
        }

        // Displays a category's share of total carried weight; the All category intentionally has no percent.
        private static void RefreshPercentLabel(CategoryWeightBinding binding, string buttonName, float weight, float allWeight)
        {
            var percentLabel = binding.PercentLabel;
            if (percentLabel == null || percentLabel.gameObject == null)
            {
                return;
            }

            percentLabel.gameObject.SetActive(buttonName != "Button_FilterAll");
            percentLabel.text = buttonName == "Button_FilterAll"
                ? string.Empty
                : FormatPercentOfTotal(weight, allWeight);
        }

        // Updates the carried and worn values shown along the bottom of the Clothing button.
        private static void RefreshClothingSplitLabels(CategoryWeightBinding binding, float carriedWeight, float wornWeight)
        {
            var carriedLabel = binding.ClothingCarriedLabel;
            var wornLabel = binding.ClothingWornLabel;
            if (carriedLabel == null || wornLabel == null || binding.ButtonObject == null)
            {
                return;
            }

            var tuning = FrozTLDMod.HudTuning.Values;
            ApplyBottomClothingLabelTuning(carriedLabel, binding.ButtonObject, leftSide: true, tuning);
            ApplyBottomClothingLabelTuning(wornLabel, binding.ButtonObject, leftSide: false, tuning);
            carriedLabel.gameObject.SetActive(true);
            wornLabel.gameObject.SetActive(true);
            carriedLabel.text = FormatWeight(carriedWeight);
            wornLabel.text = FormatWeight(wornWeight);
        }

        // Mirrors the configured inset from opposite bottom corners of the Clothing button.
        private static void ApplyBottomClothingLabelTuning(
            UILabel label,
            GameObject buttonObject,
            bool leftSide,
            HudTuning tuning)
        {
            label.fontSize = tuning.CategoryWeightFontSize;
            label.color = new Color(0.98f, 0.98f, 0.98f, tuning.BackpackCategoryWeightOpacity);

            var size = GetButtonSize(buttonObject);
            var xInset = Mathf.Abs(tuning.CategoryWeightHorizontalOffsetFromUpperRight);
            var yInset = Mathf.Abs(tuning.CategoryWeightVerticalOffsetFromUpperRight);
            var bottomCorner = leftSide
                ? new Vector3(size.x * -0.5f, size.y * -0.5f, 0f)
                : new Vector3(size.x * 0.5f, size.y * -0.5f, 0f);

            label.transform.localPosition = bottomCorner + new Vector3(leftSide ? xInset : -xInset, yInset, 0f);
        }

        // Formats weight exactly as the game's two-decimal category readout.
        private static string FormatWeight(float weight)
        {
            return GearItemInterop.TruncateToTwoDecimals(weight).ToString("0.00");
        }

        // Calculates a whole-number percentage from the same truncated values shown to the player.
        private static string FormatPercentOfTotal(float weight, float allWeight)
        {
            var displayedTotal = GearItemInterop.TruncateToTwoDecimals(allWeight);
            if (displayedTotal <= 0f)
            {
                return "0%";
            }

            var displayedCategory = GearItemInterop.TruncateToTwoDecimals(weight);
            return Mathf.RoundToInt((displayedCategory / displayedTotal) * 100f).ToString("0") + "%";
        }

        // Returns the upper-right corner in the button's local coordinate system.
        private static Vector3 GetButtonUpperRightPosition(GameObject buttonObject)
        {
            var size = GetButtonSize(buttonObject);
            return new Vector3(size.x * 0.5f, size.y * 0.5f, 0f);
        }

        // Finds the largest native widget under a category button, excluding labels created by this mod.
        private static Vector2 GetButtonSize(GameObject buttonObject)
        {
            var widgets = buttonObject.GetComponentsInChildren<UIWidget>(true);
            var bestSize = Vector2.zero;
            var bestArea = 0;
            foreach (var widget in widgets)
            {
                if (widget == null ||
                    widget.gameObject == null ||
                    widget.width <= 0 ||
                    widget.height <= 0 ||
                    widget.gameObject.name == "FrozTLDMod_CategoryWeight" ||
                    widget.gameObject.name == "FrozTLDMod_CategoryWeightPercent" ||
                    widget.gameObject.name == "FrozTLDMod_ClothingCarriedWeight" ||
                    widget.gameObject.name == "FrozTLDMod_ClothingWornWeight")
                {
                    continue;
                }

                var area = widget.width * widget.height;
                if (area > bestArea)
                {
                    bestArea = area;
                    bestSize = new Vector2(widget.width, widget.height);
                }
            }

            return bestSize;
        }

        // Shows or hides every label without destroying bindings needed when the inventory reopens.
        private void SetLabelsVisible(bool visible)
        {
            foreach (var binding in _bindingsByButtonName.Values)
            {
                var label = binding.Label;
                if (label != null && label.gameObject != null)
                {
                    label.gameObject.SetActive(visible);
                }

                if (binding.ClothingCarriedLabel != null && binding.ClothingCarriedLabel.gameObject != null)
                {
                    binding.ClothingCarriedLabel.gameObject.SetActive(visible);
                }

                if (binding.ClothingWornLabel != null && binding.ClothingWornLabel.gameObject != null)
                {
                    binding.ClothingWornLabel.gameObject.SetActive(visible);
                }

                if (binding.PercentLabel != null && binding.PercentLabel.gameObject != null)
                {
                    binding.PercentLabel.gameObject.SetActive(visible);
                }
            }
        }

        private sealed class CategoryWeightBinding
        {
            // Keeps one category button and all labels attached to it together for later updates.
            public CategoryWeightBinding(
                GameObject buttonObject,
                GameObject labelParent,
                UILabel label,
                UILabel percentLabel,
                UILabel clothingCarriedLabel,
                UILabel clothingWornLabel)
            {
                ButtonObject = buttonObject;
                LabelParent = labelParent;
                Label = label;
                PercentLabel = percentLabel;
                ClothingCarriedLabel = clothingCarriedLabel;
                ClothingWornLabel = clothingWornLabel;
            }

            public GameObject ButtonObject { get; }
            public GameObject LabelParent { get; }
            public UILabel Label { get; }
            public UILabel PercentLabel { get; }
            public UILabel ClothingCarriedLabel { get; }
            public UILabel ClothingWornLabel { get; }
        }

    }
}
