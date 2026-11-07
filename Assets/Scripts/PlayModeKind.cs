namespace VRRoller
{
    /// <summary>
    /// Chế độ chơi do Mode Manager chọn lúc khởi động (mở rộng sau khi bật/tắt rig).
    /// </summary>
    public enum PlayModeKind
    {
        /// <summary>Không có phiên XR — dùng rig First-Person (PC không headset).</summary>
        FirstPerson,

        /// <summary>Có XR thật (Quest/Link) hoặc XR Device Simulator — cùng rig VR.</summary>
        Vr,
    }
}
