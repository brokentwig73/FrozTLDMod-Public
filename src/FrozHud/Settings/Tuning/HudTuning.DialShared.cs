using System;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        public float DialSizeScale { get; set; } = 120f;

        public float DialOpacity { get; set; } = 80f;

        public int DialOverlayFontSize { get; set; } = 14;

        public float DialOverlayFontRadius { get; set; } = 22f;

        public string DialOverlayFontWeight { get; set; } = "Bold";

        public string DialOverlayFontColor { get; set; } = "#000000";

        public float DialOverlayFontOpacity { get; set; } = 80f;

        // Clamps the dimensions and shared visual values used by every circular dial.
        private void ClampDialShared()
        {
            DialSizeScale = Mathf.Clamp(DialSizeScale, 10f, 400f);
            DialOpacity = Mathf.Clamp(DialOpacity, 0f, 100f);
            DialOverlayFontSize = Mathf.Clamp(DialOverlayFontSize, 4, 80);
            DialOverlayFontRadius = Mathf.Clamp(DialOverlayFontRadius, 0f, 100f);
            DialOverlayFontWeight = string.Equals(DialOverlayFontWeight, "Normal", StringComparison.OrdinalIgnoreCase) ? "Normal" : "Bold";
            DialOverlayFontOpacity = Mathf.Clamp(DialOverlayFontOpacity, 0f, 100f);
        }
    }
}
