using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Viên đạn: di chuyển thẳng, có vệt sáng TrailRenderer mờ dần, detect hit ShootingTarget.
    /// Dùng Rigidbody kinematic + Trigger collider. GunShooter gọi Initialize() ngay sau Instantiate.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class BulletController : MonoBehaviour
    {
        // ── Vệt sáng ─────────────────────────────────────────────────────────────
        [Header("Vệt sáng (Trail)")]
        [SerializeField, Tooltip("Bật/tắt vệt sáng theo đạn.")]
        bool enableTrail = true;

        [SerializeField, Range(0.1f, 5f), Tooltip("Thời gian vệt tồn tại và mờ dần (giây).")]
        float trailTime = 2f;

        [SerializeField, Range(0.001f, 0.3f), Tooltip("Độ rộng đầu vệt (mét).")]
        float trailStartWidth = 0.03f;

        [SerializeField, Tooltip("Màu đầu vệt — nên sáng/phát sáng.")]
        Color trailStartColor = new Color(1f, 0.95f, 0.4f, 1f);

        [SerializeField, Tooltip("Màu cuối vệt — thường transparent.")]
        Color trailEndColor = new Color(1f, 0.4f, 0f, 0f);

        [SerializeField, Tooltip("Material vệt sáng. Rỗng = tự tạo Unlit Additive (phù hợp URP).")]
        Material trailMaterial;

        // ── Runtime ───────────────────────────────────────────────────────────────
        Vector3 velocity;
        float   lifetime;
        float   timer;
        bool    hasHit;
        Rigidbody rb;
        TrailRenderer trail;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            var col = GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            if (enableTrail)
                SetupTrail();
        }

        /// <summary>
        /// Khởi tạo ngay sau Instantiate — GunShooter gọi hàm này.
        /// </summary>
        public void Initialize(Vector3 velocity, float lifetime)
        {
            this.velocity = velocity;
            this.lifetime = lifetime;
            timer  = 0f;
            hasHit = false;

            if (velocity.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(velocity.normalized);

            // Đặt emitting = true để vệt bắt đầu ngay khi đạn bay
            if (trail != null)
                trail.emitting = true;
        }

        void Update()
        {
            transform.position += velocity * Time.deltaTime;

            timer += Time.deltaTime;
            if (timer >= lifetime)
                DestroyBullet();
        }

        /// <summary>
        /// Phát hiện đạn chạm vào Collider của ShootingTarget.
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            var target = other.GetComponentInParent<ShootingTarget>();
            if (target == null) return;

            hasHit = true;
            target.OnBulletHit();
            DestroyBullet();
        }

        // ── Private helpers ───────────────────────────────────────────────────────

        /// <summary>
        /// Tạo hoặc lấy TrailRenderer và cấu hình gradient + width theo Inspector.
        /// </summary>
        void SetupTrail()
        {
            trail = GetComponent<TrailRenderer>();
            if (trail == null)
                trail = gameObject.AddComponent<TrailRenderer>();

            trail.time        = trailTime;
            trail.startWidth  = trailStartWidth;
            trail.endWidth    = 0f;
            trail.minVertexDistance = 0.02f;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows    = false;
            trail.emitting = false; // tắt cho đến khi Initialize() gọi

            // Gradient màu: đầu vệt sáng → cuối mờ dần
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(trailStartColor, 0f),
                        new GradientColorKey(trailEndColor,   1f) },
                new[] { new GradientAlphaKey(trailStartColor.a, 0f),
                        new GradientAlphaKey(0f, 1f) }
            );
            trail.colorGradient = gradient;

            // Material: dùng trailMaterial nếu có, nếu không tự tạo Unlit Additive
            trail.material = trailMaterial != null ? trailMaterial : CreateTrailMaterial();
        }

        /// <summary>
        /// Tạo material Unlit Additive tương thích URP để vệt sáng "phát sáng" lên trên scene.
        /// </summary>
        static Material CreateTrailMaterial()
        {
            // Thử URP Particles/Unlit trước, fallback về Sprites/Default
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                      ?? Shader.Find("Sprites/Default")
                      ?? Shader.Find("Unlit/Color");

            var mat = new Material(shader) { name = "M_BulletTrail_Runtime" };

            // Bật Additive blending để vệt cộng màu lên nền (trông sáng hơn)
            mat.SetFloat("_Surface", 1f);       // Transparent
            mat.SetFloat("_Blend",   3f);        // Additive (enum trong URP Particles shader)
            mat.enableInstancing = true;

            return mat;
        }

        /// <summary>
        /// Tắt emitting trước khi destroy để vệt còn lại mờ dần tự nhiên,
        /// sau đó tách trail ra scene để nó tự sống đủ trailTime rồi chết.
        /// </summary>
        void DestroyBullet()
        {
            if (trail != null && trail.time > 0f)
            {
                // Tách trail ra khỏi đạn: vệt còn lại tiếp tục mờ dần sau khi đạn biến mất
                trail.emitting = false;
                trail.transform.SetParent(null);
                Destroy(trail.gameObject, trail.time + 0.1f);
            }

            Destroy(gameObject);
        }
    }
}
