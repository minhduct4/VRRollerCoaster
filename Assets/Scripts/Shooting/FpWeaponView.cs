using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Hiển thị model súng trước mặt camera ở chế độ First-Person (kiểu FPS).
    /// Gắn vào FP Camera (không phải Cart). Súng là child của camera nên tự di chuyển cùng góc nhìn.
    /// Tự đồng bộ loại súng với VrHandWeaponLoadout khi người dùng đổi súng.
    /// </summary>
    [DisallowMultipleComponent]
    public class FpWeaponView : MonoBehaviour
    {
        // ── Weapon prefabs ────────────────────────────────────────────────────────
        [Header("Weapon Prefabs (khớp thứ tự với VrHandWeaponLoadout)")]
        [SerializeField, Tooltip("Danh sách prefab súng hiển thị FP (3 slot, khớp thứ tự với VR loadout).")]
        GameObject[] fpWeaponPrefabs = new GameObject[3];

        [SerializeField, Range(0, 2), Tooltip("Slot mặc định khi khởi động.")]
        int defaultWeaponIndex = 0;

        // ── Vị trí súng trong view ────────────────────────────────────────────────
        [Header("Grip offset (local so với FP Camera)")]
        [SerializeField, Tooltip("Vị trí local súng. X+: phải, Y-: xuống, Z+: ra trước.")]
        Vector3 gripLocalPosition = new Vector3(0.18f, -0.22f, 0.35f);

        [SerializeField, Tooltip("Góc xoay local súng (Euler degrees).")]
        Vector3 gripLocalEuler = new Vector3(0f, 0f, 0f);

        [SerializeField, Range(0.1f, 3f), Tooltip("Scale súng trong FP view.")]
        float gripScale = 1f;

        // ── Muzzle ────────────────────────────────────────────────────────────────
        [Header("Muzzle")]
        [SerializeField, Tooltip("Tên child 'Muzzle' trong prefab súng để GunShooter lấy điểm bắn.")]
        string muzzleChildName = "Muzzle";

        // ── Sync với VR loadout ───────────────────────────────────────────────────
        [Header("Sync VR Loadout (tùy chọn)")]
        [SerializeField, Tooltip("Để trống = tự tìm VrHandWeaponLoadout trong scene.")]
        VrHandWeaponLoadout vrLoadout;

        // ── Runtime ───────────────────────────────────────────────────────────────
        GameObject weaponInstance;
        int currentIndex = -1;

        /// <summary>
        /// Muzzle point để GunShooter dùng thay cho fpShootOrigin.
        /// Trả về transform của child 'Muzzle' nếu có, hoặc root súng.
        /// </summary>
        public Transform MuzzleTransform
        {
            get
            {
                if (weaponInstance == null) return transform;
                var muzzle = weaponInstance.transform.Find(muzzleChildName);
                return muzzle != null ? muzzle : weaponInstance.transform;
            }
        }

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Start()
        {
            // Tự tìm VR loadout để đồng bộ loại súng (không bắt buộc)
            if (vrLoadout == null)
                vrLoadout = FindFirstObjectByType<VrHandWeaponLoadout>();

            // Hiển thị súng mặc định
            SwitchWeapon(defaultWeaponIndex);
        }

        void Update()
        {
            // Đồng bộ loại súng với VR loadout khi index thay đổi
            if (vrLoadout != null && vrLoadout.CurrentWeaponIndex != currentIndex)
                SwitchWeapon(vrLoadout.CurrentWeaponIndex);
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Đổi sang slot súng chỉ định và cập nhật model.
        /// </summary>
        public void SwitchWeapon(int index)
        {
            var clamped = Mathf.Clamp(index, 0, Mathf.Max(0, fpWeaponPrefabs.Length - 1));
            if (clamped == currentIndex && weaponInstance != null) return;

            currentIndex = clamped;
            SpawnWeapon(clamped);
        }

        // ── Private ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Destroy instance cũ, spawn prefab mới làm child của FP Camera với grip offset.
        /// </summary>
        void SpawnWeapon(int index)
        {
            if (weaponInstance != null)
                Destroy(weaponInstance);

            // Nếu không có prefab cho slot này → ẩn súng
            if (fpWeaponPrefabs == null || index >= fpWeaponPrefabs.Length || fpWeaponPrefabs[index] == null)
            {
                weaponInstance = null;
                return;
            }

            weaponInstance = Instantiate(fpWeaponPrefabs[index], transform);
            weaponInstance.name = "FP_Weapon";

            var t = weaponInstance.transform;
            t.localPosition = gripLocalPosition;
            t.localRotation = Quaternion.Euler(gripLocalEuler);
            t.localScale    = Vector3.one * gripScale;

            // Xóa collider trên súng để không chặn raycast/đạn
            VrWeaponRuntimeMesh.StripColliders(t);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (fpWeaponPrefabs == null || fpWeaponPrefabs.Length != 3)
            {
                var arr = new GameObject[3];
                if (fpWeaponPrefabs != null)
                    for (int i = 0; i < Mathf.Min(fpWeaponPrefabs.Length, 3); i++)
                        arr[i] = fpWeaponPrefabs[i];
                fpWeaponPrefabs = arr;
            }

            defaultWeaponIndex = Mathf.Clamp(defaultWeaponIndex, 0, 2);
            gripScale = Mathf.Max(0.01f, gripScale);
        }

        void OnDrawGizmosSelected()
        {
            // Vẽ hướng muzzle để dễ căn chỉnh trong Editor
            var muzzleWorld = transform.TransformPoint(gripLocalPosition);
            var forward     = transform.TransformDirection(Quaternion.Euler(gripLocalEuler) * Vector3.forward);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(muzzleWorld, muzzleWorld + forward * 0.5f);
            Gizmos.DrawWireSphere(muzzleWorld, 0.02f);
        }
#endif
    }
}
