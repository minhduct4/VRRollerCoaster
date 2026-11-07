using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Metadata Editor-only gắn lên mỗi module preview để chọn hai vị trí đặt trụ đỡ.
    /// GameObject chứa component này được tag EditorOnly và không được đưa vào build.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrackModuleInstance : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        ModularTrackBuilder builder;

        [SerializeField, HideInInspector]
        int segmentIndex;

        [SerializeField, HideInInspector]
        float startDistanceMeters;

        [SerializeField, HideInInspector]
        float lengthMeters;

        [SerializeField, HideInInspector]
        float sourceModuleLengthMeters;

        [SerializeField, HideInInspector]
        int sleeperCount;

        [SerializeField, HideInInspector]
        float sleeperWidthMeters;

        public ModularTrackBuilder Builder => builder;
        public int SegmentIndex => segmentIndex;
        public float StartDistanceMeters => startDistanceMeters;
        public float LengthMeters => lengthMeters;
        public int SupportAnchorCount => sleeperCount * 2;

        public void Configure(
            ModularTrackBuilder owner,
            int index,
            float startDistance,
            float length,
            float sourceModuleLength,
            int sleeperCountInModule,
            float sleeperWidth)
        {
            builder = owner;
            segmentIndex = index;
            startDistanceMeters = startDistance;
            lengthMeters = length;
            sourceModuleLengthMeters = sourceModuleLength;
            sleeperCount = Mathf.Max(1, sleeperCountInModule);
            sleeperWidthMeters = Mathf.Max(0.01f, sleeperWidth);
        }

        /// <summary>
        /// Mỗi sleeper có hai anchor ở hai đầu X. Anchor có cùng khoảng cách dọc spline với sleeper tương ứng.
        /// </summary>
        public float GetSupportAnchorDistanceMeters(int anchorIndex)
        {
            var sleeperIndex = Mathf.Clamp(anchorIndex / 2, 0, sleeperCount - 1);
            var normalized = (sleeperIndex + 0.5f) / sleeperCount;
            return startDistanceMeters + lengthMeters * normalized;
        }

        public Vector3 GetSupportAnchorWorldPosition(int anchorIndex)
        {
            var sleeperIndex = Mathf.Clamp(anchorIndex / 2, 0, sleeperCount - 1);
            var normalized = (sleeperIndex + 0.5f) / sleeperCount;
            var localZ = Mathf.Lerp(-sourceModuleLengthMeters * 0.5f, sourceModuleLengthMeters * 0.5f, normalized);
            var localX = (anchorIndex % 2 == 0 ? -1f : 1f) * sleeperWidthMeters * 0.5f;
            return transform.TransformPoint(new Vector3(localX, 0f, localZ));
        }

        public float GetSupportAnchorLateralOffsetMeters(int anchorIndex)
        {
            return (anchorIndex % 2 == 0 ? -1f : 1f) * sleeperWidthMeters * 0.5f;
        }

        public string GetSupportAnchorLabel(int anchorIndex)
        {
            var sleeper = anchorIndex / 2 + 1;
            var side = anchorIndex % 2 == 0 ? "L" : "R";
            return $"Sleeper {sleeper} {side}";
        }
    }
}
