using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Cộng dồn yaw/pitch từ delta chuột và kẹp góc ngẩng/cúi (First-Person).
    /// </summary>
    public static class FirstPersonLookAngles
    {
        /// <summary>
        /// Cộng delta chuột vào góc; kẹp pitch trong [minPitch, maxPitch].
        /// </summary>
        public static void Accumulate(
            ref float yawDegrees,
            ref float pitchDegrees,
            Vector2 mouseDeltaPixels,
            float sensitivity,
            bool invertY,
            float minPitch,
            float maxPitch)
        {
            yawDegrees += mouseDeltaPixels.x * sensitivity;

            // Unity Euler X dương = cúi xuống. Chuột lên (delta.y > 0) mặc định phải ngẩng (giảm pitch).
            var pitchSign = invertY ? 1f : -1f;
            pitchDegrees += mouseDeltaPixels.y * sensitivity * pitchSign;
            pitchDegrees = Mathf.Clamp(pitchDegrees, minPitch, maxPitch);
        }

        /// <summary>
        /// Rotation local: yaw quanh Y, pitch quanh X — không roll, thân Cart không bị xoay.
        /// </summary>
        public static Quaternion ToLocalRotation(float yawDegrees, float pitchDegrees)
        {
            return Quaternion.Euler(pitchDegrees, yawDegrees, 0f);
        }
    }
}
