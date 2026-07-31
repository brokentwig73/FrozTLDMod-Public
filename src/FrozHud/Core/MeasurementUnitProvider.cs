using Il2CppTLD.SaveState;

namespace FrozTLDMods.FrozTLDMod
{
    internal static class MeasurementUnitProvider
    {
        // Reads the game's active Imperial/Metric preference without inventing a default.
        internal static bool TryGetCurrent(out MeasurementUnits units)
        {
            var settingsState = SettingsState.Instance;
            if (settingsState == null)
            {
                units = default;
                return false;
            }

            units = settingsState.m_Units;
            return true;
        }
    }
}
