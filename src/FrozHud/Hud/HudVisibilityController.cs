using Il2Cpp;
using System.Collections.Generic;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    public sealed partial class FrozTLDMod
    {
        private const float HudVisibilityRefreshIntervalSeconds = 0.1f;

        private static int _hudAllowedFrame = -1;
        private static bool _hudAllowed;
        private static float _hudActiveCheckTime = -10f;
        private static bool _hudActive;
        private static readonly List<GameObject> _regularStatusHudRoots = new List<GameObject>();
        private static bool _regularStatusHudRootsCached;
        private static Panel_Rest _panelRest;
        private static Panel_Actions _panelActions;
        private static float _statsHudAllowedCheckTime = -10f;
        private static bool _statsHudAllowed;
        private static TimeWidget _panelRestTimeWidget;
        private static bool _restOrPassTimeActive;
        private static bool _sleepingActive;
        private static float _postSleepHudHiddenUntil = -10f;
        private static float _restOrPassTimeActiveUntil = -10f;
        private static float _lastPassTimeUpdateRealtime = -10f;
        private static bool _passTimeUpdateRecentlyObserved;
        // Reports whether the mod and at least one custom HUD element are enabled.
        internal static bool IsStickyHudEnabled()
        {
            return Settings != null && Settings.Enabled && Settings.HasEnabledElement();
        }

        // Reports whether global sticky mode currently has an enabled element to control.
        internal static bool HasStickyElement()
        {
            return Settings != null && Settings.Enabled && Settings.HasStickyElement();
        }

        // Returns the combined HUD-allowed state once per frame for all render paths.
        internal static bool IsHudAllowed()
        {
            // Several draw paths ask this question in one frame. Cache it by
            // frame number so we do not repeatedly scan the game's UI hierarchy.
            if (_hudAllowedFrame == Time.frameCount)
            {
                return _hudAllowed;
            }

            _hudAllowedFrame = Time.frameCount;
            _hudAllowed = CalculateHudAllowed();
            return _hudAllowed;
        }

        // Combines normal status HUD, rest, pass-time, sleep, and post-sleep signals at a throttled rate.
        internal static bool HudActive()
        {
            // Cache the combined gameplay/rest visibility signal so sticky HUD
            // checks do not walk Unity UI hierarchies every frame.
            if (Time.unscaledTime - _hudActiveCheckTime < HudVisibilityRefreshIntervalSeconds)
            {
                return _hudActive;
            }

            _hudActiveCheckTime = Time.unscaledTime;
            _hudActive = !IsSleepingActive() &&
                         !IsPostSleepHudResumeDelayActive() &&
                         (IsStatsHudAllowed() || IsRestPanelActive() || IsRestOrPassTimeActive());
            return _hudActive;
        }

        // Reports whether a rest/pass-time hook is active or still inside its short continuity window.
        internal static bool IsRestOrPassTimeActive()
        {
            return _restOrPassTimeActive ||
                   Time.realtimeSinceStartup < _restOrPassTimeActiveUntil;
        }

        // Selects the higher rest-layer clone for bed setup and accelerated pass-time.
        internal static bool ShouldUseRestTimeHudLayer()
        {
            // The bed setup panel and accelerated pass-time render above the
            // normal HUD layer. Use the rest clone for the entire flow so the
            // sundial keeps a consistent brightness and draw order.
            return !IsSleepingActive() &&
                   !IsPostSleepHudResumeDelayActive() &&
                   (IsRestPanelActive() || IsRestOrPassTimeActive());
        }

        // Reports whether the player is inside the actual sleeping operation.
        internal static bool IsSleepingActive()
        {
            return _sleepingActive;
        }

        // Reports whether the brief post-sleep delay is still hiding the HUD during interface recovery.
        internal static bool IsPostSleepHudResumeDelayActive()
        {
            return Time.realtimeSinceStartup < _postSleepHudHiddenUntil;
        }

        // Updates the sleep signal captured from Rest lifecycle hooks.
        internal static void SetSleepingActive(bool active, string source)
        {
            if (_sleepingActive == active)
            {
                return;
            }

            _sleepingActive = active;
            // Log?.Msg("Sleeping active=" + active + " source=" + source + ".");
        }

        // Delays HUD restoration so custom elements return with the native time widget after sleep.
        internal static void DelayHudAfterSleep(string source)
        {
            _postSleepHudHiddenUntil = Time.realtimeSinceStartup + PostSleepHudResumeDelaySeconds;
            _hudActiveCheckTime = -10f;
            // Log?.Msg("Post-sleep HUD resume delayed " + PostSleepHudResumeDelaySeconds.ToString("0.0") + "s source=" + source + ".");
        }

        // Updates the explicit rest/pass-time signal captured from native lifecycle hooks.
        internal static void SetRestOrPassTimeActive(bool active, string source)
        {
            if (_restOrPassTimeActive == active)
            {
                return;
            }

            _restOrPassTimeActive = active;
            // Log?.Msg("Rest/pass-time active=" + active + " source=" + source + ".");
        }

        // Extends pass-time visibility from the native operation's recurring update callback.
        internal static void RecordPassTimeUpdate(PassTime passTime)
        {
            _lastPassTimeUpdateRealtime = Time.realtimeSinceStartup;
            _restOrPassTimeActiveUntil = Time.realtimeSinceStartup + 1f;
            _passTimeUpdateRecentlyObserved = true;
        }

        // Clears the recently-observed pass-time marker after native updates stop arriving.
        private static void UpdatePassTimeActivityTimeout()
        {
            if (!_passTimeUpdateRecentlyObserved)
            {
                return;
            }

            if (Time.realtimeSinceStartup - _lastPassTimeUpdateRealtime < 0.75f)
            {
                return;
            }

            _passTimeUpdateRecentlyObserved = false;
        }

        // Finds and caches the TimeWidget owned by Panel_Rest.
        private static TimeWidget GetPanelRestTimeWidget()
        {
            if (_panelRestTimeWidget != null && _panelRestTimeWidget.gameObject != null)
            {
                return _panelRestTimeWidget;
            }

            _panelRest = PanelCache.Get(_panelRest);
            if (_panelRest == null || _panelRest.gameObject == null)
            {
                return null;
            }

            var widgets = _panelRest.gameObject.GetComponentsInChildren<TimeWidget>(true);
            foreach (var widget in widgets)
            {
                if (widget != null && widget.gameObject != null)
                {
                    _panelRestTimeWidget = widget;
                    return _panelRestTimeWidget;
                }
            }

            return null;
        }

        // Exposes the cached native rest widget to the TimeWidget suppression controller.
        internal static TimeWidget GetPanelRestTimeWidgetForSuppression()
        {
            return GetPanelRestTimeWidget();
        }

        // Reports generic custom HUD visibility from the shared element policy.
        internal static bool ShouldRenderHud(bool stickyDesired)
        {
            return ShouldRenderElement(true, stickyDesired);
        }

        // Returns full sticky alpha or the native Panel_Actions fade alpha for non-sticky mode.
        internal static float GetHudAlpha(bool stickyDesired)
        {
            // One alpha source controls the cloned TimeWidget and all IMGUI
            // overlays. Sticky mode is full alpha while allowed; non-sticky mode
            // follows the vanilla Panel_Actions fade.
            if (!IsStickyHudEnabled())
            {
                return 0f;
            }

            if (IsVanillaTimeForcedVisible())
            {
                return 1f;
            }

            if (Settings.StickyHud)
            {
                return stickyDesired && HudActive() ? 1f : 0f;
            }

            return GetVanillaTimeFadeAlpha();
        }

        // Applies shared visibility policy to the cloned time-of-day dial.
        internal static bool ShouldRenderTimeOfDay(bool stickyDesired)
        {
            return Settings != null &&
                   ShouldRenderElement(Settings.StickyHud, stickyDesired);
        }

        // Applies shared visibility policy to the clock.
        internal static bool ShouldRenderClock(bool stickyDesired)
        {
            return Settings != null &&
                   ShouldRenderElement(Settings.Clock, stickyDesired);
        }

        // Applies shared visibility policy to the scent meter.
        internal static bool ShouldRenderScentMeter(bool stickyDesired)
        {
            return Settings != null &&
                   ShouldRenderElement(Settings.ScentMeter, stickyDesired);
        }

        // Reports whether the transparent lower HUD container is needed.
        internal static bool ShouldRenderLowerHudContainer(bool stickyDesired)
        {
            return ShouldRenderElement(true, stickyDesired);
        }

        // Applies shared visibility policy to the thermometer.
        internal static bool ShouldRenderTemperature(bool stickyDesired)
        {
            return Settings != null &&
                   ShouldRenderElement(Settings.ShowTemperature, stickyDesired);
        }

        // Keeps the outdoor marker tied to the same setting and visibility as the thermometer.
        internal static bool ShouldRenderFeelsOutside(bool stickyDesired)
        {
            return Settings != null &&
                   ShouldRenderElement(Settings.ShowTemperature, stickyDesired);
        }

        // Applies shared visibility policy to the stick compass.
        internal static bool ShouldRenderStickCompass(bool stickyDesired)
        {
            return Settings != null &&
                   ShouldRenderElement(Settings.ShowStickCompass, stickyDesired);
        }

        // Applies shared visibility policy to the wind compass.
        internal static bool ShouldRenderWindCompass(bool stickyDesired)
        {
            return Settings != null &&
                   ShouldRenderElement(Settings.ShowWindCompass, stickyDesired);
        }

        // Applies shared visibility policy to the backpack gauge.
        internal static bool ShouldRenderBackpackWeight(bool stickyDesired)
        {
            return Settings != null &&
                   ShouldRenderElement(Settings.ShowBackpackWeight, stickyDesired);
        }

        // Centralizes enabled, sticky, active, and fade rules for every custom HUD element.
        private static bool ShouldRenderElement(bool enabled, bool stickyDesired)
        {
            if (!IsStickyHudEnabled())
            {
                return false;
            }

            if (!enabled)
            {
                return false;
            }

            if (Settings.StickyHud)
            {
                return stickyDesired && HudActive();
            }

            return GetHudAlpha(stickyDesired) > 0.01f;
        }

        // Reports the vanilla bed-panel exception that intentionally forces time-of-day visibility.
        internal static bool IsVanillaTimeForcedVisible()
        {
            // Bed/rest is a real vanilla exception: the regular stats HUD can be
            // hidden, but the game intentionally keeps the time widget visible.
            if (!IsStickyHudEnabled())
            {
                return false;
            }

            return IsRestPanelActive();
        }

        // Calculates the uncached combined permission for custom HUD rendering.
        private static bool CalculateHudAllowed()
        {
            return IsStickyHudEnabled() &&
                   HudActive();
        }

        // Returns the cached normal-gameplay visibility derived from native regular status bars.
        private static bool IsStatsHudAllowed()
        {
            // The reliable "normal gameplay HUD is visible" signal turned out to
            // be the regular StatusBar hierarchy roots, not Panel_HUD or
            // Essential/NonEssential HUD containers. Those stayed active in too
            // many menus and caused our overlays to linger.
            if (!IsStickyHudEnabled())
            {
                return false;
            }

            if (Time.unscaledTime - _statsHudAllowedCheckTime < HudVisibilityRefreshIntervalSeconds)
            {
                return _statsHudAllowed;
            }

            _statsHudAllowedCheckTime = Time.unscaledTime;
            try
            {
                _statsHudAllowed = IsRegularStatsHudVisible();
            }
            catch
            {
                // A failed native visibility read must not leave the custom HUD
                // visible in a menu or other state that hides normal gameplay HUDs.
                _statsHudAllowed = false;
            }

            return _statsHudAllowed;
        }

        // Reports whether the non-sticky native time panel still has visible fade alpha.
        private static bool IsVanillaTimeFadeVisible()
        {
            return GetVanillaTimeFadeAlpha() > 0.01f;
        }

        // Reads the native Panel_Actions fade alpha without keeping that panel visible.
        private static float GetVanillaTimeFadeAlpha()
        {
            try
            {
                _panelActions = PanelCache.Get(_panelActions);
                if (_panelActions != null && _panelActions.gameObject != null)
                {
                    return _panelActions.gameObject.activeInHierarchy ? Mathf.Clamp01(_panelActions.GetPanelAlpha()) : 0f;
                }
            }
            catch
            {
            }

            return 0f;
        }

        // Reports whether the native bed/rest setup panel is active.
        private static bool IsRestPanelActive()
        {
            _panelRest = PanelCache.Get(_panelRest);
            return IsPanelActive(_panelRest);
        }

        // Safely tests whether a native panel is active in hierarchy and enabled.
        private static bool IsPanelActive(Panel_Base panel)
        {
            return panel != null &&
                   panel.gameObject != null &&
                   panel.gameObject.activeInHierarchy &&
                   panel.IsEnabled();
        }

        // Reports whether any cached regular status-bar hierarchy root is currently visible.
        private static bool IsRegularStatsHudVisible()
        {
            CacheRegularStatusHudRootsIfNeeded();
            foreach (var root in _regularStatusHudRoots)
            {
                if (root != null && root.activeInHierarchy)
                {
                    return true;
                }
            }

            return false;
        }

        // Resolves the regular HUD status roots once, avoiding repeated global StatusBar searches.
        private static void CacheRegularStatusHudRootsIfNeeded()
        {
            if (_regularStatusHudRootsCached)
            {
                return;
            }

            var statusBarsRoot = GameObject.Find("StatusBars_Regular");
            if (statusBarsRoot == null)
            {
                return;
            }

            var bars = statusBarsRoot.GetComponentsInChildren<StatusBar>(true);
            foreach (var bar in bars)
            {
                if (bar == null ||
                    !bar.m_IsOnHUD ||
                    bar.m_HierarchyRoot == null)
                {
                    continue;
                }

                _regularStatusHudRoots.Add(bar.m_HierarchyRoot);
            }

            _regularStatusHudRootsCached = _regularStatusHudRoots.Count > 0;
        }

        // Tests whether a game object belongs to the regular StatusBars hierarchy.
        private static bool IsUnderStatusBarsRegular(GameObject gameObject)
        {
            var parent = gameObject != null && gameObject.transform != null
                ? gameObject.transform.parent
                : null;

            while (parent != null)
            {
                if (parent.gameObject != null && parent.gameObject.name == "StatusBars_Regular")
                {
                    return true;
                }

                parent = parent.parent;
            }

            return false;
        }

    }
}
