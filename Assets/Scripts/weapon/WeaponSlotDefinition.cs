using System;
using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Một loại súng trong Loadout — chỉnh trên Inspector (T8.6), không hardcode tên/prefab trong code.
    /// </summary>
    [Serializable]
    public class WeaponSlotDefinition
    {
        [SerializeField, Tooltip("Tên hiển thị trên panel tay phải.")]
        string displayName;

        [SerializeField, Tooltip("Prefab mesh low-poly; nên gán đủ 3 loại khác nhau.")]
        GameObject weaponPrefab;

        [SerializeField, Tooltip("Chỉ dùng khi Weapon Prefab trống (test nhanh).")]
        WeaponFallbackSilhouette fallbackSilhouette = WeaponFallbackSilhouette.ShortBlaster;

        /// <summary>Tên UI; fallback tên prefab hoặc silhouette.</summary>
        public string DisplayName => displayName;

        public GameObject WeaponPrefab => weaponPrefab;

        public WeaponFallbackSilhouette FallbackSilhouette => fallbackSilhouette;

        /// <summary>
        /// Nhãn an toàn khi sinh viên chưa điền Display Name.
        /// </summary>
        public string ResolveDisplayName(int slotIndex)
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName.Trim();

            if (weaponPrefab != null)
                return weaponPrefab.name;

            return $"Weapon {slotIndex + 1}";
        }
    }
}
