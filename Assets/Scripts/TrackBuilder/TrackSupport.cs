using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Trụ capsule nối một đầu sleeper với một điểm snap trong môi trường.
    /// Endpoint được giữ ở world-space; điểm đầu được Editor tool cập nhật lại theo spline khi rebuild preview.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TrackSupport : MonoBehaviour
    {
        [SerializeField, HideInInspector]
        ModularTrackBuilder builder;

        [SerializeField, HideInInspector]
        float anchorDistanceMeters;

        [SerializeField, HideInInspector]
        Vector3 startPoint;

        [SerializeField, HideInInspector]
        Vector3 endPoint;

        [SerializeField, HideInInspector, Min(0.01f)]
        float radiusMeters = 0.12f;

        [SerializeField, HideInInspector]
        float lateralOffsetMeters;

        [SerializeField, HideInInspector]
        Transform cylinderBody;

        [SerializeField, HideInInspector]
        Transform topCap;

        [SerializeField, HideInInspector]
        Transform bottomCap;

        public ModularTrackBuilder Builder => builder;
        public float AnchorDistanceMeters => anchorDistanceMeters;
        public Vector3 StartPoint => startPoint;
        public Vector3 EndPoint => endPoint;
        public float LateralOffsetMeters => lateralOffsetMeters;
        public bool HasCapsuleVisual => cylinderBody != null && topCap != null && bottomCap != null;

        public void Configure(
            ModularTrackBuilder owner,
            float anchorDistance,
            Vector3 start,
            Vector3 end,
            float lateralOffset,
            float radius)
        {
            builder = owner;
            anchorDistanceMeters = anchorDistance;
            endPoint = end;
            lateralOffsetMeters = lateralOffset;
            radiusMeters = Mathf.Max(0.01f, radius);
            ApplyStartPoint(start);
        }

        public void SetCapsuleVisualParts(Transform body, Transform top, Transform bottom)
        {
            cylinderBody = body;
            topCap = top;
            bottomCap = bottom;
        }

        public void ApplyStartPoint(Vector3 value)
        {
            startPoint = value;
            ApplyGeometry(startPoint, endPoint);
        }

        /// <summary>Chỉ cập nhật hình học tạm thời trong Scene View; không ghi endpoint đã serialize.</summary>
        public void PreviewEndPoint(Vector3 value)
        {
            ApplyGeometry(startPoint, value);
        }

        public void CommitEndPoint(Vector3 value)
        {
            endPoint = value;
            ApplyGeometry(startPoint, endPoint);
        }

        void ApplyGeometry(Vector3 start, Vector3 end)
        {
            var direction = end - start;
            var length = direction.magnitude;
            transform.SetPositionAndRotation(
                (start + end) * 0.5f,
                length > 0.0001f ? Quaternion.FromToRotation(Vector3.up, direction) : Quaternion.identity);
            transform.localScale = Vector3.one;

            if (!HasCapsuleVisual)
                return;

            // Giữ hai cap là sphere tỉ lệ đều; chỉ phần cylinder giữa hai cap được kéo dài.
            // Vì vậy support luôn có silhouette capsule, không bị oval/méo khi rất dài.
            var cylinderLength = Mathf.Max(0f, length - radiusMeters * 2f);
            cylinderBody.localPosition = Vector3.zero;
            cylinderBody.localRotation = Quaternion.identity;
            cylinderBody.localScale = new Vector3(radiusMeters * 2f, Mathf.Max(cylinderLength * 0.5f, 0.001f), radiusMeters * 2f);

            topCap.localPosition = Vector3.up * (cylinderLength * 0.5f);
            topCap.localRotation = Quaternion.identity;
            topCap.localScale = Vector3.one * (radiusMeters * 2f);

            bottomCap.localPosition = Vector3.down * (cylinderLength * 0.5f);
            bottomCap.localRotation = Quaternion.identity;
            bottomCap.localScale = Vector3.one * (radiusMeters * 2f);
        }
    }
}
