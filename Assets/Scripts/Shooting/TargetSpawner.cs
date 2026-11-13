using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using URandom = UnityEngine.Random;

namespace VRRoller
{
    /// <summary>
    /// Sinh bia bắn dọc ray tiến dần trước Cart — KHÔNG random toàn bộ một lần.
    /// Pre-generate vị trí dữ liệu (nhẹ); chỉ Instantiate GameObject khi Cart đến gần.
    /// Hủy bia đã qua để giữ số lượng active thấp.
    /// </summary>
    public class TargetSpawner : MonoBehaviour
    {
        // ── References ────────────────────────────────────────────────────────────
        [Header("References")]
        [SerializeField, Tooltip("Spline Container của đường ray (Track).")]
        SplineContainer track;

        [SerializeField, Tooltip("CartMover để đọc vị trí Cart.")]
        CartMover cart;

        [Header("Target Prefab")]
        [SerializeField, Tooltip("Prefab bia bắn do user tạo (cần có ShootingTarget + Collider).")]
        GameObject targetPrefab;

        // ── Spawn window ──────────────────────────────────────────────────────────
        [Header("Spawn window (mét)")]
        [SerializeField, Range(5f, 150f), Tooltip("Sinh bia trước Cart x mét.")]
        float spawnAheadMeters = 50f;

        [SerializeField, Range(1f, 50f), Tooltip("Hủy bia sau khi Cart qua x mét.")]
        float despawnBehindMeters = 15f;

        // ── Spacing ───────────────────────────────────────────────────────────────
        [Header("Khoảng cách giữa các bia (mét)")]
        [SerializeField, Range(1f, 30f), Tooltip("Khoảng cách tối thiểu.")]
        float minSpacingMeters = 8f;

        [SerializeField, Range(2f, 60f), Tooltip("Khoảng cách tối đa.")]
        float maxSpacingMeters = 20f;

        // ── Lateral (ngang) ───────────────────────────────────────────────────────
        [Header("Lệch ngang so với tâm ray (mét)")]
        [SerializeField, Tooltip("Lệch trái (âm). Ví dụ -3.")]
        float minLateral = -3f;

        [SerializeField, Tooltip("Lệch phải (dương). Ví dụ 3.")]
        float maxLateral = 3f;

        // ── Height ────────────────────────────────────────────────────────────────
        [Header("Lệch cao so với ray (mét, world up)")]
        [SerializeField, Range(-1f, 2f), Tooltip("Cao tối thiểu.")]
        float minHeight = 0.5f;

        [SerializeField, Range(0f, 8f), Tooltip("Cao tối đa.")]
        float maxHeight = 3f;

        // ── Scale ─────────────────────────────────────────────────────────────────
        [Header("Scale bia (random mỗi bia)")]
        [SerializeField, Range(0.1f, 2f), Tooltip("Scale nhỏ nhất.")]
        float minScale = 0.8f;

        [SerializeField, Range(0.5f, 5f), Tooltip("Scale lớn nhất.")]
        float maxScale = 1.5f;

        // ── Seed ──────────────────────────────────────────────────────────────────
        [Header("Random seed")]
        [SerializeField, Tooltip("0 = seed theo thời gian (khác mỗi lần Play); > 0 = cố định để tái hiện.")]
        int randomSeed = 0;

        // ── Internal data ─────────────────────────────────────────────────────────

        /// <summary>
        /// Mỗi slot là một vị trí đã pre-generate (data only, chưa spawn).
        /// </summary>
        class SpawnPoint
        {
            public float NormalizedT;      // vị trí trên spline (0–1)
            public Vector3 WorldPosition;  // vị trí thế giới đã tính sẵn
            public float ScaleValue;       // scale random
            public bool HasSpawned;        // đã Instantiate chưa
            public GameObject Instance;    // null nếu chưa spawn hoặc đã destroy
        }

