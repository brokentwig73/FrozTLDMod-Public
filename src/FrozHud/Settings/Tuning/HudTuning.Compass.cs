using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        public string DialCompassDisabledTintColor { get; set; } = "#8A8A8A";

        public float DialCompassDisabledOpacity { get; set; } = 65f;

        public float DialCompassArrowSize { get; set; } = 140f;

        public string DialCompassArrowColor { get; set; } = "#FF7A14";

        public float DialCompassArrowRadius { get; set; } = 33f;

        public float CompassBackgroundOffsetDegrees { get; set; } = 0f;
        public float StickNorthOffsetDegrees { get; set; } = -9f;

        // Clamps compass labels, stick artwork, indoor tint, and marker tuning.
        private void ClampCompass()
        {
            DialCompassDisabledOpacity = Mathf.Clamp(DialCompassDisabledOpacity, 0f, 100f);
            DialCompassArrowSize = Mathf.Clamp(DialCompassArrowSize, 10f, 1000f);
            DialCompassArrowRadius = Mathf.Clamp(DialCompassArrowRadius, 0f, 100f);
            CompassBackgroundOffsetDegrees = Mathf.Clamp(CompassBackgroundOffsetDegrees, -45f, 45f);
            StickNorthOffsetDegrees = Mathf.Clamp(StickNorthOffsetDegrees, -45f, 45f);
        }
    }
}
