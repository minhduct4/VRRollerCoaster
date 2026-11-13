using Unity.XR.CoreUtils;
using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Rig hiển thị 2 tay cầm VR (T8.1). Gắn trên XR Origin; First-Person tắt cả XR Origin nên tay không hiện.
    /// </summary>
    [DisallowMultipleComponent]
    public class VrHandControllerRig : MonoBehaviour
    {
        const string LeftHandObjectName = "Left Hand Controller";
        const string RightHandObjectName = "Right Hand Controller";
        const string CameraOffsetObjectName = "Camera Offset";

        [Header("Anchors — để trống sẽ tự tạo dưới Camera Offset (cùng parent Main Camera)")]
        [SerializeField, Tooltip("Transform anchor tay trái (có VrHandControllerAnchor).")]
        Transform leftHandAnchor;

        [SerializeField, Tooltip("Transform anchor tay phải.")]
        Transform rightHandAnchor;

        [Header("Visual")]
        [SerializeField, Tooltip("URP Lit dùng chung; để trống dùng material mặc định primitive.")]
        Material controllerMaterial;

        [SerializeField, Tooltip("Tắt mesh tay khi không có pose (Quest chưa track). Simulator vẫn hiện.")]
        bool hideWhenUntracked = true;

        Transform leftVisualRoot;
        Transform rightVisualRoot;
        VrHandControllerAnchor leftAnchor;
        VrHandControllerAnchor rightAnchor;

        /// <summary>Anchor tay phải — panel súng T8.3 gắn sau.</summary>
        public Transform RightHandAnchor => rightHandAnchor;

        /// <summary>Anchor tay trái.</summary>
        public Transform LeftHandAnchor => leftHandAnchor;

        void Awake()
        {
            EnsureHandHierarchy();
            EnsureVisuals();
        }

        void LateUpdate()
        {
            if (!hideWhenUntracked)
                return;

            UpdateVisualActive(leftVisualRoot, HandSide.Left);
            UpdateVisualActive(rightVisualRoot, HandSide.Right);
        }

        /// <summary>
        /// Tracking tay — simulator đọc Input System, không dùng InputDevices.
        /// </summary>
        public bool IsHandTracked(HandSide side)
        {
            var anchor = side == HandSide.Left ? leftAnchor : rightAnchor;
            if (anchor != null)
                return anchor.IsTracked;

            CacheAnchors();
            anchor = side == HandSide.Left ? leftAnchor : rightAnchor;
            return anchor != null && anchor.IsTracked;
        }

        /// <summary>
        /// Tạo anchor + visual nếu scene chưa setup (Play hoặc gọi từ menu Editor).
        /// </summary>
        public void EnsureHandHierarchy()
        {
            var handParent = ResolveHandParent();

            if (leftHandAnchor == null)
                leftHandAnchor = EnsureAnchor(handParent, LeftHandObjectName, HandSide.Left);
            else
                ReparentIfNeeded(leftHandAnchor, handParent);

            if (rightHandAnchor == null)
                rightHandAnchor = EnsureAnchor(handParent, RightHandObjectName, HandSide.Right);
            else
                ReparentIfNeeded(rightHandAnchor, handParent);

            CacheAnchors();
        }

        /// <summary>
        /// Tạo mesh controller dưới mỗi anchor (chỉ một lần).
        /// </summary>
        public void EnsureVisuals()
        {
            if (leftHandAnchor != null && leftVisualRoot == null)
                leftVisualRoot = VrHandControllerVisualFactory.CreateControllerVisual(
                    leftHandAnchor, HandSide.Left, controllerMaterial);

            if (rightHandAnchor != null && rightVisualRoot == null)
                rightVisualRoot = VrHandControllerVisualFactory.CreateControllerVisual(
                    rightHandAnchor, HandSide.Right, controllerMaterial);
        }

        /// <summary>
        /// Một empty anchor + component tracking.
        /// </summary>
        static Transform EnsureAnchor(Transform parent, string objectName, HandSide side)
        {
            var existing = parent.Find(objectName);
            if (existing != null)
            {
                var anchor = existing.GetComponent<VrHandControllerAnchor>();
                if (anchor == null)
                    anchor = existing.gameObject.AddComponent<VrHandControllerAnchor>();
                anchor.Configure(side);
                return existing;
            }

            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var newAnchor = go.AddComponent<VrHandControllerAnchor>();
            newAnchor.Configure(side);
            return go.transform;
        }

        /// <summary>
        /// Pose tay local phải cùng parent với Main Camera (Camera Offset), không để dưới XR Origin.
        /// </summary>
        Transform ResolveHandParent()
        {
            var xrOrigin = GetComponent<XROrigin>();
            if (xrOrigin != null && xrOrigin.CameraFloorOffsetObject != null)
                return xrOrigin.CameraFloorOffsetObject.transform;

            var cameraOffset = transform.Find(CameraOffsetObjectName);
            return cameraOffset != null ? cameraOffset : transform;
        }

        /// <summary>
        /// Kéo anchor sẵn có về Camera Offset nếu scene đang để dưới XR Origin.
        /// </summary>
        static void ReparentIfNeeded(Transform anchor, Transform handParent)
        {
            if (anchor == null || handParent == null || anchor.parent == handParent)
                return;

            anchor.SetParent(handParent, false);
        }

        /// <summary>
        /// Cache component tracking để LateUpdate không Find/GetComponent.
        /// </summary>
        void CacheAnchors()
        {
            if (leftHandAnchor != null)
                leftAnchor = leftHandAnchor.GetComponent<VrHandControllerAnchor>();
            if (rightHandAnchor != null)
                rightAnchor = rightHandAnchor.GetComponent<VrHandControllerAnchor>();
        }

        /// <summary>
        /// Ẩn visual nếu Quest chưa track tay. Simulator luôn hiện (InputDevices không thấy tay giả lập).
        /// </summary>
        void UpdateVisualActive(Transform visualRoot, HandSide side)
        {
            if (visualRoot == null)
                return;

            visualRoot.gameObject.SetActive(IsHandTracked(side));
        }
    }
}
