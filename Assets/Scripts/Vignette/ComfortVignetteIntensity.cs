using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Ánh xạ tốc độ toa (m/s) → độ đậm vignette URP (0–1).
    /// </summary>
    public static class ComfortVignetteIntensity
    {
        /// <summary>
        /// Tốc độ ≤ ngưỡng thấp → intensity tối thiểu; ≥ ngưỡng cao → intensity tối đa (nội suy tuyến tính).
        /// </summary>
        public static float FromSpeedMetersPerSecond(
            float speedMps,
            float speedAtMinIntensity,
            float speedAtMaxIntensity,
            float minIntensity,
            float maxIntensity,
            float responseExponent)
        {
            if (speedMps <= speedAtMinIntensity)
                return minIntensity;

            if (speedAtMaxIntensity <= speedAtMinIntensity)
                return maxIntensity;

            var t = Mathf.InverseLerp(speedAtMinIntensity, speedAtMaxIntensity, speedMps);
            // Exponent < 1 → vignette tăng nhanh hơn giữa/chặng nhanh (dễ thấy trên Game view).
            if (responseExponent > 0f && !Mathf.Approximately(responseExponent, 1f))
                t = Mathf.Pow(Mathf.Clamp01(t), responseExponent);

            return Mathf.Lerp(minIntensity, maxIntensity, t);
        }
    }
}
