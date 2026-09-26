using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Event channel carrying a <c>ToastRequest</c> payload.</summary>
    [CreateAssetMenu(fileName = "Evt_Toast", menuName = "VoidMart/Events/Toast")]
    public class ToastChannel : GameEventChannel<ToastRequest> { }
}
