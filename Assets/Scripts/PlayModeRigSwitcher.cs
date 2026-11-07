using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Bật/tắt root rig VR và First-Person theo chế độ đã phát hiện (không di chuyển camera bằng script).
    /// </summary>
    public static class PlayModeRigSwitcher
    {
        /// <summary>
        /// Bật đúng một rig: VR (XR Origin) hoặc First-Person; rig còn lại tắt hẳn.
        /// </summary>
        public static void Apply(PlayModeKind mode, GameObject vrRigRoot, GameObject firstPersonRigRoot)
        {
            switch (mode)
            {
                case PlayModeKind.Vr:
                    SetActiveIfAssigned(vrRigRoot, true);
                    SetActiveIfAssigned(firstPersonRigRoot, false);
                    break;

                case PlayModeKind.FirstPerson:
                    SetActiveIfAssigned(vrRigRoot, false);
                    SetActiveIfAssigned(firstPersonRigRoot, true);
                    break;
            }
        }

        /// <summary>
        /// Gán active an toàn khi reference đã kéo trong Inspector.
        /// </summary>
        static void SetActiveIfAssigned(GameObject root, bool active)
        {
            if (root == null)
                return;

            if (root.activeSelf == active)
                return;

            root.SetActive(active);
        }
    }
}