        readonly List<SpawnPoint> spawnPoints = new List<SpawnPoint>(64);

        float trackLength;
        float prevCartT;           // detect cart wrap (closed spline)
        bool initialized;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Start()
        {
            if (!ValidateRefs()) return;

            trackLength = track.CalculateLength();
            if (trackLength < 0.1f)
            {
                Debug.LogError("TargetSpawner: track quá ngắn.", this);
                return;
            }

            InitRandom();

            // Pre-generate TẤT CẢ vị trí dữ liệu ngay lúc Start (nhẹ, chỉ là struct)
            // Nhưng KHÔNG Instantiate — Instantiate dần trong Update khi Cart đến gần
            GenerateAllSlots();

            prevCartT = cart.NormalizedTime;
            initialized = true;
        }

        void Update()
        {
            if (!initialized || targetPrefab == null) return;

            var cartT = cart.NormalizedTime;

            // Detect cart đã vòng (closed spline): reset tất cả slot để spawn lại vòng sau
            if (track.Spline.Closed && cartT < prevCartT - 0.5f)
                ResetAllSlotsForNextLap();

            prevCartT = cartT;

            var spawnAheadT  = spawnAheadMeters  / trackLength;
            var despawnBehindT = despawnBehindMeters / trackLength;

            foreach (var sp in spawnPoints)
                UpdateSlot(sp, cartT, spawnAheadT, despawnBehindT);
        }

        // ── Core logic ────────────────────────────────────────────────────────────

        /// <summary>
        /// Quyết định spawn hoặc despawn cho một slot dựa trên khoảng cách đến Cart.
        /// </summary>
        void UpdateSlot(SpawnPoint sp, float cartT, float spawnAheadT, float despawnBehindT)
        {
            // Khoảng cách chuẩn hóa: dương = phía trước Cart, âm = đã qua
            float dt = NormalizedDelta(sp.NormalizedT, cartT);

            bool shouldBeAlive = dt >= -despawnBehindT && dt <= spawnAheadT;

            if (shouldBeAlive && !sp.HasSpawned)
            {
                // Vào cửa sổ: spawn lần đầu hoặc spawn lại (vòng mới)
                sp.Instance   = Instantiate(targetPrefab, sp.WorldPosition, Quaternion.identity, transform);
                sp.Instance.transform.localScale = Vector3.one * sp.ScaleValue;
                sp.HasSpawned = true;
            }
            else if (!shouldBeAlive && sp.HasSpawned)
            {
                // Ra khỏi cửa sổ: hủy nếu chưa bị bắn
                if (sp.Instance != null)
                    Destroy(sp.Instance);
                sp.Instance   = null;
                sp.HasSpawned = false;
            }
        }

        /// <summary>
        /// Pre-generate toàn bộ vị trí data (không Instantiate).
        /// Chỉ chạy một lần lúc Start.
        /// </summary>
        void GenerateAllSlots()
        {
            spawnPoints.Clear();

            var clampedMin = Mathf.Min(minSpacingMeters, maxSpacingMeters);
            var clampedMax = Mathf.Max(minSpacingMeters, maxSpacingMeters);

            // Bắt đầu cách đầu ray một đoạn ngẫu nhiên để không spawn tại t=0
            float cursor = URandom.Range(clampedMin * 0.3f, clampedMin);

            while (cursor < trackLength)
            {
                float t = cursor / trackLength;
                var wp = CalcWorldPosition(t);

                spawnPoints.Add(new SpawnPoint
                {
                    NormalizedT   = t,
                    WorldPosition = wp,
                    ScaleValue    = URandom.Range(minScale, maxScale),
                    HasSpawned    = false,
                    Instance      = null
                });

                cursor += URandom.Range(clampedMin, clampedMax);
            }

            Debug.Log($"TargetSpawner: pre-generated {spawnPoints.Count} vị trí bia trên ray.", this);
        }

