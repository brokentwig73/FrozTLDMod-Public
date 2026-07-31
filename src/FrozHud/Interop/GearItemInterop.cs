using Il2Cpp;
using Il2CppTLD.SaveState;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal static class GearItemInterop
    {
        // TLD's displayed LBS values matched this factor with final truncation
        // to two decimals across All, Fire Starting, First Aid, Food, Tool, and
        // Material in the captured inventory sample.
        private const float PoundsPerKilogram = 2.2046f;

        // Enumerates an indexed IL2CPP collection through its reflected Count/Length and indexer.
        internal static IEnumerable<object> EnumerateIndexedList(object list)
        {
            var countObject = Il2CppReflection.GetObjectMember(list, "Count") ??
                              Il2CppReflection.GetObjectMember(list, "Length");
            if (countObject != null && int.TryParse(countObject.ToString(), out var count))
            {
                for (var index = 0; index < count; index++)
                {
                    yield return GetIndexedItem(list, index);
                }
            }
        }

        // Reads one item through the collection's indexer, get_Item method, or managed Array implementation.
        internal static object GetIndexedItem(object list, int index)
        {
            if (list == null)
            {
                return null;
            }

            try
            {
                var property = list.GetType().GetProperty("Item");
                if (property != null)
                {
                    return property.GetValue(list, new object[] { index });
                }
            }
            catch
            {
            }

            try
            {
                var method = list.GetType().GetMethod("get_Item", new[] { typeof(int) });
                if (method != null)
                {
                    return method.Invoke(list, new object[] { index });
                }
            }
            catch
            {
            }

            try
            {
                if (list is Array array)
                {
                    return array.GetValue(index);
                }
            }
            catch
            {
            }

            return null;
        }

        // Unwraps either a GearItem or an inventory wrapper that stores one in m_GearItem.
        internal static GearItem GetGearItem(object item)
        {
            if (item is GearItem gear)
            {
                return gear;
            }

            return Il2CppReflection.GetObjectMember(item, "m_GearItem") as GearItem;
        }

        // Returns the item's weight in the player's selected display units.
        internal static float GetGearWeight(GearItem gear, MeasurementUnits measurementUnits)
        {
            if (gear == null)
            {
                return 0f;
            }

            return TryGetItemWeight(gear.GetItemWeightKG(false), measurementUnits, out var weight) ? weight : 0f;
        }

        // Converts TLD's fixed-point kilograms only when Imperial display units are active.
        internal static bool TryGetItemWeight(object itemWeight, MeasurementUnits measurementUnits, out float weight)
        {
            var units = Il2CppReflection.GetObjectMember(itemWeight, "m_Units");
            if (TryGetInt64(units, out var rawUnits))
            {
                var kilograms = rawUnits / 1000000000f;
                weight = measurementUnits == MeasurementUnits.Imperial
                    ? kilograms * PoundsPerKilogram
                    : kilograms;
                return true;
            }

            weight = 0f;
            return false;
        }

        // Matches the game's category totals by truncating rather than rounding the final value.
        internal static float TruncateToTwoDecimals(float value)
        {
            return Mathf.Floor(value * 100f) / 100f;
        }

        // Converts common reflected integer representations into a signed 64-bit value.
        internal static bool TryGetInt64(object value, out long number)
        {
            if (value is long longValue)
            {
                number = longValue;
                return true;
            }

            if (value is int intValue)
            {
                number = intValue;
                return true;
            }

            if (value != null && long.TryParse(value.ToString(), out number))
            {
                return true;
            }

            number = 0;
            return false;
        }
    }
}
