using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Snapshot T2.3–T2.4: công thức tốc độ theo độ dốc (hàm thuần).
    /// </summary>
    public static class CartSpeed
    {
        /// <summary>
        /// Tính tốc (m/s) từ hướng tiến tuyến: thành phần Y cho biết leo hay xuống dốc.
        /// </summary>
        /// <param name="forwardY">Y của vector tiến tuyến đã chuẩn hóa (world space).</param>
        public static float MetersPerSecond(
            float cruiseSpeed,
            float minSpeed,
            float maxSpeed,
            float slopeInfluence,
            float forwardY)
        {
            var speed = cruiseSpeed - slopeInfluence * forwardY;
            return Mathf.Clamp(speed, minSpeed, maxSpeed);
        }

        /// <summary>
        /// Cộng quãng đường đã đi vào tham số t trên spline (0–1 theo chiều dài ray).
        /// </summary>
        public static float AdvanceNormalizedTime(float t, float speedMetersPerSecond, float trackLengthMeters, float deltaTime)
        {
            if (trackLengthMeters <= 0f)
                return t;

            return t + speedMetersPerSecond * deltaTime / trackLengthMeters;
        }

        /// <summary>
        /// Giữ t trong khoảng hợp lệ sau mỗi frame (ray khép kín hoặc mở).
        /// </summary>
        public static float WrapOrClampTime(float t, bool closedLoop)
        {
            if (closedLoop)
                return Mathf.Repeat(t, 1f);

            return t > 1f ? 1f : t;
        }
    }
}
