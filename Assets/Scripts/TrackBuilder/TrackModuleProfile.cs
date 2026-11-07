using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Cấu hình dùng chung cho các mảnh ray được rải bởi <see cref="ModularTrackBuilder"/>.
    /// Mọi kích thước dùng đơn vị mét; prefab phải có trục Z là chiều dài mảnh ray.
    /// </summary>
    [CreateAssetMenu(
        fileName = "TrackModuleProfile",
        menuName = "VR Roller/Track Module Profile",
        order = 10)]
    public sealed class TrackModuleProfile : ScriptableObject
    {
        [Header("Module prefab")]
        [SerializeField, Tooltip("Prefab ray thẳng. Pivot ở tâm; trục Z là hướng chạy; scale mặc định = 1.")]
        GameObject modulePrefab;

        [SerializeField, Min(0.05f), Tooltip("Chiều dài Z của prefab gốc, tính bằng mét.")]
        float moduleLengthMeters = 1f;

        [Header("Default generated module")]
        [SerializeField, Min(0.1f), Tooltip("Khoảng cách tâm hai rail của prefab mặc định.")]
        float railGaugeMeters = 1f;

        [SerializeField, Min(0.01f), Tooltip("Bán kính mỗi rail của prefab mặc định.")]
        float railRadiusMeters = 0.08f;

        [SerializeField, Min(0.05f), Tooltip("Khoảng cách giữa các tà vẹt của prefab mặc định.")]
        float sleeperSpacingMeters = 0.5f;

        [SerializeField, Min(0.05f), Tooltip("Kích thước X/Y/Z của một tà vẹt trong prefab mặc định.")]
        Vector3 sleeperSizeMeters = new Vector3(1.35f, 0.12f, 0.16f);

        [Header("Adaptive subdivision")]
        [SerializeField, Min(0.02f), Tooltip("Mảnh ray không bị rút ngắn thấp hơn mức này, kể cả ở cua gắt.")]
        float minimumModuleLengthMeters = 0.15f;

        [SerializeField, Range(0.5f, 20f), Tooltip("Adaptive subdivision giữ tổng đổi hướng của một module không vượt góc này khi có thể.")]
        float maxTurnAnglePerModule = 4f;

        public GameObject ModulePrefab => modulePrefab;
        public float ModuleLengthMeters => moduleLengthMeters;
        public float RailGaugeMeters => railGaugeMeters;
        public float RailRadiusMeters => railRadiusMeters;
        public float SleeperSpacingMeters => sleeperSpacingMeters;
        public Vector3 SleeperSizeMeters => sleeperSizeMeters;
        public float MinimumModuleLengthMeters => minimumModuleLengthMeters;
        public float MaxTurnAnglePerModule => maxTurnAnglePerModule;

        /// <summary>Editor tool gọi hàm này sau khi tạo/cập nhật prefab ray mặc định.</summary>
        public void SetGeneratedModulePrefab(GameObject prefab)
        {
            modulePrefab = prefab;
        }

        void OnValidate()
        {
            moduleLengthMeters = Mathf.Max(0.05f, moduleLengthMeters);
            railGaugeMeters = Mathf.Max(0.1f, railGaugeMeters);
            railRadiusMeters = Mathf.Max(0.01f, railRadiusMeters);
            sleeperSpacingMeters = Mathf.Max(0.05f, sleeperSpacingMeters);
            sleeperSizeMeters.x = Mathf.Max(0.05f, sleeperSizeMeters.x);
            sleeperSizeMeters.y = Mathf.Max(0.05f, sleeperSizeMeters.y);
            sleeperSizeMeters.z = Mathf.Max(0.05f, sleeperSizeMeters.z);
            minimumModuleLengthMeters = Mathf.Max(0.02f, minimumModuleLengthMeters);
            maxTurnAnglePerModule = Mathf.Clamp(maxTurnAnglePerModule, 0.5f, 20f);
        }
    }
}
