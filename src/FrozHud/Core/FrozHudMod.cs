using MelonLoader;
using System.Reflection;
using HarmonyLib;

namespace FrozTLDMods.FrozTLDMod
{
    // MelonLoader entry point. This class deliberately owns the global render
    // gates so the NGUI TimeWidget clone and the IMGUI overlays make the same
    // visibility decision every frame.
    public sealed partial class FrozTLDMod : MelonMod
    {
        internal static HudSettings Settings { get; private set; }
        internal static MelonLogger.Instance Log { get; private set; }
        internal static FrozTLDModOverlay Overlay { get; private set; }
        internal static FrozTimeHudController TimeHud { get; private set; }
        internal static ClockLayoutSettings ClockLayout { get; private set; }
        internal static HudTuningSettings HudTuning { get; private set; }
        private const float PostSleepHudResumeDelaySeconds = 2f;
        private static BackpackCategoryWeightDisplay _backpackCategoryWeightDisplay;
        private static WeaponReticleController _weaponReticleController;
        private static RememberLastWeaponController _rememberLastWeaponController;
        private static FireStartHeldStarterSelector _fireStartHeldStarterSelector;
        private static LightSourceLifeWarningController _lightSourceLifeWarningController;

        // Creates settings and controllers, then applies all Harmony patches at mod startup.
        public override void OnInitializeMelon()
        {
            Settings = new HudSettings();
            Settings.AddToModSettings("FROZ TLD MOD");
            Settings.RefreshFields();
            Log = LoggerInstance;
            ClockLayout = new ClockLayoutSettings();
            HudTuning = new HudTuningSettings();
            Overlay = new FrozTLDModOverlay();
            TimeHud = new FrozTimeHudController();

            _fireStartHeldStarterSelector = new FireStartHeldStarterSelector();
            _lightSourceLifeWarningController = new LightSourceLifeWarningController();
            _weaponReticleController = new WeaponReticleController();
            _rememberLastWeaponController = new RememberLastWeaponController();
            _backpackCategoryWeightDisplay = new BackpackCategoryWeightDisplay();

            HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());

            LoggerInstance.Msg("FrozTLDMod initialized.");
        }

        // Advances each active controller and throttled data cache once per game frame.
        public override void OnUpdate()
        {
            TimeHud?.Update();
            Overlay?.SetStickyDesired(TimeHud?.StickyDesired == true);
            Overlay?.Update();
            _fireStartHeldStarterSelector?.Update();
            DragFuelIntoFireController.Update();
            _lightSourceLifeWarningController?.Update();
            _weaponReticleController?.Update();
            _rememberLastWeaponController?.Update();
            UpdatePassTimeActivityTimeout();
            _backpackCategoryWeightDisplay?.Update();
        }

        // Reapplies renderer state after vanilla's late-frame loose-fuel preview update.
        public override void OnLateUpdate()
        {
            DragFuelIntoFireController.LateUpdate();
        }

        // Draws the Froz-owned IMGUI HUD elements.
        public override void OnGUI()
        {
            Overlay?.Draw();
        }

    }
}
