using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Chỉ ghi gợi ý Quest Link. Không khởi tạo OpenXR — ép loader khi không HMD làm Editor đứng (xrGetSystem retry).
    /// </summary>
    public static class XrStandaloneBootstrap
    {
        static bool s_linkHintLoggedThisSession;

        /// <summary>
        /// Một dòng gợi ý mỗi lần Play nếu không có XR (Link cần Init on Startup Standalone).
        /// </summary>
        public static void LogLinkHintIfNeeded(bool xrActive)
        {
            if (xrActive || s_linkHintLoggedThisSession || !ShouldRunOnThisPlatform())
                return;

            s_linkHintLoggedThisSession = true;
            Debug.Log(
                "Quest Link trên PC: bật Initialize XR on Startup (tab Windows/Mac/Linux). " +
                "Sinh viên không headset: tắt lại (Hotfix XR).");
        }

        /// <summary>
        /// Chỉ đúng trên Standalone (Windows/Mac/Linux Editor hoặc player).
        /// </summary>
        public static bool ShouldRunOnThisPlatform()
        {
#if UNITY_ANDROID
            return false;
#else
            return Application.platform == RuntimePlatform.WindowsPlayer
                   || Application.platform == RuntimePlatform.WindowsEditor
                   || Application.platform == RuntimePlatform.OSXPlayer
                   || Application.platform == RuntimePlatform.OSXEditor
                   || Application.platform == RuntimePlatform.LinuxPlayer
                   || Application.platform == RuntimePlatform.LinuxEditor;
#endif
        }
    }
}
