using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

namespace VRRoller
{
    /// <summary>
    /// Snapshot T2.2: lấy pose toa trên Spline Container.
    /// </summary>
    public static class CartSplinePose
    {
        const float MinVectorSqr = 0.0001f;
        const float ForwardLookAheadMeters = 0.25f;

        /// <summary>
        /// Hướng tiến tại tham số t: ưu tiên tiếp tuyến spline; nếu bằng 0 thì lấy delta vị trí phía trước.
        /// </summary>
        public static bool TryGetForward(
            SplineContainer track,
            float trackLengthMeters,
            float t,
            out Vector3 forward)
        {
            forward = default;
            if (track == null || trackLengthMeters <= 0f)
                return false;

            track.Evaluate(t, out _, out float3 tangent, out _);
            forward = ((Vector3)tangent).normalized;
            if (forward.sqrMagnitude > MinVectorSqr)
                return true;

            var step = ForwardLookAheadMeters / trackLengthMeters;
            var tAhead = track.Spline.Closed
                ? Mathf.Repeat(t + step, 1f)
                : Mathf.Min(t + step, 1f);

            var pos = (Vector3)track.EvaluatePosition(t);
            var ahead = (Vector3)track.EvaluatePosition(tAhead);
            forward = (ahead - pos).normalized;
            return forward.sqrMagnitude > MinVectorSqr;
        }

        /// <summary>
        /// Vị trí world và rotation nhìn theo tiếp tuyến, up lấy từ EvaluateUpVector của spline.
        /// </summary>
        public static bool TryGetWorldPose(
            SplineContainer track,
            float trackLengthMeters,
            float t,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = default;
            rotation = default;

            if (!TryGetForward(track, trackLengthMeters, t, out var forward))
                return false;

            position = (Vector3)track.EvaluatePosition(t);
            var up = ((Vector3)track.EvaluateUpVector(t)).normalized;
            if (up.sqrMagnitude < MinVectorSqr)
                up = Vector3.up;

            rotation = Quaternion.LookRotation(forward, up);
            return true;
        }
    }
}
