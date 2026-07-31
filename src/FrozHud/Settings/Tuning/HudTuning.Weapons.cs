namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class HudTuning
    {
        public HudPoint WeaponReticlePistolOffset { get; set; } = new HudPoint { X = 86f, Y = -42f };

        // Clamps weapon reticle offsets and visual tuning.
        private void ClampWeapons()
        {
            WeaponReticlePistolOffset = HudPoint.Clamp(WeaponReticlePistolOffset);
        }
    }
}
