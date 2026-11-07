using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace VRRoller
{
    /// <summary>
    /// URP: Volume (vignette) chỉ vẽ lên Game/VR khi camera bật Post Processing.
    /// </summary>
    public static class UrpCameraPostProcessing
    {
        /// <summary>
        /// Bật renderPostProcessing trên Universal Additional Camera Data (nếu có).
        /// </summary>
        public static void EnableOnCamera(Camera camera)
        {
            if (camera == null)
                return;

            var data = camera.GetUniversalAdditionalCameraData();
            if (data == null)
                return;

            data.renderPostProcessing = true;
        }

        /// <summary>
        /// Bật post-processing cho mọi camera đang active (Main FP + XR HMD).
        /// </summary>
        public static void EnableOnAllActiveCameras()
        {
            var cameras = Camera.allCamerasCount;
            if (cameras <= 0)
                return;

            var buffer = new Camera[cameras];
            Camera.GetAllCameras(buffer);

            for (var i = 0; i < cameras; i++)
            {
                var cam = buffer[i];
                if (cam != null && cam.isActiveAndEnabled)
                    EnableOnCamera(cam);
            }
        }
    }
}
