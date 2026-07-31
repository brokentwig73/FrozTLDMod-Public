using ModSettings;
using System.IO;
using System.Reflection;

namespace FrozTLDMods.FrozTLDMod
{
    // User-facing ModSettings page. Runtime visual tuning is compiled into
    // named settings classes; this JSON persists the user's gameplay controls.
    internal sealed class HudSettings : JsonModSettings
    {
        // Stores ModSettings values under the mod's deployed UserData subfolder.
        public HudSettings()
            : base(Path.Combine("FrozTLDMod", "FrozTLDMod.json"))
        {
        }

        [Name("Mod Enabled")]
        [Description("Enable Froz TLD Mod.")]
        public bool Enabled = true;

        [Section("Hud Settings")]

        [Name("Sticky HUD (Includes ToD)")]
        [Description("Make enabled HUD elements sticky. Tab toggle on and off with no fade out.")]
        public bool StickyHud = true;

        [Name("Show Stick Compass")]
        [Description("Show the stick compass element in HUD area.")]
        public bool ShowStickCompass = true;

        [Name("Show Wind Compass")]
        [Description("Show the wind compass element in HUD area.")]
        public bool ShowWindCompass = true;

        [Name("Show Temperatures")]
        [Description("Show current feels-like temperature and outdoor temperature in HUD area.")]
        public bool ShowTemperature = true;

        [Name("Show Clock")]
        [Description("Show clock in HUD area.")]
        public bool Clock = true;

        [Name("Show Scent Meter")]
        [Description("Show scent meter in HUD area.")]
        public bool ScentMeter = true;

        [Name("Show Backpack Weight/Max")]
        [Description("Show current backpack weight against max carry weight in HUD area.")]
        public bool ShowBackpackWeight = true;

        [Name("Show Category Weights")]
        [Description("Show each backpack category's total weight beside the category buttons.")]
        public bool ShowCategoryWeights = true;

        [Section("Fire Starting")]

        [Name("Default Lit Torch/Flare")]
        [Description("When starting a fire, pre-select a held lit torch or flare.")]
        public bool PreferHeldFirestarter = true;

        [Name("Use Tinder After Level 3")]
        [Description("After Level 3 Fire Starting, default tinder if available.")]
        public bool DefaultTinderAfterFireStartingLevel3 = true;

        [Name("Exclude Birch Bark")]
        [Description("Prevent game from defaulting Birch Bark on all Fire Starting Levels")]
        public bool ExcludeBirchBarkTinder = false;

        [Name("Default Stick for Fire Starting")]
        [Description("When sticks are available, pre-select a stick as the fuel used to start a fire.")]
        public bool DefaultStickForFireStarting = true;

        [Name("Drag Fuel Into Fire")]
        [Description("Right-click loose wood, sticks, coal, or other valid fuel and place it onto a burning fire to add it directly.")]
        public bool DragFuelIntoFire = true;

        [Section("Remember Last Tool")]

        [Name("Remember Crafting")]
        [Description("Remember the last tool used for each crafted item.")]
        public bool RememberCraftingTool = true;

        [Name("Remember Breakdown")]
        [Description("Remember the last tool used when breaking down objects, including wood.")]
        public bool RememberBreakdownTool = true;

        [Name("Remember Harvesting")]
        [Description("Remember the last tool used when harvesting or quartering carcasses.")]
        public bool RememberAnimalHarvestingTool = true;

        [Name("Remember Ice Fishing")]
        [Description("Remember the last tool used when making or clearing ice fishing holes.")]
        public bool RememberIceFishingTool = true;

        [Section("Remember Last Weapon")]

        [Name("Remember Weapon")]
        [Description("Remember the last equipped weapon specifically.  2 will bring the exact weapon up, radial will remember specific weapon per type.")]
        public bool RememberWeapon = true;

        [Section("Weapons")]

        [Name("Pistol Hip-Fire Reticle")]
        [Description("Show hip-fire reticle when a revolver is equipped.")]
        public bool ReticleOnPistols = true;

        [Name("Rifle Reticle")]
        [Description("Show reticle when a rifle is equipped.")]
        public bool ReticleOnRifles = false;

        [Name("Flare Gun Reticle")]
        [Description("Show reticle when a flare-gun is equipped.")]
        public bool ReticleOnFlareGun = false;

        [Section("Sound Settings")]

        [Name("Aurora Ambience Volume")]
        [Description("Adjust the continuous Aurora sky ambience. Does not affect nearby powered electrical objects.")]
        [Slider(0f, 100f, 101, NumberFormat = "{0,3:D}%")]
        public int AuroraAmbienceVolumePercent = 100;

        [Name("Aurora Electrical Volume")]
        [Description("Adjust Aurora-powered lights and machinery. Does not affect the sky ambience.")]
        [Slider(0f, 100f, 101, NumberFormat = "{0,3:D}%")]
        public int AuroraElectricalVolumePercent = 100;

        [Section("Other Settings")]

        [Name("Skip Startup Disclaimers")]
        [Description("Enable Hinterland's built in -skipintro command line argument to disable the start up disclaimers")]
        public bool SkipStartupDisclaimers = true;

        [Name("Red Light Life Bar")]
        [Description("Tint the torch/flare life bar red as it gets close to burning out.")]
        public bool RedTorchFlareLifeBar = true;

        [Name("Fix Placement Spacing")]
        [Description("Fix huge collision boundaries between items when placing, especially on dropped items.")]
        public bool FixPlacementSpacing = true;

        // Reports whether any custom HUD element requires the overlay controller to run.
        public bool HasEnabledElement()
        {
            // StickyHud counts as an enabled element because it always includes
            // the cloned Time-of-Day dial.
            return StickyHud || Clock || ScentMeter || ShowTemperature || ShowStickCompass || ShowWindCompass || ShowBackpackWeight;
        }

        // Reports whether sticky mode currently has at least one enabled element to keep visible.
        public bool HasStickyElement()
        {
            return StickyHud && HasEnabledElement();
        }

        // Applies settings that need an immediate runtime refresh, then updates dependent fields.
        protected override void OnChange(FieldInfo field, object oldValue, object newValue)
        {
            if (field.Name == nameof(Enabled) ||
                field.Name == nameof(AuroraAmbienceVolumePercent) ||
                field.Name == nameof(AuroraElectricalVolumePercent))
            {
                AuroraSoundController.ApplyCurrentSetting();
            }

            RefreshFields();
        }

        // Refreshes visibility rules for settings whose availability depends on other options.
        internal void RefreshFields()
        {
            SetFieldVisible(nameof(ExcludeBirchBarkTinder), true);
        }
    }

}
