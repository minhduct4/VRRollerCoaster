using UnityEngine;
using UnityEngine.Splines;

namespace VRRoller
{
    /// <summary>
    /// Snapshot **T5.1–T5.3** (+ T2.2–T2.4): Cart **đứng ga** đến khi Start; Reset về ga.
    /// Dùng chung helper tốc độ/spline Phase 2 (<see cref="CartSpeed"/>).
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("VRRoller/Teaching/Phase 5 — Cart Mover (T5 Start/Reset)")]
    public class CartMover : MonoBehaviour
    {
        const float MinTrackLength = 0.001f;

        [Header("Track")]
        [SerializeField, Tooltip("Object Track trong scene Main (Spline Container).")]
        SplineContainer track;

        [Header("Speed (m/s) — chỉnh Inspector, không hardcode trong logic")]
        [SerializeField, Range(1f, 30f), Tooltip("Tốc trên đoạn gần ngang.")]
        float cruiseSpeed = 8f;

        [SerializeField, Range(0.5f, 20f), Tooltip("Sàn tốc — không dừng hẳn khi leo dốc.")]
        float minSpeed = 2f;

        [SerializeField, Range(2f, 40f), Tooltip("Trần tốc khi xuống dốc.")]
        float maxSpeed = 16f;

        [SerializeField, Range(0f, 25f), Tooltip("Càng lớn càng chênh tốc lên/xuống dốc.")]
        float slopeInfluence = 10f;

        float normalizedTime;
        float trackLengthMeters;
        bool isRunning;
        float currentSpeedMetersPerSecond;

        /// <summary>Toa đang chạy dọc ray (sau Start).</summary>
        public bool IsRunning => isRunning;

        /// <summary>Tốc độ hiện tại của toa, tính bằng mét trên giây.</summary>
        public float CurrentSpeedMetersPerSecond => currentSpeedMetersPerSecond;
        public float NormalizedTime => normalizedTime;
        void Start()
        {
            if (!TryRefreshTrackLength())
                return;

            ResetToStartAndWait();
        }

        void Update()
        {
            if (!TryRefreshTrackLength())
                return;

            if (!isRunning)
            {
                currentSpeedMetersPerSecond = 0f;
                return;
            }

            if (CartSplinePose.TryGetForward(track, trackLengthMeters, normalizedTime, out var forward))
            {
                currentSpeedMetersPerSecond = CartSpeed.MetersPerSecond(
                    cruiseSpeed, minSpeed, maxSpeed, slopeInfluence, forward.y);
                normalizedTime = CartSpeed.AdvanceNormalizedTime(
                    normalizedTime, currentSpeedMetersPerSecond, trackLengthMeters, Time.deltaTime);
            }

            normalizedTime = CartSpeed.WrapOrClampTime(normalizedTime, track.Spline.Closed);
            SyncTransformToSpline(normalizedTime);
        }

        /// <summary>
        /// Start (T5.2): chạy ray nếu đang chờ ở ga.
        /// </summary>
        public bool TryBeginRide()
        {
            if (!CartRideLifecycle.TryBeginRide(ref isRunning))
                return false;

            Debug.Log("Roller (Teaching): ride started.");
            return true;
        }

        /// <summary>
        /// Reset (T5.3): về t = 0, dừng, chờ Start.
        /// </summary>
        public void ResetToStartAndWait()
        {
            CartRideLifecycle.EnterWaitingAtStart(ref normalizedTime, ref isRunning);
            currentSpeedMetersPerSecond = 0f;

            if (TryRefreshTrackLength())
                SyncTransformToSpline(normalizedTime);
        }

        /// <summary>
        /// Kiểm tra Track đã gán và cache chiều dài ray (mét).
        /// </summary>
        bool TryRefreshTrackLength()
        {
            if (track == null)
            {
                Debug.LogError($"{nameof(CartMover)}: gán Track (Spline Container).", this);
                return false;
            }

            trackLengthMeters = track.CalculateLength();
            if (trackLengthMeters < MinTrackLength)
            {
                Debug.LogError($"{nameof(CartMover)}: spline quá ngắn hoặc chưa có knot.", this);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Chỉ đặt transform Cart — camera VR là con XR Origin (T4.1).
        /// </summary>
        void SyncTransformToSpline(float t)
        {
            if (!CartSplinePose.TryGetWorldPose(track, trackLengthMeters, t, out var position, out var rotation))
                return;

            transform.SetPositionAndRotation(position, rotation);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Khi chỉnh slider Inspector, giữ min ≤ cruise ≤ max.
        /// </summary>
        void OnValidate()
        {
            if (maxSpeed < minSpeed)
                maxSpeed = minSpeed;
            if (cruiseSpeed < minSpeed)
                cruiseSpeed = minSpeed;
            if (cruiseSpeed > maxSpeed)
                cruiseSpeed = maxSpeed;
        }
#endif
    }
}
