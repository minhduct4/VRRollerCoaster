using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;

namespace VRRoller
{
    /// <summary>
    /// Phát hiện sample XR Device Simulator / XR Interaction Simulator — không cần OpenXR, không tự giả lập HMD.
    /// </summary>
    public static class XrSimulatorProbe
    {
        /// <summary>
        /// True khi instance simulator đang sống (kéo prefab vào scene hoặc auto-instantiate).
        /// </summary>
        public static bool IsActive()
        {
            if (XRDeviceSimulator.instance != null)
                return true;

            return XRInteractionSimulator.instance != null;
        }
    }
}
