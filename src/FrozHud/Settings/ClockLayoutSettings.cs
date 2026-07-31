using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // Compiled layout values for the lower HUD container and analog clock.
    internal sealed class ClockLayoutSettings
    {
        public ClockLayout Values { get; private set; } = new();

        // Initializes and validates the compiled clock layout defaults.
        public ClockLayoutSettings()
        {
            Values.Clamp();
        }
    }

    internal sealed class ClockLayout
    {
        // The lower HUD rows are centered below the cloned TimeWidget horizon.
        public float LowerHudGap { get; set; } = 20f;
        public float LowerOffsetX { get; set; } = 0f;
        public float LowerOffsetY { get; set; } = 0f;
        public float LowerWidth { get; set; } = 140f;
        public float LowerHeight { get; set; } = 56f;
        public float TimeOffsetHours { get; set; } = 0f;

        // Restricts dimensions to values that remain usable in the IMGUI layout.
        public void Clamp()
        {
            LowerHudGap = Mathf.Clamp(LowerHudGap, 0f, 200f);
            LowerWidth = Mathf.Clamp(LowerWidth, 20f, 400f);
            LowerHeight = Mathf.Clamp(LowerHeight, 8f, 200f);
        }
    }
}
