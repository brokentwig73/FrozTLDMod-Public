using Il2Cpp;
using System.Collections.Generic;
using UnityEngine;

namespace FrozTLDMods.FrozTLDMod
{
    // Session-only weapon memory. Inventory "Use" is the authoritative way to
    // choose the preferred weapon. Radial and hotkey weapon equips consume that
    // remembered exact instance instead of replacing the memory with whatever
    // vanilla happened to select next. Radial remains exact-only; the hotkey can
    // choose an identical weapon only when the remembered instance is gone.
    internal sealed partial class RememberLastWeaponController
    {
        private const float RadialRecentWindowSeconds = 3.0f;
        private const float RadialPanelScanIntervalSeconds = 0.25f;
        private const string StoneGearName = "GEAR_Stone";
        private const string GunTypeRevolver = "Revolver";
        private const string GunTypeRifle = "Rifle";
        private const string GunTypeFlareGun = "FlareGun";

        private static WeaponMemoryRecord _rememberedWeapon;
        private static readonly List<Panel_ActionsRadial> _knownRadialPanels = new();
        private static readonly List<Panel_ActionsRadial> _visibleRadialPanels = new();
        private static float _lastRadialWeaponUiTime = -100f;
        private static float _nextRadialPanelScanTime;

        // Keeps visible radial-menu arms synchronized with the remembered exact weapon instance.
        public void Update()
        {
            if (!IsFeatureEnabled())
            {
                return;
            }

            if (_rememberedWeapon == null)
            {
                return;
            }

            CollectVisibleRadialPanels(_visibleRadialPanels);
            if (_visibleRadialPanels.Count > 0)
            {
                _lastRadialWeaponUiTime = Time.realtimeSinceStartup;
            }

            ApplyRememberedWeaponToRadialMenu(_visibleRadialPanels);
        }

        // Records inventory selections or substitutes memory into radial/hotkey weapon requests.
        internal static void HandleWeaponSelection(PlayerManager playerManager, ref GearItem selectedGear)
        {
            if (!IsFeatureEnabled() ||
                playerManager == null ||
                selectedGear == null)
            {
                return;
            }

            if (!IsRememberableWeapon(selectedGear))
            {
                return;
            }

            if (IsInventoryWeaponUiActive())
            {
                RememberWeapon(selectedGear);
                return;
            }

            MaybeReplaceWithRememberedWeapon(
                playerManager,
                ref selectedGear,
                allowIdenticalFallback: !IsRadialWeaponSelectionRecent());
        }

        // Replaces vanilla's choice with the remembered instance, optionally using an identical replacement.
        private static void MaybeReplaceWithRememberedWeapon(
            PlayerManager playerManager,
            ref GearItem selectedGear,
            bool allowIdenticalFallback)
        {
            if (_rememberedWeapon == null)
            {
                return;
            }

            var rememberedGear = GetRememberedGear(playerManager);
            var usedIdenticalFallback = false;
            if (rememberedGear == null && allowIdenticalFallback)
            {
                rememberedGear = FindIdenticalRememberedWeapon(playerManager);
                usedIdenticalFallback = rememberedGear != null;
            }

            if (rememberedGear == null)
            {
                return;
            }

            var heldGear = GetHeldGear(playerManager);
            if (IsSameInstance(heldGear, rememberedGear) || IsSameInstance(selectedGear, rememberedGear))
            {
                return;
            }

            if (usedIdenticalFallback)
            {
                RememberWeapon(rememberedGear);
            }

            selectedGear = rememberedGear;
        }

        // Saves the exact weapon explicitly chosen through inventory UI.
        private static void RememberWeapon(GearItem gear)
        {
            var record = new WeaponMemoryRecord(
                GetInstanceId(gear),
                SafeGearName(gear),
                DescribeWeaponKind(gear),
                gear);

            if (_rememberedWeapon != null &&
                _rememberedWeapon.InstanceId == record.InstanceId &&
                _rememberedWeapon.GearName == record.GearName)
            {
                return;
            }

            _rememberedWeapon = record;
        }

        // Returns the remembered GearItem only while the same valid native instance still exists.
        private static GearItem GetRememberedGear(PlayerManager playerManager)
        {
            if (_rememberedWeapon == null)
            {
                return null;
            }

            var rememberedGear = _rememberedWeapon.Gear;
            if (IsSameInstance(rememberedGear, _rememberedWeapon.InstanceId) &&
                SafeGearName(rememberedGear) == _rememberedWeapon.GearName &&
                IsRememberableWeapon(rememberedGear))
            {
                return rememberedGear;
            }

            return null;
        }

        // Finds another inventory item with the remembered gear name for hotkey use after the original is gone.
        private static GearItem FindIdenticalRememberedWeapon(PlayerManager playerManager)
        {
            if (_rememberedWeapon == null || playerManager == null)
            {
                return null;
            }

            var heldGear = GetHeldGear(playerManager);
            if (IsIdenticalRememberedWeapon(heldGear))
            {
                return heldGear;
            }

            var gearList = Il2CppReflection.GetObjectMember(playerManager, "m_GearItemList");
            foreach (var item in Il2CppList.Enumerate(gearList))
            {
                if (item is GearItem gear && IsIdenticalRememberedWeapon(gear))
                {
                    return gear;
                }
            }

            return null;
        }

