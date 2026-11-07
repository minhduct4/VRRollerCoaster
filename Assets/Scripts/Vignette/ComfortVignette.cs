using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRRoller
{
    /// <summary>
    /// Vignette URP co/giãn theo vận tốc Cart — chỉ khi PlayMode VR (Quest / Simulation). Tắt ở First-Person (T6.3).
    /// </summary>
    [DisallowMultipleComponent]
    public class ComfortVignette : MonoBehaviour
    {
        [Header("Volume URP")]
        [SerializeField, Tooltip("Global hoặc Local Volume dùng ComfortVignetteProfile.")]
        Volume comfortVolume;

        [Header("Nguồn tốc độ")]
        [SerializeField, Tooltip("Cart Mover trên cùng toa hoặc kéo Cart.")]
        CartMover cartMover;

        [Header("Ngưỡng tốc (m/s) — T6.2")]
        [SerializeField, Range(0f, 20f), Tooltip("Dưới ngưỡng này vignette = Min Intensity (ga / chậm).")]
        float speedAtMinIntensity = 2f;

        [SerializeField, Range(2f, 40f), Tooltip("Từ ngưỡng này trở lên vignette = Max Intensity.")]
        float speedAtMaxIntensity = 10f;

        [Header("Độ đậm vignette (0–1) — T6.2")]
        [SerializeField, Range(0f, 0.8f), Tooltip("Tốc thấp / đứng ga.")]
        float minIntensity = 0f;

        [SerializeField, Range(0f, 1f), Tooltip("Tốc cao (xuống dốc). URP cho phép ~0.6–0.65 rõ; Quest chỉnh thấp hơn nếu cần.")]
        float maxIntensity = 0.58f;

        [SerializeField, Range(0.3f, 1f), Tooltip("< 1 = vignette tăng mạnh hơn khi vừa tăng tốc (dễ thấy).")]
        float responseExponent = 0.65f;

        [SerializeField, Range(0.05f, 0.8f), Tooltip("Viền mềm — cao hơn = vùng tối rộng hơn.")]
        float vignetteSmoothness = 0.48f;

        [Header("Làm mượt")]
        [SerializeField, Range(0.02f, 0.5f), Tooltip("Giây — tránh nhấp nháy khi tốc đổi đột ngột.")]
        float intensitySmoothTime = 0.2f;

        Vignette m_vignette;
        float m_smoothedIntensity;
        float m_intensityVelocity;
        PlayModeKind m_lastMode = PlayModeKind.FirstPerson;

        void Awake()
        {
            if (cartMover == null)
                cartMover = GetComponent<CartMover>();

            if (!TryResolveVignette())
                enabled = false;
        }

        void LateUpdate()
        {
            if (m_vignette == null)
                return;

            var mode = ResolvePlayMode();
            if (mode == PlayModeKind.Vr)
                UrpCameraPostProcessing.EnableOnAllActiveCameras();
            if (mode != m_lastMode)
            {
                m_lastMode = mode;
                m_smoothedIntensity = mode == PlayModeKind.Vr ? m_smoothedIntensity : 0f;
                m_intensityVelocity = 0f;
            }

            var target = mode == PlayModeKind.Vr
                ? ComfortVignetteIntensity.FromSpeedMetersPerSecond(
                    cartMover != null ? cartMover.CurrentSpeedMetersPerSecond : 0f,
                    speedAtMinIntensity,
                    speedAtMaxIntensity,
                    minIntensity,
                    maxIntensity,
                    responseExponent)
                : 0f;

            m_smoothedIntensity = Mathf.SmoothDamp(
                m_smoothedIntensity,
                target,
                ref m_intensityVelocity,
                intensitySmoothTime);

            m_vignette.active = mode == PlayModeKind.Vr;
            m_vignette.intensity.Override(m_smoothedIntensity);
            m_vignette.smoothness.Override(vignetteSmoothness);
        }

        /// <summary>
        /// Mode Manager có thể chưa sẵn sàng frame đầu — mặc định First-Person (vignette tắt).
        /// </summary>
        PlayModeKind ResolvePlayMode()
        {
            var manager = ModeManager.Instance;
            return manager != null ? manager.PlayMode : PlayModeKind.FirstPerson;
        }

        /// <summary>
        /// Cache component Vignette trong profile của Volume.
        /// </summary>
        bool TryResolveVignette()
        {
            if (comfortVolume == null || comfortVolume.profile == null)
            {
                Debug.LogError($"{nameof(ComfortVignette)}: gán Volume + profile ComfortVignetteProfile.", this);
                return false;
            }

            if (comfortVolume.profile.TryGet(out m_vignette))
                return true;

            Debug.LogError($"{nameof(ComfortVignette)}: profile thiếu override Vignette.", this);
            return false;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (speedAtMaxIntensity < speedAtMinIntensity)
                speedAtMaxIntensity = speedAtMinIntensity;
            if (maxIntensity < minIntensity)
                maxIntensity = minIntensity;
        }
#endif
    }
}
