using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Trạng thái chờ Start / đang chạy / reset về đầu ray (T5.1–T5.3).
    /// </summary>
    public static class CartRideLifecycle
    {
        /// <summary>
        /// Đặt lại tham số t và trạng thái chờ — toa đứng ở ga (t = 0).
        /// </summary>
        public static void EnterWaitingAtStart(ref float normalizedTime, ref bool isRunning)
        {
            normalizedTime = 0f;
            isRunning = false;
        }

        /// <summary>
        /// Khởi động chuyến đi sau Start (chỉ khi đang chờ).
        /// </summary>
        /// <returns>True nếu chuyển sang đang chạy.</returns>
        public static bool TryBeginRide(ref bool isRunning)
        {
            if (isRunning)
                return false;

            isRunning = true;
            return true;
        }
    }
}
