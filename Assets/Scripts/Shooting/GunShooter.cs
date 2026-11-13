using UnityEngine;
using UnityEngine.InputSystem;

namespace VRRoller
{
    /// <summary>
    /// Bắn đạn từ 2 súng VR (tay trái / tay phải).
    /// Hỗ trợ cả XR trigger, XR Device Simulator, và chuột First-Person.
    /// </summary>
    [DisallowMultipleComponent]
    public class GunShooter : MonoBehaviour
    {
        // Tên weapon instance được VrHandWeaponLoadout spawn dưới anchor tay
        const string WeaponLeftName  = "Weapon_L";
        const string WeaponRightName = "Weapon_R";
        // Tên child muzzle theo convention (user đặt trong prefab súng)
        const string MuzzleChildName = "Muzzle";

        // ── References ──────────────────────────────────────────────────────────
        [Header("References — để trống sẽ tự tìm VrHandControllerRig trong scene")]
        [SerializeField, Tooltip("Anchor tay trái (VrHandControllerAnchor).")]
        Transform leftHandAnchor;

        [SerializeField, Tooltip("Anchor tay phải (VrHandControllerAnchor).")]
        Transform rightHandAnchor;

        [Header("Bullet")]
        [SerializeField, Tooltip("Prefab viên đạn (cần có BulletController).")]
        GameObject bulletPrefab;

        [SerializeField, Range(5f, 100f), Tooltip("Tốc độ đạn (m/s).")]
        float bulletSpeed = 40f;

        [SerializeField, Range(0.5f, 10f), Tooltip("Thời gian sống đạn (giây) nếu không trúng.")]
        float bulletLifetime = 3f;

        // ── Fire rate ────────────────────────────────────────────────────────────
        [Header("Fire rate")]
        [SerializeField, Range(0.05f, 2f), Tooltip("Cooldown giữa 2 phát bắn mỗi tay (giây). Nhỏ hơn = bắn nhanh hơn.")]
        float fireCooldown = 0.25f;

        [SerializeField, Tooltip("Giữ trigger/chuột = bắn liên tục. Tắt = chỉ bắn một phát mỗi lần nhấn.")]
        bool continuousFire = true;

        // ── First-Person fallback ────────────────────────────────────────────────
        [Header("First-Person fallback")]
        [SerializeField, Tooltip("Điểm bắn khi không có tay VR (thường là Main Camera). Rỗng = tự dùng Camera.main.")]
        Transform fpShootOrigin;

        [SerializeField, Tooltip("Bắt đầu bắn từ First-Person (mouse left/right). Tắt nếu chỉ muốn bắn trên VR/Sim.")]
        bool enableFpShooting = true;

        // ── Muzzle offset ────────────────────────────────────────────────────────
        [Header("Muzzle offset (nếu prefab súng không có child 'Muzzle')")]
        [SerializeField, Tooltip("Offset tính từ gốc Transform súng (local). Z dương = về phía trước súng.")]
        Vector3 fallbackMuzzleLocalOffset = new Vector3(0f, 0f, 0.3f);

        // ── Runtime state ────────────────────────────────────────────────────────
        InputAction leftTriggerAction;
        InputAction rightTriggerAction;
        float leftCooldown;
        float rightCooldown;
        FpWeaponView fpWeaponView; // tự tìm lúc Start — muzzle FP chính xác hơn fpShootOrigin

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Start()
        {
            AutoFindHandAnchors();
            fpWeaponView = FindFirstObjectByType<FpWeaponView>();
            BindInputActions();
        }

        void OnEnable()
        {
            leftTriggerAction?.Enable();
            rightTriggerAction?.Enable();
        }

        void OnDisable()
        {
            leftTriggerAction?.Disable();
            rightTriggerAction?.Disable();
        }

        void OnDestroy()
        {
            leftTriggerAction?.Dispose();
            rightTriggerAction?.Dispose();
        }

        void Update()
        {
            leftCooldown  -= Time.deltaTime;
            rightCooldown -= Time.deltaTime;

            CheckAndFire(leftTriggerAction,  ref leftCooldown,  leftHandAnchor,  WeaponLeftName);
            CheckAndFire(rightTriggerAction, ref rightCooldown, rightHandAnchor, WeaponRightName);
        }

        // ── Internal ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Kiểm tra input và bắn nếu hết cooldown.
        /// </summary>
        void CheckAndFire(InputAction action, ref float cooldown, Transform anchor, string weaponName)
        {
            if (action == null || bulletPrefab == null) return;

            bool triggered = continuousFire ? action.IsPressed() : action.WasPressedThisFrame();
            if (!triggered || cooldown > 0f) return;

            cooldown = fireCooldown;
            FireFromHand(anchor, weaponName);
        }

