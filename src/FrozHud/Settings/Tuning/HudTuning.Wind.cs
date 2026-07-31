using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        public float DialWindArrowSize { get; set; } = 140f;

        public string DialWindArrowColor { get; set; } = "#FF7A14";

        public float DialWindArrowRadius { get; set; } = 33f;

        public float DialWindFontCircleRadius { get; set; } = 20f;

        public float DialWindSpeedCircleBorder { get; set; } = 6f;

        // Clamps wind marker, speed readout, and center-circle tuning.
        private void ClampWind()
        {
            DialWindArrowSize = Mathf.Clamp(DialWindArrowSize, 10f, 1000f);
            DialWindArrowRadius = Mathf.Clamp(DialWindArrowRadius, 0f, 100f);
            DialWindFontCircleRadius = Mathf.Clamp(DialWindFontCircleRadius, 4f, 80f);
            DialWindSpeedCircleBorder = Mathf.Clamp(DialWindSpeedCircleBorder, 1f, 20f);
        }
    }
}
