using Il2Cpp;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class FrozTimeHudController
    {
        // Moves a cloned hierarchy onto the NGUI layer used by its target parent.
        private static void SetLayerRecursive(GameObject root, int layer)
        {
            var transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (var transform in transforms)
            {
                if (transform != null && transform.gameObject != null)
                {
                    transform.gameObject.layer = layer;
                }
            }
        }

        // Returns the NGUI camera that renders the primary clone's layer.
        private Camera GetCloneCamera()
        {
            if (_cachedCamera != null)
            {
                return _cachedCamera;
            }

            _cachedCamera = NGUITools.FindCameraForLayer(_clone.layer);
            return _cachedCamera;
        }

        // Restores known vanilla widget colors after Unity cloning inherits transient fade colors.
        private static void RestoreVanillaColors(GameObject clone)
        {
            var widgets = clone.GetComponentsInChildren<UIWidget>(true);
            foreach (var widget in widgets)
            {
                if (widget == null)
                {
                    continue;
                }

                widget.color = GetVanillaColor(widget.gameObject.name, widget.color.a);
            }
        }

        // Applies HUD alpha to the primary clone using its cached widget array.
        private void ApplyHudAlpha(GameObject clone, float hudAlpha)
        {
            ApplyHudAlpha(clone, hudAlpha, _cloneWidgets, ref _lastAppliedAlpha);
        }

        // Applies vanilla base alpha values only when the requested HUD alpha has changed.
        private void ApplyHudAlpha(GameObject clone, float hudAlpha, UIWidget[] widgets, ref float lastAppliedAlpha)
        {
            if (clone == null)
            {
                return;
            }

            if (Mathf.Abs(hudAlpha - lastAppliedAlpha) < 0.001f)
            {
                return;
            }

            lastAppliedAlpha = hudAlpha;
            widgets ??= clone.GetComponentsInChildren<UIWidget>(true);
            foreach (var widget in widgets)
            {
                if (widget == null || widget.gameObject == null)
                {
                    continue;
                }

                if (widget.gameObject.name == "arrows")
                {
                    widget.color = GetVanillaColor(widget.gameObject.name, 0f);
                    widget.gameObject.SetActive(false);
                    continue;
                }

                widget.color = GetVanillaColor(widget.gameObject.name, GetVanillaBaseAlpha(widget.gameObject.name) * hudAlpha);
            }
        }

        // Returns the cached horizon widget used to align the custom IMGUI elements.
        private UIWidget GetHorizonWidget()
        {
            if (_horizonWidget != null && _horizonWidget.gameObject != null)
            {
                return _horizonWidget;
            }

            _horizonWidget = FindChildWidget(_clone, "horizon");
            return _horizonWidget;
        }

        // Disables the unused native directional arrows on the primary clone.
        private void DisableArrows()
        {
            DisableArrows(_clone, _cloneWidgets);
        }

        // Disables arrow widgets on any supplied TimeWidget clone.
        private static void DisableArrows(GameObject clone, UIWidget[] widgets)
        {
            if (clone == null)
            {
                return;
            }

            widgets ??= clone.GetComponentsInChildren<UIWidget>(true);
            foreach (var widget in widgets)
            {
                if (widget != null &&
                    widget.gameObject != null &&
                    widget.gameObject.name == "arrows")
                {
                    widget.color = GetVanillaColor(widget.gameObject.name, 0f);
                    widget.gameObject.SetActive(false);
                }
            }
        }

        // Finds a named UIWidget under a known clone hierarchy.
        private static UIWidget FindChildWidget(GameObject root, string childName)
        {
            var widgets = root.GetComponentsInChildren<UIWidget>(true);
            foreach (var widget in widgets)
            {
                if (widget != null &&
                    widget.gameObject != null &&
                    widget.gameObject.name == childName)
                {
                    return widget;
                }
            }

            return null;
        }

        // Converts NGUI world corners into a top-left-origin IMGUI rectangle.
        private static bool TryGetImguiRect(UIWidget widget, Camera camera, out Rect rect)
        {
            rect = default;

            var corners = widget.worldCorners;
            if (corners == null || corners.Count < 4)
            {
                return false;
            }

            var screen0 = camera.WorldToScreenPoint(corners[0]);
            var screen2 = camera.WorldToScreenPoint(corners[2]);
            var left = Mathf.Min(screen0.x, screen2.x);
            var right = Mathf.Max(screen0.x, screen2.x);
            var top = Screen.height - Mathf.Max(screen0.y, screen2.y);
            var bottom = Screen.height - Mathf.Min(screen0.y, screen2.y);

            rect = new Rect(left, top, right - left, bottom - top);
            return rect.width > 0f && rect.height > 0f;
        }

        // Returns the native foreground/background color family with a caller-supplied alpha.
        private static Color GetVanillaColor(string widgetName, float alpha)
        {
            return widgetName switch
            {
                "bg" => new Color(0f, 0f, 0f, alpha),
                "glow" => new Color(0f, 0f, 0f, alpha),
                _ => new Color(0.98f, 0.98f, 0.98f, alpha)
            };
        }

        // Returns the measured vanilla base alpha for each known TimeWidget layer.
        private static float GetVanillaBaseAlpha(string widgetName)
        {
            return widgetName switch
            {
                "bg" => 0.116f,
                "glow" => 0.116f,
                "horizon" => 0.784f,
                _ => 1f
            };
        }
    }
}
