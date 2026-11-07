using UnityEngine;
using UnityEngine.InputSystem;

namespace VRRoller
{
    /// <summary>
    /// Đọc chuột / phím cho góc nhìn First-Person qua Input System (không dùng Input Manager cũ).
    /// </summary>
    public static class FirstPersonLookInput
    {
        /// <summary>
        /// Delta chuột frame này (pixel). Zero nếu không có chuột.
        /// </summary>
        public static Vector2 ReadMouseDelta()
        {
            if (Mouse.current == null)
                return Vector2.zero;

            return Mouse.current.delta.ReadValue();
        }

        /// <summary>
        /// Escape vừa nhấn — dùng để thả khóa con trỏ khi test trên PC.
        /// </summary>
        public static bool WasEscapePressedThisFrame()
        {
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        }

        /// <summary>
        /// Click trái vừa nhấn — khóa lại con trỏ sau khi đã thả Escape.
        /// </summary>
        public static bool WasPrimaryClickPressedThisFrame()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        /// <summary>
        /// Khóa hoặc thả con trỏ (Game view First-Person).
        /// </summary>
        public static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
