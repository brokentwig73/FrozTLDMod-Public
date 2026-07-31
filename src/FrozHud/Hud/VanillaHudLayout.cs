using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class FrozTLDModOverlay
    {
        // Returns the owned TimeWidget horizon rectangle once its exact NGUI layout is ready.
        public bool TryGetHudRect(out Rect rect)
        {
            rect = default;
            return FrozTLDMod.TimeHud != null &&
                   FrozTLDMod.TimeHud.TryGetHorizonImguiRect(out rect);
        }
    }
}
