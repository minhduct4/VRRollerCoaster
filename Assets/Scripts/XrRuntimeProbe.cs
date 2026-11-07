using UnityEngine.XR;
using UnityEngine.XR.Management;

namespace VRRoller
{
    /// <summary>
    /// Đọc trạng thái XR lúc runtime (không gán rig, không bật loader).
    /// </summary>
    public static class XrRuntimeProbe
    {
        /// <summary>
        /// True khi có simulator, display XR active, hoặc loader OpenXR đã gắn (Quest / Link).
        /// </summary>
        public static bool IsXrSessionRunning()
        {
            // Simulator dùng Input System, không qua OpenXR — phải kiểm trước loader.
            if (XrSimulatorProbe.IsActive())
                return true;

            if (IsXrDisplayActive())
                return true;

            return HasActiveXrLoader();
        }

        /// <summary>
        /// XR Management đã khởi tạo và loader (vd. OpenXR) đang được dùng.
        /// </summary>
        public static bool HasActiveXrLoader()
        {
            var settings = XRGeneralSettings.Instance;
            if (settings == null)
                return false;

            var manager = settings.Manager;
            if (manager == null)
                return false;

            return manager.activeLoader != null;
        }

        /// <summary>
        /// OpenXR / HMD: XRSettings bật và thiết bị display đang active (Quest, Link, simulator có session).
        /// </summary>
        public static bool IsXrDisplayActive()
        {
            return XRSettings.enabled && XRSettings.isDeviceActive;
        }
    }
}