        /// <summary>
        /// Tính vị trí thế giới của bia tại normalized t, lệch ngang và cao theo random đã gen.
        /// </summary>
        Vector3 CalcWorldPosition(float t)
        {
            // Lấy pose trên spline
            track.Evaluate(t, out float3 pos3, out float3 tangent3, out float3 up3);
            var pos     = (Vector3)pos3;
            var forward = ((Vector3)tangent3).normalized;
            var up      = ((Vector3)up3).normalized;

            if (up.sqrMagnitude < 0.001f)      up = Vector3.up;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;

            // Right = cross(forward, up) — dùng để lệch ngang
            var right = Vector3.Cross(forward, up).normalized;

            // Random lệch ngang và cao
            var lateral = URandom.Range(minLateral, maxLateral);
            var height  = URandom.Range(minHeight,  maxHeight);

            return pos + right * lateral + Vector3.up * height;
        }

        /// <summary>
        /// Reset tất cả slot khi Cart hoàn thành một vòng (spline closed).
        /// Bia sẽ được spawn lại trên vòng tiếp theo.
        /// </summary>
        void ResetAllSlotsForNextLap()
        {
            foreach (var sp in spawnPoints)
            {
                if (sp.Instance != null)
                    Destroy(sp.Instance);
                sp.Instance   = null;
                sp.HasSpawned = false;
            }

            // Regenerate world positions (vẫn giữ NormalizedT, chỉ random lại scale/vị trí)
            foreach (var sp in spawnPoints)
            {
                sp.WorldPosition = CalcWorldPosition(sp.NormalizedT);
                sp.ScaleValue    = URandom.Range(minScale, maxScale);
            }
        }

        /// <summary>
        /// Khoảng cách chuẩn hóa từ cartT đến slotT (tính theo chiều tiến).
        /// Dương: slot ở phía trước. Âm: slot đã qua.
        /// Với spline khép kín, xử lý wrap quanh 0/1.
        /// </summary>
        float NormalizedDelta(float slotT, float cartT)
        {
            var delta = slotT - cartT;

            if (track.Spline.Closed)
            {
                // Wrap: đảm bảo delta nằm trong [-0.5, 0.5]
                if (delta >  0.5f) delta -= 1f;
                if (delta < -0.5f) delta += 1f;
            }

            return delta;
        }

        /// <summary>Khởi tạo Random với seed cố định hoặc theo giờ.</summary>
        void InitRandom()
        {
            var seed = randomSeed > 0 ? randomSeed : (int)System.DateTime.Now.Ticks;
            URandom.InitState(seed);
        }

        /// <summary>Kiểm tra references bắt buộc trước Start.</summary>
        bool ValidateRefs()
        {
            if (track == null)
            {
                Debug.LogError("TargetSpawner: gán Track (Spline Container).", this);
                return false;
            }

            if (cart == null)
            {
                Debug.LogError("TargetSpawner: gán CartMover.", this);
                return false;
            }

            if (targetPrefab == null)
                Debug.LogWarning("TargetSpawner: chưa gán Target Prefab — bia sẽ không xuất hiện.", this);

            return true;
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (minSpacingMeters > maxSpacingMeters)
                maxSpacingMeters = minSpacingMeters;

            if (minScale > maxScale)
                maxScale = minScale;

            if (minHeight > maxHeight)
                maxHeight = minHeight;
        }

        void OnDrawGizmosSelected()
        {
            if (!Application.isPlaying || !initialized) return;

            foreach (var sp in spawnPoints)
            {
                // Xanh = chưa spawn, Đỏ = đang active, Xám = đã qua
                Gizmos.color = sp.HasSpawned && sp.Instance != null
                    ? Color.red
                    : sp.HasSpawned
                        ? Color.gray
                        : new Color(0f, 1f, 0.5f, 0.6f);
                Gizmos.DrawWireCube(sp.WorldPosition, Vector3.one * 0.5f);
            }
        }
#endif
    }
}
