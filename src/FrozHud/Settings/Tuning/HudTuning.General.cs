using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        public int HudInnerBoxSize { get; set; } = 140;

        public HudPoint HudParentRow1Offset { get; set; } = new HudPoint { X = 0f, Y = 30f };

        public HudPoint HudParentRow2Offset { get; set; } = new HudPoint();

        public HudPoint HudInnerBoxStickCompassOffset { get; set; } = new HudPoint();

        public HudPoint HudInnerBoxWindDialOffset { get; set; } = new HudPoint();

        public HudPoint HudInnerBoxDialThermometerOffset { get; set; } = new HudPoint();

        public HudPoint HudInnerBoxDialClockOffset { get; set; } = new HudPoint();

        public HudPoint HudInnerBoxScentMeterOffset { get; set; } = new HudPoint();

        public HudPoint HudInnerBoxBackpackOffset { get; set; } = new HudPoint();

        // Clamps the shared HUD rows, inner boxes, text, and element offsets.
        private void ClampGeneralLayout()
        {
            HudInnerBoxSize = Mathf.Clamp(HudInnerBoxSize, 20, 140);
            HudParentRow1Offset = HudPoint.Clamp(HudParentRow1Offset);
            HudParentRow2Offset = HudPoint.Clamp(HudParentRow2Offset);
            HudInnerBoxStickCompassOffset = HudPoint.Clamp(HudInnerBoxStickCompassOffset);
            HudInnerBoxWindDialOffset = HudPoint.Clamp(HudInnerBoxWindDialOffset);
            HudInnerBoxDialThermometerOffset = HudPoint.Clamp(HudInnerBoxDialThermometerOffset);
            HudInnerBoxDialClockOffset = HudPoint.Clamp(HudInnerBoxDialClockOffset);
            HudInnerBoxScentMeterOffset = HudPoint.Clamp(HudInnerBoxScentMeterOffset);
            HudInnerBoxBackpackOffset = HudPoint.Clamp(HudInnerBoxBackpackOffset);
        }
    }
}
