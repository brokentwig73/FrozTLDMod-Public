using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // Compiled visual tuning for HUD text and dial art. These values started
    // life in FrozTLDModTuning.json while the HUD was being tuned live; the JSON
    // file is no longer read at runtime. Keep each knob named so it can be
    // tweaked in code or wired back to a settings file later.
    internal sealed class HudTuningSettings
    {
        public HudTuning Values { get; private set; } = new();

        // Initializes and validates every compiled HUD tuning group.
        public HudTuningSettings()
        {
            Values.Clamp();
        }

    }

    internal sealed partial class HudTuning
    {
        // Most values began as percent-style tuning knobs. Clamp keeps accidental
        // code edits from exploding the IMGUI layout.
        public void Clamp()
        {
            ClampGeneralLayout();
            ClampDialShared();
            ClampCompass();
            ClampClockDial();
            ClampThermometer();
            ClampWind();
            ClampScent();
            ClampBackpack();
            ClampInventory();
            ClampWeapons();
        }
    }

    internal sealed class HudPoint
    {
        public float X { get; set; } = 0f;
        public float Y { get; set; } = 0f;

        // Produces a non-null HUD offset whose coordinates remain within the supported tuning range.
        public static HudPoint Clamp(HudPoint point)
        {
            point ??= new HudPoint();
            point.X = Mathf.Clamp(point.X, -200f, 200f);
            point.Y = Mathf.Clamp(point.Y, -200f, 200f);
            return point;
        }
    }

}
