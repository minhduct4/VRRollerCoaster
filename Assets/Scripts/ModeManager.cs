using System.Collections;
using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Phát hiện XR rồi bật đúng rig. Không gọi InitializeLoaderSync — Quest Link dùng Init on Startup.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public class ModeManager : MonoBehaviour
    {
        public static ModeManager Instance { get; private set; }

        [Header("Rigs — root GameObject trong Hierarchy")]
        [SerializeField, Tooltip("XR Origin (VR). Nên là con của Cart. T3.2 bật khi có XR.")]
        GameObject vrRigRoot;

        [SerializeField, Tooltip("Root First-Person (vd. empty chứa Main Camera). T3.3 bật khi không XR.")]
        GameObject firstPersonRigRoot;

        [Header("XR probe")]
        [SerializeField, Tooltip("Số frame chờ thêm nếu XR init bất đồng bộ (Quest, Init on Startup).")]
        int xrProbeExtraFrames = 30;

        /// <summary>Phiên XR có đang chạy hay không (kết quả probe mới nhất).</summary>
        public bool IsXrActive { get; private set; }

        /// <summary>Chế độ suy ra từ probe; rig được áp qua <see cref="PlayModeRigSwitcher"/>.</summary>
        public PlayModeKind PlayMode { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("Chỉ nên có một ModeManager trong scene.", this);
                Destroy(this);
                return;
            }

            Instance = this;
        }

        void Start()
        {
            StartCoroutine(ProbeRoutine());
        }

        /// <summary>
        /// Probe ngay; nếu chưa có XR thì chờ thêm vài frame (loader lên trễ khi Init on Startup).
        /// </summary>
        IEnumerator ProbeRoutine()
        {
            ApplyDetection(XrRuntimeProbe.IsXrSessionRunning());
            XrStandaloneBootstrap.LogLinkHintIfNeeded(IsXrActive);

            if (!IsXrActive)
                yield return ProbeExtraFrames();
        }

        /// <summary>
        /// Quest / Link có thể sẵn sàng sau frame đầu — chỉ đọc trạng thái, không ép OpenXR.
        /// </summary>
        IEnumerator ProbeExtraFrames()
        {
            for (var frame = 0; frame < xrProbeExtraFrames; frame++)
            {
                yield return null;

                if (!XrRuntimeProbe.IsXrSessionRunning())
                    continue;

                ApplyDetection(true);
                yield break;
            }
        }

        /// <summary>
        /// Lưu kết quả probe, suy ra PlayMode, áp rig và log chế độ ra Console.
        /// </summary>
        void ApplyDetection(bool xrActive)
        {
            IsXrActive = xrActive;
            PlayMode = xrActive ? PlayModeKind.Vr : PlayModeKind.FirstPerson;

            PlayModeRigSwitcher.Apply(PlayMode, vrRigRoot, firstPersonRigRoot);

            if (PlayMode == PlayModeKind.Vr && vrRigRoot == null)
                Debug.LogWarning("ModeManager: PlayMode VR nhưng chưa gán Vr Rig Root.", this);

            if (PlayMode == PlayModeKind.FirstPerson && firstPersonRigRoot == null)
                Debug.LogWarning("ModeManager: PlayMode FirstPerson nhưng chưa gán Fp Rig Root.", this);

            PlayModeConsole.LogSelectedMode(PlayMode);
        }
    }
}
