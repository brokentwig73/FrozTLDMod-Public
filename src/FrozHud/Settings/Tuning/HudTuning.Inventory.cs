using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        // Backpack category weights are parented to each sidebar category icon.
        // Offsets are local NGUI units from the icon's upper-right corner.
        // Negative values move the text left/down from that corner.
        public int CategoryWeightFontSize { get; set; } = 9;
        public float CategoryWeightHorizontalOffsetFromUpperRight { get; set; } = -3f;
        public float CategoryWeightVerticalOffsetFromUpperRight { get; set; } = -3f;

        public HudPoint CategoryWeightPercentOffset { get; set; } = new HudPoint { X = -44f, Y = 0f };

        public float BackpackCategoryWeightOpacity { get; set; } = 0.7f;

        // Clamps inventory category-weight label sizes, opacity, and offsets.
        private void ClampInventory()
        {
            CategoryWeightFontSize = Mathf.Clamp(CategoryWeightFontSize, 6, 40);
            CategoryWeightHorizontalOffsetFromUpperRight = Mathf.Clamp(CategoryWeightHorizontalOffsetFromUpperRight, -100f, 0f);
            CategoryWeightVerticalOffsetFromUpperRight = Mathf.Clamp(CategoryWeightVerticalOffsetFromUpperRight, -100f, 0f);
            CategoryWeightPercentOffset = HudPoint.Clamp(CategoryWeightPercentOffset);
            BackpackCategoryWeightOpacity = Mathf.Clamp01(BackpackCategoryWeightOpacity);
        }
    }
}
