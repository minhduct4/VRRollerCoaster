using Unity.XR.CoreUtils;
using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Quy tắc chống jitter VR (T4.1): Cart di chuyển transform toa; camera VR do tracking / TrackedPoseDriver.
    /// </summary>
    public static class VrCameraPolicy
    {
        /// <summary>
        /// True nếu transform nằm dưới XR Origin (rig VR / Simulation).
        /// </summary>
        public static bool IsUnderXrOrigin(Transform transform)
        {
            if (transform == null)
                return false;

            return transform.GetComponentInParent<XROrigin>(true) != null;
        }

        /// <summary>
        /// CartMover chỉ được gắn trên root toa — không gắn camera hoặc node con XR Origin.
        /// </summary>
        public static bool IsValidCartMoverHost(Transform transform, out string reason)
        {
            reason = null;
            if (transform == null)
            {
                reason = "Transform null.";
                return false;
            }

            if (transform.GetComponent<Camera>() != null)
            {
                reason = "Cart Mover không được gắn trên Camera.";
                return false;
            }

            if (IsUnderXrOrigin(transform))
            {
                reason = "Cart Mover phải ở root toa, không nằm dưới XR Origin.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// First Person Look chỉ thuộc FirstPersonRig — không gắn dưới XR Origin.
        /// </summary>
        public static bool IsValidFirstPersonLookHost(Transform transform, out string reason)
        {
            reason = null;
            if (transform == null)
            {
                reason = "Transform null.";
                return false;
            }

            if (IsUnderXrOrigin(transform))
            {
                reason = "First Person Look không được gắn dưới XR Origin (sẽ ghi đè góc VR).";
                return false;
            }

            return true;
        }
    }
}
