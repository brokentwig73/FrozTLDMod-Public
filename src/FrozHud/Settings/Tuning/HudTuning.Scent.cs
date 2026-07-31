using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        public float ScentMeterScale { get; set; } = 100f;

        public HudPoint ScentMeterPosition { get; set; } = new HudPoint { X = 0f, Y = -8f };

        // Clamps the native scent layout's overall scale and position.
        private void ClampScent()
        {
            ScentMeterScale = Mathf.Clamp(ScentMeterScale, 10f, 400f);
            ScentMeterPosition = HudPoint.Clamp(ScentMeterPosition);
        }
    }
}
