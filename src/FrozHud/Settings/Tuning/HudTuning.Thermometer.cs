using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        public int DialThermometerFeelsLikeFontSize { get; set; } = 16;

        public float DialThermometerFontCircleSize { get; set; } = 40f;

        public string DialThermometerFeelsLikeColor { get; set; } = "#FF7A14";

        public string DialThermometerFeelsLikeFontColor { get; set; } = "#000000";

        public float DialThermometerFontCircleBorder { get; set; } = 6f;

        public float DialThermometerFeelsLikeMarkerSize { get; set; } = 140f;

        public float DialThermometerFeelsLikeMarkerRadius { get; set; } = 35f;

        public string DialThermometerOutdoorColor { get; set; } = "#4FA3FF";

        public float DialThermometerOutdoorMarkerSize { get; set; } = 140f;

        public float DialThermometerOutdoorMarkerRadius { get; set; } = 35f;

        public float DialThermometerFreezingMarkerSize { get; set; } = 140f;

        public float DialThermometerFreezingMarkerRadius { get; set; } = 15f;

        public string DialThermometerFreezingColor { get; set; } = "#FF0000";

        public float DialThermometerRangeRadius { get; set; } = 23.5f;

        public int DialThermometerRangeFontSize { get; set; } = 14;

        public string DialThermometerRangeFontColor { get; set; } = "#000000";

        // Clamps thermometer range text, markers, colors, and center reading geometry.
        private void ClampThermometer()
        {
            DialThermometerFeelsLikeFontSize = Mathf.Clamp(DialThermometerFeelsLikeFontSize, 8, 60);
            DialThermometerFontCircleSize = Mathf.Clamp(DialThermometerFontCircleSize, 4f, 120f);
            DialThermometerFontCircleBorder = Mathf.Clamp(DialThermometerFontCircleBorder, 0f, 12f);
            DialThermometerFeelsLikeMarkerSize = Mathf.Clamp(DialThermometerFeelsLikeMarkerSize, 10f, 1000f);
            DialThermometerFeelsLikeMarkerRadius = Mathf.Clamp(DialThermometerFeelsLikeMarkerRadius, 0f, 100f);
            DialThermometerOutdoorMarkerSize = Mathf.Clamp(DialThermometerOutdoorMarkerSize, 10f, 1000f);
            DialThermometerOutdoorMarkerRadius = Mathf.Clamp(DialThermometerOutdoorMarkerRadius, 0f, 100f);
            DialThermometerFreezingMarkerSize = Mathf.Clamp(DialThermometerFreezingMarkerSize, 10f, 1000f);
            DialThermometerFreezingMarkerRadius = Mathf.Clamp(DialThermometerFreezingMarkerRadius, 0f, 100f);
            DialThermometerRangeRadius = Mathf.Clamp(DialThermometerRangeRadius, 0f, 100f);
            DialThermometerRangeFontSize = Mathf.Clamp(DialThermometerRangeFontSize, 4, 40);
        }
    }
}
