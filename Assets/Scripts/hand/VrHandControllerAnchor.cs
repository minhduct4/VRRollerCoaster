using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

namespace VRRoller
{
    /// <summary>
    /// Một anchor tay: đọc pose từ Input System XR controller, không ghi đè camera HMD.
    /// </summary>
    [DisallowMultipleComponent]
    public class VrHandControllerAnchor : MonoBehaviour
    {
        static readonly Vector3 LeftSimulatorRestLocal = new Vector3(-0.25f, -0.15f, 0.4f);
        static readonly Vector3 RightSimulatorRestLocal = new Vector3(0.25f, -0.15f, 0.4f);

        [SerializeField, Tooltip("Trái hoặc phải — chọn đúng binding XR.")]
        HandSide handSide = HandSide.Left;

        InputAction positionAction;
        InputAction rotationAction;
        InputAction isTrackedAction;

        /// <summary>Tay trái hay phải (T8.3+ dùng cho panel súng).</summary>
        public HandSide Side => handSide;

        /// <summary>
        /// Simulator dùng Input System, không đi qua UnityEngine.XR.InputDevices — Quest vẫn đọc được cả hai.
        /// </summary>
        public bool IsTracked
        {
            get
            {
                if (isTrackedAction != null && isTrackedAction.enabled && isTrackedAction.IsPressed())
                    return true;

                if (XrSimulatorProbe.IsActive())
                    return true;

                var node = handSide == HandSide.Left ? XRNode.LeftHand : XRNode.RightHand;
                var device = InputDevices.GetDeviceAtXRNode(node);
                return device.isValid
                       && device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out var tracked)
                       && tracked;
            }
        }

        void OnEnable()
        {
            BindTrackingActions();
            positionAction?.Enable();
            rotationAction?.Enable();
            isTrackedAction?.Enable();
        }

        void OnDisable()
        {
            positionAction?.Disable();
            rotationAction?.Disable();
            isTrackedAction?.Disable();
        }

        void OnDestroy()
        {
            positionAction?.Dispose();
            rotationAction?.Dispose();
            isTrackedAction?.Dispose();
        }

        /// <summary>
        /// Sau tracking — pose controller là <b>local</b> so với parent (giống TrackedPoseDriver trên Main Camera).
        /// </summary>
        void LateUpdate()
        {
            if (positionAction == null || rotationAction == null)
                return;

            var position = positionAction.ReadValue<Vector3>();
            var rotation = rotationAction.ReadValue<Quaternion>();
            if (position.sqrMagnitude < 0.0001f && XrSimulatorProbe.IsActive())
            {
                position = handSide == HandSide.Left ? LeftSimulatorRestLocal : RightSimulatorRestLocal;
                rotation = Quaternion.identity;
            }

            transform.SetLocalPositionAndRotation(position, rotation);
        }

        /// <summary>
        /// Khởi tạo action đọc devicePosition/deviceRotation/isTracked theo tay.
        /// </summary>
        void BindTrackingActions()
        {
            positionAction?.Dispose();
            rotationAction?.Dispose();
            isTrackedAction?.Dispose();

            var handTag = handSide == HandSide.Left ? "{LeftHand}" : "{RightHand}";
            positionAction = new InputAction(
                name: $"{handSide}HandPosition",
                type: InputActionType.Value,
                binding: $"<XRController>{handTag}/devicePosition");

            rotationAction = new InputAction(
                name: $"{handSide}HandRotation",
                type: InputActionType.Value,
                binding: $"<XRController>{handTag}/deviceRotation");

            isTrackedAction = new InputAction(
                name: $"{handSide}HandTracked",
                type: InputActionType.Button,
                binding: $"<XRController>{handTag}/isTracked");
        }

        /// <summary>
        /// Gán tay khi tạo anchor từ code hoặc menu Editor.
        /// </summary>
        public void Configure(HandSide side)
        {
            handSide = side;
            if (isActiveAndEnabled)
            {
                OnDisable();
                OnEnable();
            }
        }
    }
}
