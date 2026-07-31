using Il2Cpp;

namespace FrozTLDMods.FrozTLDMod
{
    internal static class ToolMemoryHelpers
    {
        // Converts reflected text to a single log- and key-safe line.
        internal static string CleanText(object value)
        {
            return value != null ? value.ToString().Replace("\r", " ").Replace("\n", " ") : string.Empty;
        }

        // Returns a stable human-readable tool key, including an explicit no-tool value.
        internal static string DescribeGear(GearItem gear)
        {
            if (gear == null || gear.gameObject == null)
            {
                return "none";
            }

            return CleanText(gear.gameObject.name);
        }

        // Returns the persistent game instance ID used to prefer the exact same physical tool.
        internal static string GetGearToolId(GearItem gear)
        {
            if (gear == null)
            {
                return string.Empty;
            }

            return gear.m_InstanceID.ToString();
        }
    }
}
