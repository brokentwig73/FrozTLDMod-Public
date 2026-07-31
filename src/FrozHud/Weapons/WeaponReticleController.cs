using Il2Cpp;
using System;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // Uses the vanilla bow crosshair as a template, but renders our own clone
    // under Panel_HUD. The original Sprite_Crosshair can sit under inactive
    // vanilla parents for guns, so cloning avoids fighting those parent states.
    internal sealed class WeaponReticleController
    {
        private const string CloneName = "FrozTLDMod_WeaponReticle";
        private const string PistolCloneName = "FrozTLDMod_PistolReticle";
        private const string GunTypeRevolver = "Revolver";
        private const string GunTypeRifle = "Rifle";
        private const string GunTypeFlareGun = "FlareGun";
        private const string PredictionCameraName = "CameraGlobalRT";
        private const float ReferenceRefreshSeconds = 0.25f;
        private const float RevolverReticleRayDistanceMeters = 1000f;
        private static readonly Vector3 RevolverHipFireCameraOffset = new Vector3(0.635f, -0.318f, 1.135f);
        private static readonly Color RevolverReticleColor = new Color(1f, 0.08f, 0.08f, 1f);

        private UISprite _templateCrosshair;
        private GameObject _cloneObject;
        private UISprite _cloneCrosshair;
        private GameObject _pistolCloneObject;
        private UISprite _pistolCloneCrosshair;
        private Camera _reticleUiCamera;
        private Panel_HUD _panelHud;
        private GearItem _activeHeldGear;
        private vp_FPSWeapon _activeWeapon;
        private vp_FPSCamera _activeShooterCamera;
        private Camera _predictionCamera;
        private float _nextReferenceRefreshTime;
        private float _pistolCloneScreenDepth;
        private bool _hasPistolCloneScreenDepth;

        // Selects the correct reticle behavior for the held gun and updates its visibility/position.
        public void Update()
        {
            var gunItem = GetHeldGunItem();
            var gunType = GetGunTypeName(gunItem);
            if (!ShouldShowReticleForHeldGun(gunItem, gunType))
            {
                SetCloneVisible(false);
                SetPistolCloneVisible(false);
                return;
            }

            RefreshReferencesIfNeeded();
            if (gunType == GunTypeRevolver)
            {
                SetCloneVisible(false);
                if (TryGetPredictedRevolverReticlePoint(out var point) &&
                    TryCreatePistolClone() &&
                    ApplyPistolClone(point))
                {
                    SetPistolCloneVisible(true);
                }
                else
                {
                    SetPistolCloneVisible(false);
                }

                return;
            }

            SetPistolCloneVisible(false);
            if (TryCreateClone())
            {
                ApplyCloneVisuals(gunType);
                SetCloneVisible(true);
            }
        }

        // Applies settings and native aiming/interaction rules before an added reticle may appear.
        private static bool ShouldShowReticleForHeldGun(GunItem gunItem, string gunType)
        {
            if (FrozTLDMod.Settings == null || !FrozTLDMod.Settings.Enabled)
            {
                return false;
            }

            if (gunItem == null)
            {
                return false;
            }

            // Hard-coded on: bullets follow the weapon's true aim direction, not
            // this center-screen reference dot, so hide the added reticle while
            // actively aiming.
            if (IsGunAiming(gunItem))
            {
                return false;
            }

            if (HasInteractiveObjectUnderCrosshair())
            {
                return false;
            }

            return (gunType == GunTypeRevolver && FrozTLDMod.Settings.ReticleOnPistols) ||
                   (gunType == GunTypeRifle && FrozTLDMod.Settings.ReticleOnRifles) ||
                   (gunType == GunTypeFlareGun && FrozTLDMod.Settings.ReticleOnFlareGun);
        }

        // Returns the GunItem component from the gear currently held by the player.
        private static GunItem GetHeldGunItem()
        {
            var heldGear = GetHeldGear();
            return heldGear != null ? heldGear.m_GunItem : null;
        }

        // Returns the gear currently held by the local player.
        private static GearItem GetHeldGear()
        {
            var playerManager = GameManager.GetPlayerManagerComponent();
            return playerManager != null ? playerManager.m_ItemInHands : null;
        }

        // Converts the native gun-type enum to the stable names used by settings logic.
        private static string GetGunTypeName(GunItem gunItem)
        {
            return gunItem != null ? gunItem.m_GunType.ToString() : string.Empty;
        }

        // Uses the gun's own aiming state so added reticles follow vanilla bow behavior.
        private static bool IsGunAiming(GunItem gunItem)
        {
            return gunItem != null && gunItem.IsAiming();
        }

        // Suppresses weapon reticles when the crosshair is being used for a world interaction.
        private static bool HasInteractiveObjectUnderCrosshair()
        {
            var playerManager = GameManager.GetPlayerManagerComponent();
            return playerManager != null && playerManager.HasInteractiveObjectUnderCrossHair();
        }

        // Creates the centered rifle/flare-gun reticle from the vanilla bow sprite once.
        private bool TryCreateClone()
        {
            if (_cloneObject != null && _cloneCrosshair != null)
            {
                return true;
            }

            var panel = GetPanelHud();
            var template = GetTemplateCrosshair();
            if (panel == null || panel.gameObject == null || template == null || template.gameObject == null)
            {
                return false;
            }

            _cloneObject = UnityEngine.Object.Instantiate(template.gameObject);
            _cloneObject.name = CloneName;
            _cloneObject.transform.SetParent(panel.transform, false);
            _cloneObject.transform.localPosition = Vector3.zero;
            _cloneObject.transform.localRotation = Quaternion.identity;
            _cloneObject.transform.localScale = template.transform.localScale;
            _cloneObject.layer = template.gameObject.layer;
            _cloneCrosshair = _cloneObject.GetComponent<UISprite>();
            SetCloneVisible(false);
            return _cloneCrosshair != null;
        }

        // Copies vanilla dimensions and applies the color/depth appropriate to the gun type.
        private void ApplyCloneVisuals(string gunType)
        {
            if (_cloneCrosshair == null)
            {
                return;
            }

            var template = GetTemplateCrosshair();
            _cloneCrosshair.spriteName = template != null ? template.spriteName : "crosshair4";
            _cloneCrosshair.width = template != null ? template.width : 22;
            _cloneCrosshair.height = template != null ? template.height : 22;
            _cloneCrosshair.color = gunType == GunTypeRevolver
                ? RevolverReticleColor
                : new Color(0.98f, 0.98f, 0.98f, 1f);
            _cloneCrosshair.alpha = 1f;
            _cloneCrosshair.depth = template != null ? template.depth : _cloneCrosshair.depth;
            _cloneObject.transform.localPosition = Vector3.zero;
        }

        // Creates the revolver reticle beside the vanilla sprite so it inherits the same HUD lifecycle.
        private bool TryCreatePistolClone()
        {
            if (_pistolCloneObject != null && _pistolCloneCrosshair != null)
            {
                return true;
            }

            var template = GetTemplateCrosshair();
            if (template == null || template.gameObject == null || template.transform.parent == null)
            {
                return false;
            }

            _pistolCloneObject = UnityEngine.Object.Instantiate(template.gameObject);
            _pistolCloneObject.name = PistolCloneName;
            _pistolCloneObject.transform.SetParent(template.transform.parent, false);
            _pistolCloneObject.transform.localPosition = template.transform.localPosition;
            _pistolCloneObject.transform.localRotation = template.transform.localRotation;
            _pistolCloneObject.transform.localScale = template.transform.localScale;
            _pistolCloneObject.layer = template.gameObject.layer;
            _pistolCloneCrosshair = _pistolCloneObject.GetComponent<UISprite>();
            _reticleUiCamera = NGUITools.FindCameraForLayer(_pistolCloneObject.layer);
            CachePistolCloneScreenDepth();
            ApplyPistolCloneVisuals();
            SetPistolCloneVisible(false);
            return _pistolCloneCrosshair != null;
        }

        // Applies the vanilla sprite geometry with the revolver-specific red color.
        private void ApplyPistolCloneVisuals()
        {
            if (_pistolCloneObject == null || _pistolCloneCrosshair == null)
            {
                return;
            }

            var template = GetTemplateCrosshair();
            _pistolCloneCrosshair.spriteName = template != null ? template.spriteName : "crosshair4";
            _pistolCloneCrosshair.width = template != null ? template.width : 22;
            _pistolCloneCrosshair.height = template != null ? template.height : 22;
            _pistolCloneCrosshair.color = RevolverReticleColor;
            _pistolCloneCrosshair.alpha = 1f;
            _pistolCloneCrosshair.depth = template != null ? template.depth : _pistolCloneCrosshair.depth;
        }

        // Moves the revolver clone to the predicted impact point.
        private bool ApplyPistolClone(Vector2 point)
        {
            if (_pistolCloneObject == null || _pistolCloneCrosshair == null)
            {
                return false;
            }

            return TryMovePistolCloneToScreenPoint(point);
        }

        // Converts a screen-space impact point into the NGUI camera's world space.
        private bool TryMovePistolCloneToScreenPoint(Vector2 point)
        {
            if (_reticleUiCamera == null)
            {
                _reticleUiCamera = NGUITools.FindCameraForLayer(_pistolCloneObject.layer);
            }

            if (_reticleUiCamera == null)
            {
                return false;
            }

            if (!_hasPistolCloneScreenDepth)
            {
                CachePistolCloneScreenDepth();
            }

            var targetScreenPoint = new Vector3(point.x, Screen.height - point.y, _pistolCloneScreenDepth);
            _pistolCloneObject.transform.position = _reticleUiCamera.ScreenToWorldPoint(targetScreenPoint);
            return true;
        }

        // Caches the clone's NGUI depth so screen-to-world movement preserves its draw plane.
        private void CachePistolCloneScreenDepth()
        {
            if (_reticleUiCamera == null || _pistolCloneObject == null)
            {
                _hasPistolCloneScreenDepth = false;
                return;
            }

            _pistolCloneScreenDepth = _reticleUiCamera.WorldToScreenPoint(_pistolCloneObject.transform.position).z;
            _hasPistolCloneScreenDepth = true;
        }

        // Reproduces the revolver's hip-fire ray and projects its world hit into HUD coordinates.
        private bool TryGetPredictedRevolverReticlePoint(out Vector2 point)
        {
            point = default;
            var cameraTransform = _activeShooterCamera != null ? _activeShooterCamera.transform : null;
            if (cameraTransform == null || _predictionCamera == null)
            {
                return false;
            }

            var origin = cameraTransform.TransformPoint(RevolverHipFireCameraOffset);
            var ray = new Ray(origin, cameraTransform.forward);
            if (!Physics.Raycast(ray, out var hit, RevolverReticleRayDistanceMeters))
            {
                return false;
            }

            return TryProjectWorldPoint(_predictionCamera, hit.point, out point);
        }

        // Refreshes weapon and camera references only when they become stale or the held gear changes.
        private void RefreshReferencesIfNeeded()
        {
            var heldGear = GetHeldGear();
            var heldGearChanged = heldGear != _activeHeldGear;
            var weaponNeedsRefresh = heldGearChanged ||
                                     _activeWeapon == null ||
                                     _activeWeapon.gameObject == null ||
                                     !_activeWeapon.gameObject.activeInHierarchy ||
                                     _activeWeapon.m_GearItem != heldGear;
            var predictionCameraNeedsRefresh = _predictionCamera == null ||
                                               _predictionCamera.gameObject == null ||
                                               !_predictionCamera.enabled ||
                                               !_predictionCamera.gameObject.activeInHierarchy;

            if (!heldGearChanged && Time.realtimeSinceStartup < _nextReferenceRefreshTime)
            {
                return;
            }

            _nextReferenceRefreshTime = Time.realtimeSinceStartup + ReferenceRefreshSeconds;
            if (weaponNeedsRefresh)
            {
                _activeHeldGear = heldGear;
                _activeWeapon = FindActiveWeapon(heldGear);
                _activeShooterCamera = FindActiveShooterCamera();
            }

            if (predictionCameraNeedsRefresh)
            {
                _predictionCamera = FindCameraByName(PredictionCameraName);
            }
        }

        // Finds the active first-person weapon component associated with the held GearItem.
        private static vp_FPSWeapon FindActiveWeapon(GearItem heldGear)
        {
            if (heldGear == null)
            {
                return null;
            }

            var weapons = Resources.FindObjectsOfTypeAll<vp_FPSWeapon>();
            foreach (var weapon in weapons)
            {
                if (weapon == null || weapon.gameObject == null || !weapon.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (weapon.m_GearItem == heldGear)
                {
                    return weapon;
                }
            }

            return null;
        }

        // Returns the shooter camera used by the active first-person weapon.
        private vp_FPSCamera FindActiveShooterCamera()
        {
            if (_activeWeapon == null || _activeWeapon.gameObject == null)
            {
                return null;
            }

            var shooter = _activeWeapon.gameObject.GetComponent<vp_FPSShooter>();
            return shooter != null ? shooter.m_Camera : null;
        }

        // Finds an enabled game camera by name during the throttled reference refresh.
        private static Camera FindCameraByName(string name)
        {
            var cameras = Resources.FindObjectsOfTypeAll<Camera>();
            foreach (var camera in cameras)
            {
                if (camera == null || camera.gameObject == null || !camera.enabled || !camera.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (string.Equals(camera.name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return camera;
                }
            }

            return null;
        }

        // Projects a visible world point into the top-left-origin coordinates used by the HUD clone.
        private static bool TryProjectWorldPoint(Camera camera, Vector3 worldPoint, out Vector2 point)
        {
            point = default;
            if (camera == null)
            {
                return false;
            }

            var screenPoint = camera.WorldToScreenPoint(worldPoint);
            if (screenPoint.z <= 0f)
            {
                return false;
            }

            point = new Vector2(screenPoint.x, Screen.height - screenPoint.y);
            return point.x >= 0f && point.x <= Screen.width && point.y >= 0f && point.y <= Screen.height;
        }

        // Shows or hides the centered rifle/flare-gun clone.
        private void SetCloneVisible(bool visible)
        {
            if (_cloneObject != null)
            {
                _cloneObject.SetActive(visible);
            }
        }

        // Shows or hides the predicted revolver clone.
        private void SetPistolCloneVisible(bool visible)
        {
            if (_pistolCloneObject != null)
            {
                _pistolCloneObject.SetActive(visible);
            }
        }

        // Caches the vanilla bow crosshair sprite used as the visual template for all clones.
        private UISprite GetTemplateCrosshair()
        {
            if (_templateCrosshair != null && _templateCrosshair.gameObject != null)
            {
                return _templateCrosshair;
            }

            var panel = GetPanelHud();
            if (panel == null || panel.gameObject == null)
            {
                return null;
            }

            var nonEssentialHud = FindChild(panel.transform, "NonEssentialHud");
            var crosshair = FindChild(nonEssentialHud, "Sprite_Crosshair");
            if (crosshair == null)
            {
                return null;
            }

            _templateCrosshair = crosshair.gameObject.GetComponent<UISprite>();
            return _templateCrosshair;
        }

        // Returns the cached native HUD panel, refreshing it if Unity destroyed the old instance.
        private Panel_HUD GetPanelHud()
        {
            _panelHud = PanelCache.Get(_panelHud);
            return _panelHud;
        }

        // Finds a direct child by native object name without performing a global scene search.
        private static Transform FindChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child != null && child.gameObject != null && child.gameObject.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

    }
}
