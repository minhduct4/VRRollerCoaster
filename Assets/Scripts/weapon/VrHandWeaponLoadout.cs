using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Gắn súng lên anchor tay; 3 slot cấu hình Inspector (T8.6).
    /// </summary>
    [DefaultExecutionOrder(50)]
    [DisallowMultipleComponent]
    public class VrHandWeaponLoadout : MonoBehaviour
    {
        const int WeaponSlotCount = 3;

        [Header("3 loại súng (T8.6)")]
        [SerializeField, Tooltip("Đúng 3 phần tử: prefab + tên hiển thị mỗi loại.")]
        WeaponSlotDefinition[] weaponSlots = new WeaponSlotDefinition[WeaponSlotCount];

        [SerializeField, Range(0, 2), Tooltip("Loại súng khi bắt đầu Play.")]
        int defaultWeaponIndex;

        [Header("Grip (local so với anchor tay)")]
        [SerializeField, Tooltip("Offset súng tay trái.")]
        Vector3 leftGripLocalPosition = new Vector3(0f, 0f, 0.1f);

        [SerializeField]
        Vector3 leftGripLocalEuler = new Vector3(0f, 0f, 0f);

        [SerializeField, Tooltip("Offset súng tay phải.")]
        Vector3 rightGripLocalPosition = new Vector3(0f, 0f, 0.1f);

        [SerializeField]
        Vector3 rightGripLocalEuler = new Vector3(0f, 0f, 0f);

        [Header("References — tự tìm nếu trống")]
        [SerializeField]
        VrHandControllerRig handRig;

        Transform leftWeaponInstance;
        Transform rightWeaponInstance;
        int currentWeaponIndex = -1;

        /// <summary>Loại súng đang gắn (T8.4 đọc/ghi).</summary>
        public int CurrentWeaponIndex => currentWeaponIndex;

        /// <summary>Số slot súng (luôn 3 cho MVP).</summary>
        public int SlotCount => WeaponSlotCount;

        void Start()
        {
            if (handRig == null)
                handRig = GetComponent<VrHandControllerRig>();

            if (handRig == null)
            {
                Debug.LogWarning("VrHandWeaponLoadout: thiếu VrHandControllerRig.", this);
                return;
            }

            handRig.EnsureHandHierarchy();
            EquipWeaponIndex(defaultWeaponIndex);
        }

        void LateUpdate()
        {
            SyncWeaponVisibility(HandSide.Left, leftWeaponInstance);
            SyncWeaponVisibility(HandSide.Right, rightWeaponInstance);
        }

        /// <summary>
        /// Tên trên panel UI (T8.3) — từ slot Inspector.
        /// </summary>
        public string GetDisplayName(int index)
        {
            var slot = GetSlot(index);
            return slot == null
                ? $"Weapon {Mathf.Clamp(index, 0, WeaponSlotCount - 1) + 1}"
                : slot.ResolveDisplayName(Mathf.Clamp(index, 0, WeaponSlotCount - 1));
        }

        /// <summary>
        /// Đổi loại súng cả hai tay (T8.4 gọi).
        /// </summary>
        public void EquipWeaponIndex(int index)
        {
            var clamped = Mathf.Clamp(index, 0, WeaponSlotCount - 1);
            currentWeaponIndex = clamped;

            leftWeaponInstance = SpawnOnHand(
                handRig.LeftHandAnchor,
                leftWeaponInstance,
                clamped,
                leftGripLocalPosition,
                leftGripLocalEuler,
                "Weapon_L");

            rightWeaponInstance = SpawnOnHand(
                handRig.RightHandAnchor,
                rightWeaponInstance,
                clamped,
                rightGripLocalPosition,
                rightGripLocalEuler,
                "Weapon_R");
        }

        /// <summary>
        /// Xoay vòng 3 loại súng.
        /// </summary>
        public void CycleWeaponNext()
        {
            var next = (Mathf.Max(0, currentWeaponIndex) + 1) % WeaponSlotCount;
            EquipWeaponIndex(next);
        }

        WeaponSlotDefinition GetSlot(int index)
        {
            if (weaponSlots == null || index < 0 || index >= weaponSlots.Length)
                return null;

            return weaponSlots[index];
        }

        /// <summary>
        /// Hủy instance cũ, spawn prefab hoặc fallback theo slot.
        /// </summary>
        Transform SpawnOnHand(
            Transform anchor,
            Transform previousInstance,
            int slotIndex,
            Vector3 gripLocalPosition,
            Vector3 gripLocalEuler,
            string instanceName)
        {
            if (anchor == null)
                return previousInstance;

            if (previousInstance != null)
                Destroy(previousInstance.gameObject);

            var slot = GetSlot(slotIndex);
            GameObject instance;
            if (slot != null && slot.WeaponPrefab != null)
            {
                instance = Instantiate(slot.WeaponPrefab, anchor);
                instance.name = instanceName;
            }
            else
            {
                var silhouette = slot != null
                    ? slot.FallbackSilhouette
                    : (WeaponFallbackSilhouette)(slotIndex % WeaponSlotCount);
                instance = VrWeaponRuntimeMesh.CreateFallbackWeapon(instanceName, silhouette);
                instance.transform.SetParent(anchor, false);
            }

            var t = instance.transform;
            t.localPosition = gripLocalPosition;
            t.localRotation = Quaternion.Euler(gripLocalEuler);
            VrWeaponRuntimeMesh.StripColliders(t);
            return t;
        }

        /// <summary>
        /// Ẩn súng khi tay chưa tracked — cùng nguồn với mesh controller (Input System / simulator).
        /// </summary>
        void SyncWeaponVisibility(HandSide side, Transform weaponRoot)
        {
            if (weaponRoot == null || handRig == null)
                return;

            weaponRoot.gameObject.SetActive(handRig.IsHandTracked(side));
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (weaponSlots == null || weaponSlots.Length != WeaponSlotCount)
            {
                var resized = new WeaponSlotDefinition[WeaponSlotCount];
                if (weaponSlots != null)
                {
                    for (var i = 0; i < Mathf.Min(weaponSlots.Length, WeaponSlotCount); i++)
                        resized[i] = weaponSlots[i];
                }

                for (var i = 0; i < WeaponSlotCount; i++)
                    resized[i] ??= new WeaponSlotDefinition();

                weaponSlots = resized;
            }

            defaultWeaponIndex = Mathf.Clamp(defaultWeaponIndex, 0, WeaponSlotCount - 1);
        }
#endif
    }
}