        // Tests whether a gear item is a same-type replacement rather than the remembered instance itself.
        private static bool IsIdenticalRememberedWeapon(GearItem gear)
        {
            return gear != null &&
                   _rememberedWeapon != null &&
                   GetInstanceId(gear) != _rememberedWeapon.InstanceId &&
                   SafeGearName(gear) == _rememberedWeapon.GearName &&
                   IsRememberableWeapon(gear);
        }

        // Restricts weapon memory to stones, bows, revolvers, rifles, and flare guns.
        private static bool IsRememberableWeapon(GearItem gear)
        {
            if (gear == null)
            {
                return false;
            }

            var gearName = SafeGearName(gear);
            if (gearName == StoneGearName)
            {
                return true;
            }

            if (Il2CppReflection.GetObjectMember(gear, "m_BowItem") != null)
            {
                return true;
            }

            var gunItem = Il2CppReflection.GetObjectMember(gear, "m_GunItem");
            var gunType = Il2CppReflection.GetObjectMember(gunItem, "m_GunType")?.ToString();
            return gunType == GunTypeRevolver ||
                   gunType == GunTypeRifle ||
                   gunType == GunTypeFlareGun;
        }

        // Detects inventory weapon selection so it becomes authoritative memory rather than an override target.
        private static bool IsInventoryWeaponUiActive()
        {
            var inventoryVisible =
                InterfaceManager.TryGetPanel<Panel_Inventory>(out var inventoryPanel) &&
                IsPanelVisible(inventoryPanel);
            var examineVisible =
                InterfaceManager.TryGetPanel<Panel_Inventory_Examine>(out var examinePanel) &&
                IsPanelVisible(examinePanel);
            return inventoryVisible || examineVisible;
        }

        // Safely tests whether a generic native panel is active and enabled.
        private static bool IsPanelVisible(Panel_Base panel)
        {
            if (panel == null || panel.gameObject == null || !panel.gameObject.activeInHierarchy)
            {
                return false;
            }

            try
            {
                return panel.IsEnabled();
            }
            catch
            {
                return false;
            }
        }

        // Classifies remembered weapons for readable logging and future behavior splits.
        private static string DescribeWeaponKind(GearItem gear)
        {
            var gearName = SafeGearName(gear);
            if (gearName == StoneGearName)
            {
                return "Stone";
            }

            if (Il2CppReflection.GetObjectMember(gear, "m_BowItem") != null)
            {
                return "Bow";
            }

            var gunItem = Il2CppReflection.GetObjectMember(gear, "m_GunItem");
            var gunType = Il2CppReflection.GetObjectMember(gunItem, "m_GunType")?.ToString();
            return string.IsNullOrEmpty(gunType) ? "Unknown" : gunType;
        }

        // Returns the gear currently held by the local player.
        private static GearItem GetHeldGear()
        {
            return GetHeldGear(GameManager.GetPlayerManagerComponent());
        }

        // Returns the gear currently held by a supplied PlayerManager.
        private static GearItem GetHeldGear(PlayerManager playerManager)
        {
            return Il2CppReflection.GetObjectMember(playerManager, "m_ItemInHands") as GearItem;
        }

        // Compares two GearItems by the game's persistent instance ID.
        private static bool IsSameInstance(GearItem gear, GearItem target)
        {
            return gear != null && target != null && GetInstanceId(gear) == GetInstanceId(target);
        }

        // Compares a GearItem to a stored native instance ID.
        private static bool IsSameInstance(GearItem gear, int instanceId)
        {
            return gear != null && GetInstanceId(gear) == instanceId;
        }

        // Reads a GearItem's native instance ID with a non-matching null sentinel.
        private static int GetInstanceId(GearItem gear)
        {
            return gear != null ? gear.m_InstanceID : int.MinValue;
        }

        // Reads a gear name defensively across destroyed IL2CPP wrappers.
        private static string SafeGearName(GearItem gear)
        {
            if (gear == null)
            {
                return string.Empty;
            }

            try
            {
                return gear.name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        // Reports whether weapon memory is enabled globally and in its own setting.
        private static bool IsFeatureEnabled()
        {
            return FrozTLDMod.Settings != null &&
                   FrozTLDMod.Settings.Enabled &&
                   FrozTLDMod.Settings.RememberWeapon;
        }

        // Reads normalized condition defensively for radial display refreshes.
        private static float GetCondition(GearItem gear)
        {
            try
            {
                return gear != null ? gear.GetNormalizedCondition() : 0f;
            }
            catch
            {
                return 0f;
            }
        }

        // Updates a reflected radial label only when it is a UILabel.
        private static void SetLabelText(object labelObject, string text)
        {
            if (labelObject is UILabel label)
            {
                label.text = text;
            }
        }


        private sealed class WeaponMemoryRecord
        {
            // Captures the exact selected weapon and the identifiers needed to validate or replace it.
            public WeaponMemoryRecord(int instanceId, string gearName, string kind, GearItem gear)
            {
                InstanceId = instanceId;
                GearName = gearName;
                Kind = kind;
                Gear = gear;
            }

            public int InstanceId { get; }
            public string GearName { get; }
            public string Kind { get; }
            public GearItem Gear { get; }
        }

    }
}
