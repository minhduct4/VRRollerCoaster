using UnityEngine;

namespace VRRoller
{
    /// <summary>Cho phép chọn trực tiếp thân/cap của support để chỉnh endpoint của support cha.</summary>
    [DisallowMultipleComponent]
    public sealed class TrackSupportPart : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        TrackSupport support;

        public TrackSupport Support => support;

        public void Configure(TrackSupport value)
        {
            support = value;
        }
    }
}
