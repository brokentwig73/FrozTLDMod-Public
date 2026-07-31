using Il2Cpp;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class FrozTimeHudController
    {
        // Finds the vanilla Panel_Actions TimeWidget template while excluding both Froz clones.
        private static TimeWidget FindPanelActionsTimeWidget()
        {
            _panelActions = PanelCache.Get(_panelActions);
            if (_panelActions == null || _panelActions.gameObject == null)
            {
                return null;
            }

            var widgets = _panelActions.gameObject.GetComponentsInChildren<TimeWidget>(true);
            foreach (var widget in widgets)
            {
                if (widget == null || widget.gameObject == null || widget.transform == null)
                {
                    continue;
                }

                if (widget.gameObject.name == CloneName)
                {
                    continue;
                }

                if (widget.gameObject.name == RestCloneName)
                {
                    continue;
                }

                var parent = widget.transform.parent;
                if (parent == _panelActions.transform)
                {
                    return widget;
                }
            }

            return null;
        }

        // Finds and caches the native Panel_HUD TimePopup widget used as the rest-layer anchor.
        private static TimeWidget FindPanelHudTimePopupTimeWidget()
        {
            if (_panelHudTimePopupWidget != null &&
                _panelHudTimePopupWidget.gameObject != null &&
                IsPanelHudTimePopupTimeWidget(_panelHudTimePopupWidget))
            {
                return _panelHudTimePopupWidget;
            }

            _panelHud = PanelCache.Get(_panelHud);
            if (_panelHud == null || _panelHud.gameObject == null)
            {
                return null;
            }

            var widgets = _panelHud.gameObject.GetComponentsInChildren<TimeWidget>(true);
            foreach (var widget in widgets)
            {
                if (widget == null || widget.gameObject == null || widget.transform == null)
                {
                    continue;
                }

                if (widget.gameObject.name == CloneName ||
                    widget.gameObject.name == RestCloneName)
                {
                    continue;
                }

                if (IsPanelHudTimePopupTimeWidget(widget))
                {
                    _panelHudTimePopupWidget = widget;
                    return widget;
                }
            }

            return null;
        }

        // Disables only the vanilla Panel_Actions TimeWidget so the rest of Panel_Actions keeps working.
        private void DisableVanillaPanelActionsTimeWidget()
        {
            // Disable only the specific vanilla TimeWidget under Panel_Actions.
            // Do not disable Panel_Actions itself; it owns broader survival-panel
            // behavior. Other TimeWidgets, such as Panel_Rest, remain untouched.
            var source = _sourceWidget;
            if (source == null || source.gameObject == null)
            {
                source = FindPanelActionsTimeWidget();
                _sourceWidget = source;
            }

            if (source == null || source.gameObject == null)
            {
                return;
            }

            if (source.enabled)
            {
                source.enabled = false;
            }

            if (source.gameObject.activeSelf)
            {
                source.gameObject.SetActive(false);
            }
        }

        // Hides the native Panel_Rest widget while the Froz rest clone is responsible for the sundial.
        private static void DisableRestPanelTimeWidget()
        {
            var restWidget = FrozTLDMod.GetPanelRestTimeWidgetForSuppression();
            if (restWidget == null || restWidget.gameObject == null)
            {
                return;
            }

            if (restWidget.gameObject.activeSelf)
            {
                restWidget.gameObject.SetActive(false);
            }
        }

        // Disables the native HUD TimePopup widget after its transform is used to anchor the rest clone.
        private static void DisablePanelHudTimePopupWidget()
        {
            var timePopupWidget = FindPanelHudTimePopupTimeWidget();
            if (timePopupWidget == null || timePopupWidget.gameObject == null)
            {
                return;
            }

            if (timePopupWidget.enabled)
            {
                timePopupWidget.enabled = false;
            }

            if (timePopupWidget.gameObject.activeSelf)
            {
                timePopupWidget.gameObject.SetActive(false);
            }
        }

        // Identifies the exact TimeWidget under TimePopup and Panel_HUD by its parent chain.
        private static bool IsPanelHudTimePopupTimeWidget(TimeWidget widget)
        {
            var parent = widget.transform.parent;
            if (parent == null || parent.name != "TimeWidgetPos")
            {
                return false;
            }

            var hasTimePopup = false;
            var hasPanelHud = false;
            var current = parent;
            while (current != null)
            {
                if (current.name == "TimePopup")
                {
                    hasTimePopup = true;
                }
                else if (current.name == "Panel_HUD")
                {
                    hasPanelHud = true;
                }

                current = current.parent;
            }

            return hasTimePopup && hasPanelHud;
        }
    }
}
