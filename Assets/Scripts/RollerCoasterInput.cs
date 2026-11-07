using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRRoller
{
    /// <summary>
    /// Map <b>Roller</b> — Start / Reset (T4.4, T8.5). Quest: chỉ <b>tay trái</b> A/B + Enter/Backspace PC.
    /// Tay phải A/B dùng map <b>Weapon</b> (<see cref="VrWeaponPanelInput"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public class RollerCoasterInput : MonoBehaviour
    {
        const string MapName = "Roller";
        const string StartActionName = "Start";
        const string ResetActionName = "Reset";

        [SerializeField, Tooltip("Assets/Settings/VRRoller.inputactions — map Roller.")]
        InputActionAsset inputActions;

        InputAction m_startAction;
        InputAction m_resetAction;

        /// <summary>Bấm Start (Enter / nút A tay cầm / simulator).</summary>
        public event Action StartPressed;

        /// <summary>Bấm Reset (Backspace / nút B).</summary>
        public event Action ResetPressed;

        void Awake()
        {
            if (!TryCacheActions())
                enabled = false;
        }

        void OnEnable()
        {
            if (m_startAction == null)
                return;

            m_startAction.performed += OnStartPerformed;
            m_resetAction.performed += OnResetPerformed;
            m_startAction.Enable();
            m_resetAction.Enable();
        }

        void OnDisable()
        {
            if (m_startAction == null)
                return;

            m_startAction.performed -= OnStartPerformed;
            m_resetAction.performed -= OnResetPerformed;
            m_startAction.Disable();
            m_resetAction.Disable();
        }

        /// <summary>
        /// Log + event cho CartRideController.
        /// </summary>
        void OnStartPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            Debug.Log("Roller: Start");
            StartPressed?.Invoke();
        }

        /// <summary>
        /// Log + event reset về ga.
        /// </summary>
        void OnResetPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            Debug.Log("Roller: Reset");
            ResetPressed?.Invoke();
        }

        /// <summary>
        /// Lấy action Start/Reset từ map Roller; báo lỗi nếu thiếu asset hoặc tên action.
        /// </summary>
        bool TryCacheActions()
        {
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(RollerCoasterInput)}: gán Input Action Asset VRRoller.", this);
                return false;
            }

            var map = inputActions.FindActionMap(MapName, true);
            m_startAction = map.FindAction(StartActionName, true);
            m_resetAction = map.FindAction(ResetActionName, true);
            return true;
        }
    }
}
