using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Dòng log chuẩn cho chấm bài / kiểm thử ba chế độ (cùng format mọi lần đổi mode).
    /// </summary>
    public static class PlayModeConsole
    {
        /// <summary>
        /// Ghi Console một dòng cố định: <c>Mode: VR</c> hoặc <c>Mode: FirstPerson</c>.
        /// </summary>
        public static void LogSelectedMode(PlayModeKind mode)
        {
            switch (mode)
            {
                case PlayModeKind.Vr:
                    Debug.Log("Mode: VR");
                    break;
                case PlayModeKind.FirstPerson:
                    Debug.Log("Mode: FirstPerson");
                    break;
            }
        }
    }
}
