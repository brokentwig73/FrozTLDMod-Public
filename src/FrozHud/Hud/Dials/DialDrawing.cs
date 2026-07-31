using Il2Cpp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    internal sealed partial class FrozTLDModOverlay
    {
        // Returns an embedded texture's aspect ratio with a safe default for missing assets.
        private static float GetTextureAspectRatio(Texture2D texture)
        {
            if (texture == null || texture.height <= 0)
            {
                return 0.42f;
            }

            return Mathf.Max(0.1f, texture.width / (float)texture.height);
        }

        // Centers the shared circular dial background within an item's assigned layout box.
        private static Rect GetDialCircleRect(Rect itemRect, HudTuning tuning)
        {
            var circleSize = Mathf.Min(itemRect.width, itemRect.height) * tuning.DialSizeScale * 0.01f;
            return new Rect(
                itemRect.x + (itemRect.width - circleSize) * 0.5f,
                itemRect.y + (itemRect.height - circleSize) * 0.5f,
                circleSize,
                circleSize);
        }

        // Parses a configured HTML color and applies the caller's final HUD alpha.
        private static Color GetHudColor(string colorText, float alpha)
        {
            if (!string.IsNullOrEmpty(colorText) && colorText[0] != '#')
            {
                colorText = "#" + colorText;
            }

            ColorUtility.TryParseHtmlString(colorText, out var color);
            color.a = alpha;
            return color;
        }

        // Lazily loads the cloudy white background shared by circular gauges.
        private Texture2D GetDialBackgroundTexture()
        {
            if (_dialBackgroundTexture != null)
            {
                return _dialBackgroundTexture;
            }

            _dialBackgroundTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.dialBackground.png");
            return _dialBackgroundTexture;
        }

        // Loads a PNG embedded in the mod assembly into a runtime Unity texture.
        private static Texture2D LoadEmbeddedTexture(string resourceName)
        {
            // Assets are embedded resources so the deployed DLL is self-contained.
            try
            {
                using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    return null;
                }

                using var memory = new MemoryStream();
                stream.CopyTo(memory);

                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(texture, memory.ToArray()))
                {
                    UnityEngine.Object.Destroy(texture);
                    return null;
                }

                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                return texture;
            }
            catch
            {
                return null;
            }
        }

        // Returns the cached inward- or outward-facing triangle marker asset.
        private static Texture2D GetTriangleMarkerTexture(bool pointsInward)
        {
            if (pointsInward)
            {
                if (_triangleMarkerInTexture != null)
                {
                    return _triangleMarkerInTexture;
                }

                _triangleMarkerInTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.dialTriangleMarkerIn.png");
                return _triangleMarkerInTexture;
            }

            if (_triangleMarkerOutTexture != null)
            {
                return _triangleMarkerOutTexture;
            }

            _triangleMarkerOutTexture = LoadEmbeddedTexture("FrozTLDMods.FrozTLDMod.Assets.dialTriangleMarkerOut.png");
            return _triangleMarkerOutTexture;
        }

        // Converts marker scale percent into pixels using the exact triangle asset being drawn.
        private static float GetTriangleMarkerDrawSize(Texture2D texture, float sizePercent)
        {
            var nativeSize = Mathf.Max(texture.width, texture.height);
            return Mathf.Max(1f, nativeSize * sizePercent * 0.01f);
        }

        // Draws a directional triangle at an angle/radius and rotates it around its own center.
        private static void DrawTriangleMarker(
            Rect circleRect,
            float angleDegrees,
            float radiusPercent,
            float sizePercent,
            Color color,
            bool pointsInward,
            string markerName)
        {
            var texture = GetTriangleMarkerTexture(pointsInward);
            if (texture == null)
            {
                return;
            }

            var radius = circleRect.width * radiusPercent * 0.01f;
            var markerSize = GetTriangleMarkerDrawSize(texture, sizePercent);
            var radians = angleDegrees * Mathf.Deg2Rad;
            var markerCenter = new Vector2(
                circleRect.center.x + Mathf.Sin(radians) * radius,
                circleRect.center.y - Mathf.Cos(radians) * radius);
            var markerRect = new Rect(
                markerCenter.x - markerSize * 0.5f,
                markerCenter.y - markerSize * 0.5f,
                markerSize,
                markerSize);
            var oldMatrix = GUI.matrix;

            GUI.color = color;
            var centerRotationClockwiseDegrees = angleDegrees;
            GUIUtility.RotateAroundPivot(centerRotationClockwiseDegrees, markerRect.center);
            GUI.DrawTexture(markerRect, texture, ScaleMode.ScaleToFit, true);
            GUI.matrix = oldMatrix;
        }

        // Draws the outward wind marker using the same angle for position and orientation.
        private static void DrawWindTriangleMarker(
            Rect circleRect,
            float positionAngleDegrees,
            float radiusPercent,
            float sizePercent,
            Color color,
            string markerName)
        {
            var texture = GetTriangleMarkerTexture(pointsInward: false);
            if (texture == null)
            {
                return;
            }

            var radius = circleRect.width * radiusPercent * 0.01f;
            var markerSize = GetTriangleMarkerDrawSize(texture, sizePercent);
            var radians = positionAngleDegrees * Mathf.Deg2Rad;
            var markerCenter = new Vector2(
                circleRect.center.x + Mathf.Sin(radians) * radius,
                circleRect.center.y - Mathf.Cos(radians) * radius);
            var markerRect = new Rect(
                markerCenter.x - markerSize * 0.5f,
                markerCenter.y - markerSize * 0.5f,
                markerSize,
                markerSize);
            var oldMatrix = GUI.matrix;

            GUI.color = color;
            var centerRotationClockwiseDegrees = positionAngleDegrees;
            GUIUtility.RotateAroundPivot(centerRotationClockwiseDegrees, markerRect.center);
            GUI.DrawTexture(markerRect, texture, ScaleMode.ScaleToFit, true);
            GUI.matrix = oldMatrix;
        }


        // Lazily creates the white fill texture used behind the wind-speed value.
        private static Texture2D GetWindSpeedBadgeFillTexture()
        {
            if (_windSpeedBadgeFillTexture != null)
            {
                return _windSpeedBadgeFillTexture;
            }

            _windSpeedBadgeFillTexture = CreateCircleTexture(fill: true);
            return _windSpeedBadgeFillTexture;
        }

        // Recreates the thermometer value border only when its configured thickness changes.
        private static Texture2D GetThermometerValueBorderTexture(HudTuning tuning)
        {
            if (_thermometerValueBorderTexture != null &&
                Mathf.Approximately(_thermometerValueBorderSize, tuning.DialThermometerFontCircleBorder))
            {
                return _thermometerValueBorderTexture;
            }

            _thermometerValueBorderSize = tuning.DialThermometerFontCircleBorder;
            _thermometerValueBorderTexture = CreateCircleTexture(fill: false, tuning.DialThermometerFontCircleBorder);
            return _thermometerValueBorderTexture;
        }

        // Recreates the wind-speed border only when its configured thickness changes.
        private static Texture2D GetWindSpeedBadgeBorderTexture(HudTuning tuning)
        {
            if (_windSpeedBadgeBorderTexture != null &&
                Mathf.Approximately(_windSpeedBadgeBorderThicknessPercent, tuning.DialWindSpeedCircleBorder))
            {
                return _windSpeedBadgeBorderTexture;
            }

            _windSpeedBadgeBorderThicknessPercent = tuning.DialWindSpeedCircleBorder;
            _windSpeedBadgeBorderTexture = CreateCircleTexture(fill: false, tuning.DialWindSpeedCircleBorder);
            return _windSpeedBadgeBorderTexture;
        }

        // Generates an antialiased circular fill or ring texture for small numeric badges.
        private static Texture2D CreateCircleTexture(bool fill, float borderThicknessPercent = 5f)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var center = (size - 1) * 0.5f;
            var radius = center - 1f;
            var borderWidth = size * borderThicknessPercent * 0.01f;

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var distance = Mathf.Sqrt((x - center) * (x - center) + (y - center) * (y - center));
                    var edgeAlpha = Mathf.Clamp01(radius - distance + 1f);
                    var alpha = fill
                        ? edgeAlpha
                        : Mathf.Clamp01(distance - (radius - borderWidth)) * edgeAlpha;
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            texture.Apply();
            return texture;
        }
    }
}