        /// <summary>
        /// Tính điểm và hướng muzzle rồi spawn đạn.
        /// </summary>
        void FireFromHand(Transform anchor, string weaponName)
        {
            var (pos, dir) = ResolveMuzzlePose(anchor, weaponName);
            SpawnBullet(pos, dir);
        }

        /// <summary>
        /// Tìm muzzle theo thứ tự ưu tiên:
        /// 1) Child "Muzzle" trong weapon prefab.
        /// 2) fallbackMuzzleLocalOffset tính từ gốc weapon.
        /// 3) Anchor tay trực tiếp.
        /// 4) FP shoot origin hoặc Camera.main.
        /// </summary>
        (Vector3 position, Vector3 direction) ResolveMuzzlePose(Transform anchor, string weaponName)
        {
            if (anchor != null)
            {
                var weapon = anchor.Find(weaponName);
                if (weapon != null)
                {
                    // Ưu tiên child "Muzzle" trong prefab súng
                    var muzzle = weapon.Find(MuzzleChildName);
                    if (muzzle != null)
                        return (muzzle.position, muzzle.forward);

                    // Fallback: dùng forward + offset từ gốc súng
                    var muzzlePos = weapon.TransformPoint(fallbackMuzzleLocalOffset);
                    return (muzzlePos, weapon.forward);
                }

                // Súng chưa được spawn (VrHandWeaponLoadout chưa chạy)
                return (anchor.position, anchor.forward);
            }

            // First-Person: bắn từ muzzle của FpWeaponView nếu có, fallback = camera
            if (!enableFpShooting)
                return (Vector3.zero, Vector3.zero);

            // Ưu tiên muzzle của FpWeaponView (đầu nòng súng FP)
            if (fpWeaponView != null)
            {
                var fpMuzzle = fpWeaponView.MuzzleTransform;
                return (fpMuzzle.position, fpMuzzle.forward);
            }

            if (fpShootOrigin != null)
                return (fpShootOrigin.position, fpShootOrigin.forward);

            var cam = Camera.main;
            return cam != null
                ? (cam.transform.position, cam.transform.forward)
                : (Vector3.zero, Vector3.forward);
        }

        /// <summary>Instantiate đạn và khởi tạo velocity + lifetime.</summary>
        void SpawnBullet(Vector3 position, Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.001f) return;

            var go = Instantiate(bulletPrefab, position, Quaternion.LookRotation(direction));
            var bullet = go.GetComponent<BulletController>();
            if (bullet != null)
                bullet.Initialize(direction.normalized * bulletSpeed, bulletLifetime);
            else
                Destroy(go, bulletLifetime);
        }

        /// <summary>
        /// Bind trigger XR tay trái/phải. Thêm fallback bàn phím cho First-Person và Simulator.
        /// Trái: trigger trái + Q. Phải: trigger phải + mouse left.
        /// </summary>
        void BindInputActions()
        {
            leftTriggerAction?.Dispose();
            rightTriggerAction?.Dispose();

            leftTriggerAction = new InputAction("ShootLeft", InputActionType.Button);
            leftTriggerAction.AddBinding("<XRController>{LeftHand}/trigger");
            leftTriggerAction.AddBinding("<Keyboard>/q");

            rightTriggerAction = new InputAction("ShootRight", InputActionType.Button);
            rightTriggerAction.AddBinding("<XRController>{RightHand}/trigger");
            rightTriggerAction.AddBinding("<Mouse>/leftButton");

            leftTriggerAction.Enable();
            rightTriggerAction.Enable();
        }

        /// <summary>
        /// Nếu user không gán anchor trên Inspector, tự tìm VrHandControllerRig trong scene.
        /// </summary>
        void AutoFindHandAnchors()
        {
            if (leftHandAnchor != null && rightHandAnchor != null) return;

            var handRig = FindFirstObjectByType<VrHandControllerRig>();
            if (handRig == null)
            {
                Debug.LogWarning("GunShooter: không tìm thấy VrHandControllerRig. Gán tay thủ công.", this);
                return;
            }

            if (leftHandAnchor == null)  leftHandAnchor  = handRig.LeftHandAnchor;
            if (rightHandAnchor == null) rightHandAnchor = handRig.RightHandAnchor;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (maxSpeed > 0f && bulletSpeed > maxSpeed)
                bulletSpeed = maxSpeed;

            fireCooldown = Mathf.Max(0.05f, fireCooldown);
        }

        // Dùng để tránh warning "maxSpeed không tồn tại"
        const float maxSpeed = 150f;
#endif
    }
}
