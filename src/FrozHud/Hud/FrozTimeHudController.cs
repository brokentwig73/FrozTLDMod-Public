using Il2Cpp;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // Owns the upper-right time-of-day dial. The game still creates the original
    // TimeWidget, but we clone it under a stable parent and hide the vanilla copy
    // so sticky behavior does not depend on Panel_Actions staying alive.
    internal sealed partial class FrozTimeHudController
    {
        private const string CloneName = "FrozTLDMod_OwnedTimeWidget";
        private const string RestCloneName = "FrozTLDMod_RestTimeWidget";
        private const string VisiblePreferenceKey = "FrozTLDMod.StickyVisible";

        private GameObject _clone;
        private GameObject _restClone;
        private TimeWidget _cloneWidget;
        private TimeWidget _restCloneWidget;
        private TimeWidget _sourceWidget;
        private static Panel_Actions _panelActions;
        private static Panel_HUD _panelHud;
        private static TimeWidget _panelHudTimePopupWidget;
        private UIWidget[] _cloneWidgets;
        private UIWidget[] _restCloneWidgets;
        private UIWidget _horizonWidget;
        private Camera _cachedCamera;
        private Rect _cachedHorizonImguiRect;
        private float _lastAppliedAlpha = -1f;
        private float _lastAppliedRestAlpha = -1f;
        private float _lastToggleTime = -10f;
        private float _nextHorizonRectRefreshTime = -1f;
        private float _nextMaintenanceTime = -1f;
        private float _nextRestCloneSearchTime = -1f;
        private bool _visible;
        private bool _waitingForTemplate;
        private bool _autoTabApplied;
        private bool _autoTemplateRequested;
        private bool _hasCachedHorizonImguiRect;

        public bool Visible => FrozTLDMod.ShouldRenderTimeOfDay(_visible);
        public bool HasClone => _clone != null;
        public bool StickyDesired => _visible;

        // Initializes a session-hidden sticky state; auto-Tab enables it once gameplay HUD is ready.
        public FrozTimeHudController()
        {
            // Start each play session hidden from our perspective. If Sticky HUD
            // is enabled, ApplyAutoTabIfNeeded turns it on once the in-game HUD
            // is allowed. This avoids persisting an accidental off/on state across
            // launches while still letting Tab turn it off during the session.
            _visible = false;
            _waitingForTemplate = _visible;
        }

        // Returns a cached IMGUI rectangle for the cloned horizon, refreshing it four times per second.
        public bool TryGetHorizonImguiRect(out Rect rect)
        {
            rect = default;

            if (_clone == null)
            {
                return false;
            }

            if (_hasCachedHorizonImguiRect &&
                Time.realtimeSinceStartup < _nextHorizonRectRefreshTime)
            {
                rect = _cachedHorizonImguiRect;
                return rect.width > 0f && rect.height > 0f;
            }

            var horizon = GetHorizonWidget();
            if (horizon == null)
            {
                _hasCachedHorizonImguiRect = false;
                return false;
            }

            var camera = GetCloneCamera();
            _nextHorizonRectRefreshTime = Time.realtimeSinceStartup + 0.25f;

            if (camera != null && TryGetImguiRect(horizon, camera, out rect))
            {
                _cachedHorizonImguiRect = rect;
                _hasCachedHorizonImguiRect = true;
                return true;
            }

            _hasCachedHorizonImguiRect = false;
            return false;
        }

        // Creates and maintains the normal/rest clones according to current HUD and sticky visibility.
        public void Update()
        {
            if (!FrozTLDMod.IsStickyHudEnabled())
            {
                SetVisible(false, persist: false);
                return;
            }

            ApplyAutoTabIfNeeded();

            var shouldRender = Visible;

            if (_clone == null && (shouldRender || _waitingForTemplate))
            {
                TryCreateClone();
            }

            UpdateRestClone();

            var useRestLayer = FrozTLDMod.ShouldUseRestTimeHudLayer();
            if (!shouldRender)
            {
                if (_clone != null && _clone.activeSelf)
                {
                    _clone.SetActive(false);
                }

                if (_restClone != null && _restClone.activeSelf)
                {
                    _restClone.SetActive(false);
                }

                return;
            }

            if (useRestLayer)
            {
                if (_clone != null && _clone.activeSelf)
                {
                    _clone.SetActive(false);
                }

                UpdateRestClone();
                return;
            }

            if (_restClone != null && _restClone.activeSelf)
            {
                _restClone.SetActive(false);
            }

            if (_clone == null)
            {
                return;
            }

            if (_clone.activeSelf != shouldRender)
            {
                _clone.SetActive(shouldRender);
            }

            RunVisibleMaintenanceIfNeeded(force: false);
        }

        // Handles Tab toggles, including duplicate-input debounce and first-session template bootstrap.
        public bool ToggleFromHotkey()
        {
            if (_clone != null && Time.realtimeSinceStartup - _lastToggleTime < 0.2f)
            {
                // The game can send duplicate survival-panel actions from one
                // physical Tab press. Debounce only after the clone exists; before
                // that, duplicate vanilla calls may be needed to create the template.
                return true;
            }

            if (!FrozTLDMod.IsStickyHudEnabled())
            {
                SetVisible(false, persist: false);
                return true;
            }

            if (FrozTLDMod.Settings.StickyHud &&
                _clone == null &&
                !TryCreateClone())
            {
                // First manual Tab in a fresh session may need to pass through to
                // vanilla. Once vanilla creates Panel_Actions/TimeWidget, Update()
                // will clone it and future Tabs are fully owned by this controller.
                _waitingForTemplate = true;
                _visible = true;
                SaveVisiblePreference(_visible);
                return false;
            }

            _lastToggleTime = Time.realtimeSinceStartup;
            SetVisible(!_visible, persist: true);
            return true;
        }

        // Updates sticky intent, persistence, clone visibility, alpha, and vanilla suppression together.
        private void SetVisible(bool visible, bool persist)
        {
            _visible = visible;
            if (persist)
            {
                SaveVisiblePreference(_visible);
            }

            if (_clone != null)
            {
                if (_clone.activeSelf != Visible)
                {
                    _clone.SetActive(Visible);
                }

                ApplyHudAlpha(_clone, FrozTLDMod.GetHudAlpha(_visible));
                // Once the clone owns the sundial, keep the native source hidden
                // on both on and off toggles so it cannot perform a second fade.
                DisableVanillaPanelActionsTimeWidget();
            }

            if (Visible)
            {
                RunVisibleMaintenanceIfNeeded(force: true);
            }
        }

        // Turns sticky HUD on once per session after the game allows normal HUD rendering.
        private void ApplyAutoTabIfNeeded()
        {
            // "Auto Tab" means: if Sticky HUD is enabled, behave as though the
            // player turned the Froz HUD on once after gameplay HUD becomes valid.
            // It does not keep re-enabling after the player turns the HUD off.
            if (_autoTabApplied)
            {
                return;
            }

            if (!FrozTLDMod.Settings.StickyHud || _visible)
            {
                return;
            }

            if (!FrozTLDMod.IsHudAllowed())
            {
                return;
            }

            _autoTabApplied = true;
            _visible = true;
            _waitingForTemplate = _clone == null;
            SaveVisiblePreference(_visible);

            if (_clone == null && !_autoTemplateRequested && !TryCreateClone())
            {
                // Fresh sessions may not have a Panel_Actions TimeWidget until
                // vanilla's Tab path runs. Ask the suppressor to run that path
                // once with our Harmony prefix bypassed.
                _autoTemplateRequested = true;
                VanillaHudSuppressor.RunVanillaSurvivalPanelActionOnce();
            }
        }

        // Persists the last sticky visibility for compatibility and session state inspection.
        private static void SaveVisiblePreference(bool visible)
        {
            PlayerPrefs.SetInt(VisiblePreferenceKey, visible ? 1 : 0);
            PlayerPrefs.Save();
        }

        // Clones the Panel_Actions TimeWidget under a stable parent and adopts its native layout.
        private bool TryCreateClone()
        {
            var source = FindPanelActionsTimeWidget();
            if (source == null || source.gameObject == null || source.transform.parent == null)
            {
                return false;
            }

            var stableParent = source.transform.parent.parent;
            if (stableParent == null)
            {
                return false;
            }

            _sourceWidget = source;
            _clone = Object.Instantiate(source.gameObject);
            _clone.name = CloneName;
            // Reparent with worldPositionStays=false. Preserving world position in
            // NGUI produced enormous local coordinates and pushed the clone offscreen.
            _clone.transform.SetParent(stableParent, false);
            _clone.transform.localPosition = source.transform.parent.localPosition + source.transform.localPosition;
            _clone.transform.localRotation = source.transform.localRotation;
            _clone.transform.localScale = source.transform.localScale;
            _clone.layer = source.gameObject.layer;
            _clone.SetActive(Visible);
            _cloneWidget = _clone.GetComponent<TimeWidget>();
            _cloneWidgets = _clone.GetComponentsInChildren<UIWidget>(true);
            _horizonWidget = FindChildWidget(_clone, "horizon");
            _cachedCamera = null;
            _hasCachedHorizonImguiRect = false;
            _nextHorizonRectRefreshTime = -1f;
            DisableArrows();

            CopySourceRadii();
            RestoreVanillaColors(_clone);
            ApplyHudAlpha(_clone, FrozTLDMod.GetHudAlpha(_visible));
            DisableVanillaPanelActionsTimeWidget();

            _waitingForTemplate = false;
            return true;
        }

        // Throttles alpha, sun/moon radius, and vanilla-widget maintenance while the clone is visible.
        private void RunVisibleMaintenanceIfNeeded(bool force)
        {
            if (_clone == null || !Visible)
            {
                return;
            }

            if (!force && Time.realtimeSinceStartup < _nextMaintenanceTime)
            {
                return;
            }

            _nextMaintenanceTime = Time.realtimeSinceStartup + 0.25f;
            ApplyHudAlpha(_clone, FrozTLDMod.GetHudAlpha(_visible));
            CopySourceRadii();
            DisableRestPanelTimeWidget();
            UpdateRestClone();
        }

        // Activates and updates the separate clone used by rest and pass-time interface layers.
        private void UpdateRestClone()
        {
            // The rest/pass-time UI draws in a different layer than the normal
            // survival HUD. A second clone in that layer keeps the sundial visible
            // and fully bright from bed setup through accelerated rest.
            var showRestClone = Visible && FrozTLDMod.ShouldUseRestTimeHudLayer();

            if (!showRestClone)
            {
                if (_restClone != null && _restClone.activeSelf)
                {
                    _restClone.SetActive(false);
                }

                return;
            }

            if (_restClone == null && !TryCreateRestClone())
            {
                return;
            }

            if (_restClone == null)
            {
                return;
            }

            if (!_restClone.activeSelf)
            {
                _restClone.SetActive(true);
            }

            ApplyHudAlpha(_restClone, FrozTLDMod.GetHudAlpha(_visible), _restCloneWidgets, ref _lastAppliedRestAlpha);
            CopySourceRadiiToRestClone();
            DisablePanelHudTimePopupWidget();
        }

        // Creates the rest-layer clone at the native Panel_HUD TimePopup position.
        private bool TryCreateRestClone()
        {
            if (_clone == null || Time.realtimeSinceStartup < _nextRestCloneSearchTime)
            {
                return false;
            }

            _nextRestCloneSearchTime = Time.realtimeSinceStartup + 0.5f;

            var target = FindPanelHudTimePopupTimeWidget();
            if (target == null || target.gameObject == null || target.transform.parent == null)
            {
                return false;
            }

            _restClone = Object.Instantiate(_clone);
            _restClone.name = RestCloneName;
            _restClone.transform.SetParent(target.transform.parent, false);
            _restClone.transform.localPosition = target.transform.localPosition;
            _restClone.transform.localRotation = target.transform.localRotation;
            _restClone.transform.localScale = target.transform.localScale;
            SetLayerRecursive(_restClone, target.gameObject.layer);
            _restCloneWidget = _restClone.GetComponent<TimeWidget>();
            _restCloneWidgets = _restClone.GetComponentsInChildren<UIWidget>(true);
            _restClone.SetActive(true);
            RestoreVanillaColors(_restClone);
            ApplyHudAlpha(_restClone, FrozTLDMod.GetHudAlpha(_visible), _restCloneWidgets, ref _lastAppliedRestAlpha);
            DisableArrows(_restClone, _restCloneWidgets);
            DisablePanelHudTimePopupWidget();
            return true;
        }

        // Keeps the rest clone's sun/moon arcs synchronized with the live source widget.
        private void CopySourceRadiiToRestClone()
        {
            if (_sourceWidget == null || _restCloneWidget == null)
            {
                return;
            }

            _restCloneWidget.m_SunRadius = _sourceWidget.m_SunRadius;
            _restCloneWidget.m_MoonRadius = _sourceWidget.m_MoonRadius;
        }

        // Keeps the primary clone's sun/moon arcs synchronized with the live source widget.
        private void CopySourceRadii()
        {
            // Instantiated TimeWidgets initialize their sun/moon radii differently
            // than the live source. Copying radii keeps our sun/moon arc aligned
            // with vanilla.
            if (_sourceWidget == null || _cloneWidget == null)
            {
                return;
            }

            _cloneWidget.m_SunRadius = _sourceWidget.m_SunRadius;
            _cloneWidget.m_MoonRadius = _sourceWidget.m_MoonRadius;
        }

        // Reports whether the sticky HUD controller should currently operate.
        private static bool IsEnabled()
        {
            return FrozTLDMod.IsStickyHudEnabled();
        }
    }
}
