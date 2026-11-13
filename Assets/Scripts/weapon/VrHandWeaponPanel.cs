using UnityEngine;
using UnityEngine.UI;

namespace VRRoller
{
    /// <summary>
    /// Panel world-space trên tay phải: tên súng + gợi ý A (T8.3). Sinh viên gắn Canvas thủ công theo Instruction.
    /// </summary>
    [DisallowMultipleComponent]
    public class VrHandWeaponPanel : MonoBehaviour
    {
        const string HintText = "A: đổi súng";

        [Header("UI — kéo từ Canvas con Right Hand")]
        [SerializeField, Tooltip("Root Canvas hoặc panel; tắt lúc đầu.")]
        GameObject panelRoot;

        [SerializeField, Tooltip("Text hiển thị tên loại súng.")]
        Text weaponNameLabel;

        [SerializeField, Tooltip("Text gợi ý nút A.")]
        Text hintLabel;

        [Header("Billboard")]
        [SerializeField, Tooltip("Xoay panel về phía camera HMD mỗi frame.")]
        bool faceMainCamera = true;

        [Header("References")]
        [SerializeField]
        VrHandWeaponLoadout weaponLoadout;

        [SerializeField]
        VrWeaponPanelInput panelInput;

        [SerializeField]
        VrHandControllerRig handRig;

        Camera targetCamera;
        bool isVisible;

        /// <summary>Panel đang mở (T8.4 chỉ đổi súng khi true).</summary>
        public bool IsPanelVisible => isVisible;

        void Awake()
        {
            if (weaponLoadout == null)
                weaponLoadout = GetComponent<VrHandWeaponLoadout>();
            if (handRig == null)
                handRig = GetComponent<VrHandControllerRig>();
            if (panelInput == null)
                panelInput = GetComponent<VrWeaponPanelInput>();

            SetPanelVisible(false);
        }

        void Start()
        {
            targetCamera = Camera.main;

            if (panelInput != null)
            {
                panelInput.TogglePanelPressed += OnTogglePanelPressed;
                panelInput.CycleWeaponPressed += OnCycleWeaponPressed;
            }

            if (hintLabel != null)
                hintLabel.text = HintText;

            RefreshWeaponLabel();
        }

        void OnDestroy()
        {
            if (panelInput != null)
            {
                panelInput.TogglePanelPressed -= OnTogglePanelPressed;
                panelInput.CycleWeaponPressed -= OnCycleWeaponPressed;
            }
        }

        void LateUpdate()
        {
            if (!faceMainCamera || !isVisible || panelRoot == null)
                return;

            if (targetCamera == null)
                targetCamera = Camera.main;

            if (targetCamera == null)
                return;

            var t = panelRoot.transform;
            var forward = t.position - targetCamera.transform.position;
            if (forward.sqrMagnitude > 0.0001f)
                t.rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        }

        /// <summary>
        /// B tay phải — đảo trạng thái hiển thị.
        /// </summary>
        void OnTogglePanelPressed()
        {
            SetPanelVisible(!isVisible);
            if (isVisible)
                RefreshWeaponLabel();
        }

        /// <summary>
        /// A tay phải — chỉ đổi súng khi panel đang mở (T8.4).
        /// </summary>
        void OnCycleWeaponPressed()
        {
            if (!isVisible || weaponLoadout == null)
                return;

            weaponLoadout.CycleWeaponNext();
            RefreshWeaponLabel();
        }

        /// <summary>
        /// Bật/tắt panel và cập nhật nhãn súng.
        /// </summary>
        public void SetPanelVisible(bool visible)
        {
            isVisible = visible;
            if (panelRoot != null)
                panelRoot.SetActive(visible);
        }

        /// <summary>
        /// Đọc tên từ loadout (T8.4 gọi sau khi đổi súng).
        /// </summary>
        public void RefreshWeaponLabel()
        {
            if (weaponNameLabel == null || weaponLoadout == null)
                return;

            weaponNameLabel.text = weaponLoadout.GetDisplayName(weaponLoadout.CurrentWeaponIndex);
        }
    }
}
