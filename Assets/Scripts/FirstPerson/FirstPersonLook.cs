using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Gắn trên FirstPersonRig: chuột đổi yaw/pitch, kẹp góc ngẩng/cúi. Chỉ xoay local — không ghi đè vị trí camera.
    /// </summary>
    [DisallowMultipleComponent]
    public class FirstPersonLook : MonoBehaviour
    {
        [Header("Pivot")]
        [SerializeField, Tooltip("Transform xoay yaw/pitch. Để trống = object này (FirstPersonRig).")]
        Transform lookPivot;

        [Header("Chuột")]
        [SerializeField, Range(0.02f, 0.5f), Tooltip("Độ/pixel delta chuột.")]
        float sensitivity = 0.12f;

        [SerializeField, Tooltip("Bật nếu muốn chuột lên thì cúi xuống.")]
        bool invertY;

        [SerializeField, Tooltip("Khóa con trỏ khi Play. Escape thả; click trái khóa lại.")]
        bool lockCursorOnEnable = true;

        [Header("Giới hạn góc (độ)")]
        [SerializeField, Range(-89f, 0f), Tooltip("Góc ngẩng tối đa (âm = nhìn lên).")]
        float minPitch = -80f;

        [SerializeField, Range(0f, 89f), Tooltip("Góc cúi tối đa (dương = nhìn xuống).")]
        float maxPitch = 80f;

        float yawDegrees;
        float pitchDegrees;

        void Awake()
        {
            if (lookPivot == null)
                lookPivot = transform;
        }

        void OnEnable()
        {
            if (lockCursorOnEnable)
                FirstPersonLookInput.SetCursorLocked(true);
        }

        void OnDisable()
        {
            FirstPersonLookInput.SetCursorLocked(false);
        }

        void LateUpdate()
        {
            if (lookPivot == null)
                return;

            if (!TryKeepCursorPolicy())
                return;

            var delta = FirstPersonLookInput.ReadMouseDelta();
            FirstPersonLookAngles.Accumulate(
                ref yawDegrees,
                ref pitchDegrees,
                delta,
                sensitivity,
                invertY,
                minPitch,
                maxPitch);

            // Local only: CartMover vẫn là nơi duy nhất đặt world pose của thân.
            lookPivot.localRotation = FirstPersonLookAngles.ToLocalRotation(yawDegrees, pitchDegrees);
        }

        /// <summary>
        /// Escape thả chuột; click trái khóa lại. Trả về false khi chưa khóa — không xoay camera.
        /// </summary>
        bool TryKeepCursorPolicy()
        {
            if (!lockCursorOnEnable)
                return true;

            if (FirstPersonLookInput.WasEscapePressedThisFrame())
                FirstPersonLookInput.SetCursorLocked(false);

            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (FirstPersonLookInput.WasPrimaryClickPressedThisFrame())
                    FirstPersonLookInput.SetCursorLocked(true);
                else
                    return false;
            }

            return true;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Giữ minPitch ≤ maxPitch khi kéo slider trên Inspector.
        /// </summary>
        void OnValidate()
        {
            if (!VrCameraPolicy.IsValidFirstPersonLookHost(transform, out var hostReason))
                Debug.LogError($"{nameof(FirstPersonLook)}: {hostReason}", this);

            if (maxPitch < minPitch)
                maxPitch = minPitch;
        }
#endif
    }
}
