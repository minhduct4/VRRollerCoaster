using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace VRRoller
{
    /// <summary>
    /// Bia bắn: billboard (luôn hướng về camera khi chưa bị bắn),
    /// khi trúng đạn → ngã ra sau bằng Rigidbody → cộng điểm → hiện +N → tự destroy.
    /// Khi Cart đi qua quá xa, TargetSpawner tự Destroy object này.
    /// </summary>
    public class ShootingTarget : MonoBehaviour
    {
        // ── Billboard ────────────────────────────────────────────────────────────
        [Header("Billboard")]
        [SerializeField, Tooltip("Transform cần xoay để nhìn về camera. Rỗng = dùng root của prefab.")]
        Transform billboardRoot;

        [SerializeField, Tooltip("Chỉ xoay quanh trục Y (cylindrical). Tắt = quay full về camera.")]
        bool yAxisOnly = true;

        // ── Score ─────────────────────────────────────────────────────────────────
        [Header("Score")]
        [SerializeField, Range(1, 1000), Tooltip("Điểm thưởng khi bia bị bắn.")]
        int scoreValue = 1;

        // ── Floating popup ────────────────────────────────────────────────────────
        [Header("Floating +N popup")]
        [SerializeField, Tooltip("Prefab chứa FloatingScorePopup (TMP 3D text nổi lên). Rỗng = không hiện.")]
        GameObject floatingPopupPrefab;

        [SerializeField, Tooltip("Offset vị trí spawn popup so với tâm bia.")]
        Vector3 popupOffset = new Vector3(0f, 0.5f, 0f);

        // ── Fall effect ───────────────────────────────────────────────────────────
        [Header("Hiệu ứng ngã ra sau")]
        [SerializeField, Range(0f, 5f), Tooltip("Delay destroy sau khi ngã (giây). Đủ để bia rơi xuống đất).")]
        float destroyDelay = 1.8f;

        [SerializeField, Range(0f, 20f), Tooltip("Lực đẩy ra sau khi trúng đạn (Impulse).")]
        float fallBackForce = 3f;

        [SerializeField, Range(0f, 20f), Tooltip("Lực xoáy (torque) để bia nghiêng ngã ra sau.")]
        float fallTorque = 5f;

        [SerializeField, Tooltip("Khối lượng Rigidbody tạm thời khi ngã (kg). Nhỏ = ngã nhanh hơn.")]
        float fallMass = 1f;

        // ── Events ────────────────────────────────────────────────────────────────
        [Header("Events (tùy chọn)")]
        [SerializeField, Tooltip("UnityEvent khi bia trúng đạn — nối thêm logic tùy ý.")]
        UnityEvent onHit;

        // ── Runtime ───────────────────────────────────────────────────────────────
        bool hasBeenHit;
        Camera mainCam;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Awake()
        {
            if (billboardRoot == null)
                billboardRoot = transform;
        }

        void Start()
        {
            mainCam = Camera.main;
        }

        /// <summary>
        /// Billboard chạy cuối frame. Dừng ngay khi bia đã bị bắn (Rigidbody điều khiển rotation).
        /// </summary>
        void LateUpdate()
        {
            if (!hasBeenHit)
                ApplyBillboard();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// BulletController gọi khi đạn trúng. Chỉ xử lý lần đầu tiên.
        /// </summary>
        public void OnBulletHit()
        {
            if (hasBeenHit) return;
            hasBeenHit = true;

            // Cộng điểm
            ScoreManager.Instance?.AddScore(scoreValue);

            // Hiện popup +N
            ShowFloatingPopup();

            // UnityEvent tùy chỉnh
            onHit?.Invoke();

            // Hiệu ứng ngã ra sau
            StartCoroutine(FallBackwards());

            // Tự destroy sau khi ngã xong
            Destroy(gameObject, destroyDelay);
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Thêm Rigidbody (nếu chưa có), áp lực đẩy + torque để bia ngã về phía sau (ra xa camera).
        /// Billboard đã tắt ở LateUpdate → rotation vật lý tự nhiên.
        /// </summary>
        IEnumerator FallBackwards()
        {
            // Chờ 1 frame để billboard kịp tắt, tránh snap rotation
            yield return null;

            var rb = GetComponent<Rigidbody>();
            bool addedRb = false;
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                addedRb = true;
            }

            rb.isKinematic = false;
            rb.useGravity  = true;
            rb.mass        = fallMass;

            if (addedRb)
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;

            // Hướng "ra sau" = chiều bia đang nhìn (forward của bia trỏ về camera,
            // nên backward = -forward = ra xa camera = ngã ra sau)
            var backDir   = -transform.forward;
            var torqueDir =  transform.right;   // xoay quanh trục right → bia nghiêng ra sau

            rb.AddForce(backDir  * fallBackForce, ForceMode.Impulse);
            rb.AddTorque(torqueDir * fallTorque, ForceMode.Impulse);
        }

        /// <summary>
        /// Xoay billboardRoot về phía camera (Y-only hoặc full 3D).
        /// </summary>
        void ApplyBillboard()
        {
            if (mainCam == null || billboardRoot == null) return;

            var toCamera = mainCam.transform.position - billboardRoot.position;

            if (yAxisOnly)
                toCamera.y = 0f;

            if (toCamera.sqrMagnitude < 0.0001f) return;
            billboardRoot.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        /// <summary>
        /// Spawn FloatingScorePopup tại vị trí bia + popupOffset.
        /// </summary>
        void ShowFloatingPopup()
        {
            if (floatingPopupPrefab == null) return;

            var spawnPos = transform.position + popupOffset;
            var go = Instantiate(floatingPopupPrefab, spawnPos, Quaternion.identity);
            go.GetComponent<FloatingScorePopup>()?.Play($"+{scoreValue}");
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + popupOffset, 0.1f);
            // Hiển thị hướng ngã
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position, -transform.forward * 1.5f);
        }
#endif
    }
}
