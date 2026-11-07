using UnityEngine;
using UnityEngine.Splines;

namespace VRRoller
{
    /// <summary>
    /// Dữ liệu runtime tối thiểu cho tool rải ray mô-đun.
    /// Việc tạo/xóa preview nằm hoàn toàn trong ModularTrackBuilderEditor để không ảnh hưởng bản build.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineContainer))]
    public sealed class ModularTrackBuilder : MonoBehaviour
    {
        [Header("Track source")]
        [SerializeField, Tooltip("Để trống để dùng Spline Container cùng GameObject.")]
        SplineContainer splineContainer;

        [Header("Module settings")]
        [SerializeField]
        TrackModuleProfile profile;

        [SerializeField, Min(0.1f), Tooltip("Ưu tiên chiều dài mỗi mảnh. Tool tự điều chỉnh rất nhẹ để khép kín vòng ray.")]
        float preferredModuleLengthMeters = 1f;

        [SerializeField, Range(4, 32), Tooltip("Số mẫu trên mỗi mét khi quy đổi quãng đường sang tham số spline.")]
        int samplesPerMeter = 10;

        [Header("Support columns")]
        [SerializeField, Min(0.01f), Tooltip("Bán kính capsule của chân đỡ do Scene tool tạo.")]
        float supportRadiusMeters = 0.12f;

        [SerializeField, Tooltip("Material dùng cho capsule chân đỡ. Để trống sẽ dùng material mặc định của Unity.")]
        Material supportMaterial;

        [SerializeField, HideInInspector]
        Material[] supportMaterialOptions = new Material[0];

        [SerializeField, HideInInspector]
        int supportMaterialIndex;

        [Header("Generated preview (Editor only)")]
        [SerializeField, HideInInspector]
        Transform previewRoot;

        [SerializeField, HideInInspector]
        Transform supportsRoot;

        public SplineContainer SplineContainer => splineContainer != null
            ? splineContainer
            : GetComponent<SplineContainer>();

        public TrackModuleProfile Profile => profile;
        public float PreferredModuleLengthMeters => preferredModuleLengthMeters;
        public int SamplesPerMeter => samplesPerMeter;
        public float SupportRadiusMeters => supportRadiusMeters;
        public Material SupportMaterial => supportMaterial;
        public Material[] SupportMaterialOptions => supportMaterialOptions;
        public int SupportMaterialIndex => supportMaterialIndex;
        public Transform PreviewRoot => previewRoot;
        public Transform SupportsRoot => supportsRoot;

        /// <summary>Được gọi bởi Custom Editor sau khi tạo asset profile mặc định.</summary>
        public void SetProfile(TrackModuleProfile value)
        {
            profile = value;
        }

        /// <summary>Được gọi bởi Custom Editor khi tạo/xóa root preview.</summary>
        public void SetPreviewRoot(Transform value)
        {
            previewRoot = value;
        }

        /// <summary>Được gọi bởi Editor tool khi tạo root hoặc material cho các chân đỡ.</summary>
        public void SetSupportSettings(float radius, Material material, Transform root)
        {
            supportRadiusMeters = Mathf.Max(0.01f, radius);
            supportMaterial = material;
            supportsRoot = root;
        }

        /// <summary>Editor tool dùng palette này để hiển thị dropdown material cho các chân đỡ.</summary>
        public void SetSupportMaterialOptions(Material[] options, int selectedIndex)
        {
            supportMaterialOptions = options ?? new Material[0];
            supportMaterialIndex = supportMaterialOptions.Length == 0
                ? 0
                : Mathf.Clamp(selectedIndex, 0, supportMaterialOptions.Length - 1);
            if (supportMaterialOptions.Length > 0)
                supportMaterial = supportMaterialOptions[supportMaterialIndex];
        }

        void Reset()
        {
            splineContainer = GetComponent<SplineContainer>();
        }

        void OnValidate()
        {
            if (splineContainer == null)
                splineContainer = GetComponent<SplineContainer>();

            preferredModuleLengthMeters = Mathf.Max(0.1f, preferredModuleLengthMeters);
            samplesPerMeter = Mathf.Clamp(samplesPerMeter, 4, 32);
            supportRadiusMeters = Mathf.Max(0.01f, supportRadiusMeters);
            supportMaterialIndex = Mathf.Max(0, supportMaterialIndex);
        }
    }
}
