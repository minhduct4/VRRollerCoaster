using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Gắn lên renderer con của module preview để click trực tiếp Rail Left/Rail Right vẫn mở được support handles.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrackModulePart : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        TrackModuleInstance module;

        public TrackModuleInstance Module => module;

        public void Configure(TrackModuleInstance value)
        {
            module = value;
        }
    }
}
