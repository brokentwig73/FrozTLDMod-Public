using Il2Cpp;
using System.Collections.Generic;

namespace FrozTLDMods.FrozTLDMod
{
    internal static class Il2CppList
    {
        // Normalizes IL2CPP and managed list enumeration into one object sequence.
        internal static IEnumerable<object> Enumerate(object list)
        {
            if (list is Il2CppSystem.Collections.Generic.List<GearItem> gearList)
            {
                for (var i = 0; i < gearList.Count; i++)
                {
                    yield return gearList[i];
                }

                yield break;
            }

            if (list is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    yield return item;
                }
            }
        }

    }
}
