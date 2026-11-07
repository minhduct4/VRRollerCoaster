using UnityEngine;

namespace VRRoller
{
    /// <summary>
    /// Nối RollerCoasterInput → TeachingCartMoverTask5: Start chạy ray, Reset về ga (cùng Cart).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CartMover))]
    [RequireComponent(typeof(RollerCoasterInput))]
    public class CartRideController : MonoBehaviour
    {
        CartMover cartMover;
        RollerCoasterInput rollerInput;

        void Awake()
        {
            cartMover = GetComponent<CartMover>();
            rollerInput = GetComponent<RollerCoasterInput>();
        }

        void OnEnable()
        {
            rollerInput.StartPressed += HandleStart;
            rollerInput.ResetPressed += HandleReset;
        }

        void OnDisable()
        {
            if (rollerInput == null)
                return;

            rollerInput.StartPressed -= HandleStart;
            rollerInput.ResetPressed -= HandleReset;
        }

        /// <summary>
        /// Start: bắt đầu chạy nếu đang chờ ở ga.
        /// </summary>
        void HandleStart()
        {
            cartMover.TryBeginRide();
        }

        /// <summary>
        /// Reset: dừng và đưa toa về đầu ray, chờ Start lại.
        /// </summary>
        void HandleReset()
        {
            cartMover.ResetToStartAndWait();
        }
    }
}
