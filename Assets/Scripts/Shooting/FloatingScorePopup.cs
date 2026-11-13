using System.Collections;
using TMPro;
using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Popup "+N" nổi lên trên bia bắn, tự billboard về camera rồi biến mất.
    /// Gắn vào prefab popup (cần có TMP_Text hoặc TextMesh).
    /// </summary>
    public class FloatingScorePopup : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────────────────────────
        [Header("Text")]
        [SerializeField, Tooltip("TextMeshPro 3D (không phải UGUI). Tự tìm nếu trống.")]
        TMP_Text label;

        [Header("Animation")]
        [SerializeField, Range(0.1f, 3f), Tooltip("Tổng thời gian hiển thị và bay lên (giây).")]
        float duration = 1.2f;

        [SerializeField, Range(0f, 5f), Tooltip("Tốc độ bay lên (m/s).")]
        float riseSpeed = 1.5f;

        [SerializeField, Tooltip("Scale ban đầu khi pop ra.")]
        float startScale = 0.6f;

        [SerializeField, Tooltip("Scale lớn nhất (giữa chừng).")]
        float peakScale  = 1.2f;

        [Header("Color")]
        [SerializeField, Tooltip("Màu text ban đầu.")]
        Color startColor = Color.yellow;

        [SerializeField, Tooltip("Màu text lúc biến mất (alpha = 0).")]
        Color endColor   = new Color(1f, 1f, 0f, 0f);

        // ── Runtime ───────────────────────────────────────────────────────────────
        Camera mainCam;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Awake()
        {
            if (label == null)
                label = GetComponentInChildren<TMP_Text>();
        }

        void Start()
        {
            mainCam = Camera.main;
        }

        /// <summary>
        /// Gọi ngay sau Instantiate để đặt text và bắt đầu animation.
        /// </summary>
        public void Play(string text)
        {
            if (label != null)
            {
                label.text  = text;
                label.color = startColor;
            }

            transform.localScale = Vector3.one * startScale;
            StartCoroutine(AnimateRoutine());
        }

        void LateUpdate()
        {
            // Billboard: hướng về camera (giống ShootingTarget)
            if (mainCam == null) return;
            var toCamera = mainCam.transform.position - transform.position;
            if (toCamera.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
        }

        // ── Coroutine ─────────────────────────────────────────────────────────────

        /// <summary>Bay lên + phóng to → thu nhỏ + mờ dần → destroy.</summary>
        IEnumerator AnimateRoutine()
        {
            float elapsed = 0f;
            var startPos = transform.position;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration; // 0 → 1

                // Bay lên tuyến tính
                transform.position = startPos + Vector3.up * (riseSpeed * elapsed);

                // Scale: pop ra lớn rồi nhỏ dần
                float scaleCurve = t < 0.3f
                    ? Mathf.Lerp(startScale, peakScale, t / 0.3f)
                    : Mathf.Lerp(peakScale,  0.1f,     (t - 0.3f) / 0.7f);
                transform.localScale = Vector3.one * scaleCurve;

                // Fade out nửa sau
                if (label != null)
                    label.color = Color.Lerp(startColor, endColor, Mathf.SmoothStep(0f, 1f, t));

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
