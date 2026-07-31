using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        public int DialClockNumberFontSize { get; set; } = 16;

        public string DialClockNumberColor { get; set; } = "#000000";

        public float DialClockNumberOpacity { get; set; } = 60f;

        public float DialClockDotSize { get; set; } = 3f;

        public string DialClockDotColor { get; set; } = "#000000";

        public float DialClockMarkerRadius { get; set; } = 26f;

        public string DialClockHourHandColor { get; set; } = "#000000";

        public float DialClockHourHandLengthScale { get; set; } = 58f;

        public float DialClockHourHandWidthScale { get; set; } = 70f;

        public string DialClockMinuteHandColor { get; set; } = "#000000";

        public float DialClockMinuteHandLengthScale { get; set; } = 80f;

        public float DialClockMinuteHandWidthScale { get; set; } = 70f;

        // Clamps clock face, numeral, dot, and hand tuning to usable dial dimensions.
        private void ClampClockDial()
        {
            DialClockNumberFontSize = Mathf.Clamp(DialClockNumberFontSize, 4, 60);
            DialClockNumberOpacity = Mathf.Clamp(DialClockNumberOpacity, 0f, 100f);
            DialClockDotSize = Mathf.Clamp(DialClockDotSize, 1f, 40f);
            DialClockMarkerRadius = Mathf.Clamp(DialClockMarkerRadius, 0f, 100f);
            DialClockHourHandLengthScale = Mathf.Clamp(DialClockHourHandLengthScale, 1f, 200f);
            DialClockHourHandWidthScale = Mathf.Clamp(DialClockHourHandWidthScale, 1f, 400f);
            DialClockMinuteHandLengthScale = Mathf.Clamp(DialClockMinuteHandLengthScale, 1f, 200f);
            DialClockMinuteHandWidthScale = Mathf.Clamp(DialClockMinuteHandWidthScale, 1f, 400f);
        }
    }
}
