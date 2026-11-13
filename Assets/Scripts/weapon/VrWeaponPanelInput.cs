using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VRRoller
{
    /// <summary>
    /// Input map Weapon — nút B tay phải (T8.3) mở/tắt panel; không dùng chung Start/Reset.
    /// </summary>
    [DisallowMultipleComponent]
    public class VrWeaponPanelInput : MonoBehaviour
    {
        const string MapName = "Weapon";
        const string ToggleActionName = "TogglePanel";
        const string CycleActionName = "CycleWeapon";

        [SerializeField, Tooltip("Assets/Settings/VRRoller.inputactions — map Weapon.")]
        InputActionAsset inputActions;

        InputAction togglePanelAction;
        InputAction cycleWeaponAction;

        /// <summary>B (tay phải) — toggle panel chọn súng.</summary>
        public event Action TogglePanelPressed;

        /// <summary>A (tay phải) — xoay loại súng (T8.4).</summary>
        public event Action CycleWeaponPressed;

        void Awake()
        {
            if (!TryCacheActions())
                enabled = false;
        }

        void OnEnable()
        {
            if (togglePanelAction == null)
                return;

            togglePanelAction.performed += OnTogglePerformed;
            togglePanelAction.Enable();

            if (cycleWeaponAction != null)
            {
                cycleWeaponAction.performed += OnCyclePerformed;
                cycleWeaponAction.Enable();
            }
        }

        void OnDisable()
        {
            if (togglePanelAction == null)
                return;

            togglePanelAction.performed -= OnTogglePerformed;
            togglePanelAction.Disable();

            if (cycleWeaponAction != null)
            {
                cycleWeaponAction.performed -= OnCyclePerformed;
                cycleWeaponAction.Disable();
            }
        }

        void OnTogglePerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            TogglePanelPressed?.Invoke();
        }

        void OnCyclePerformed(InputAction.CallbackContext context)
        {
            if (!context.performed)
                return;

            CycleWeaponPressed?.Invoke();
        }

        /// <summary>
        /// Lấy action TogglePanel / CycleWeapon từ asset.
        /// </summary>
        bool TryCacheActions()
        {
            if (inputActions == null)
            {
                Debug.LogError($"{nameof(VrWeaponPanelInput)}: gán Input Action Asset VRRoller.", this);
                return false;
            }

            var map = inputActions.FindActionMap(MapName, true);
            togglePanelAction = map.FindAction(ToggleActionName, true);
            cycleWeaponAction = map.FindAction(CycleActionName, true);
            return togglePanelAction != null && cycleWeaponAction != null;
        }
    }
}
