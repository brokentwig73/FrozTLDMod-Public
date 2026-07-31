using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        public float BackpackWeightIconScalePercent { get; set; } = 180f;
        public float BackpackWeightIconOpacity { get; set; } = 0.6f;
        public string BackpackWeightIconColorHex { get; set; } = "#FFFFFF";
        public int BackpackWeightFontSize { get; set; } = 18;
        public float BackpackWeightFontOpacity { get; set; } = 1f;
        public string BackpackWeightFontColorHex { get; set; } = "#000000";
        public float BackpackWeightHorizontalOffset { get; set; } = -5f;
        public float BackpackWeightVerticalOffset { get; set; } = -30f;

        public float BackpackMaxVerticalOffset { get; set; } = 30f;

        public string BackpackMaxFontColor { get; set; } = "#000000";

        public string BackpackOverweightFontColor { get; set; } = "#D64A4A";

        // Clamps backpack artwork, text, opacity, and spacing values to renderer-safe ranges.
        private void ClampBackpack()
        {
            BackpackWeightIconScalePercent = Mathf.Clamp(BackpackWeightIconScalePercent, 10f, 400f);
            BackpackWeightIconOpacity = Mathf.Clamp01(BackpackWeightIconOpacity);
            BackpackWeightFontSize = Mathf.Clamp(BackpackWeightFontSize, 8, 60);
            BackpackWeightFontOpacity = Mathf.Clamp01(BackpackWeightFontOpacity);
            BackpackWeightHorizontalOffset = Mathf.Clamp(BackpackWeightHorizontalOffset, -200f, 200f);
            BackpackWeightVerticalOffset = Mathf.Clamp(BackpackWeightVerticalOffset, -200f, 200f);
            BackpackMaxVerticalOffset = Mathf.Clamp(BackpackMaxVerticalOffset, -200f, 200f);
        }
    }
}
