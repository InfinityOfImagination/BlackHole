using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Event channel carrying a <c>int</c> payload.</summary>
    [CreateAssetMenu(fileName = "Evt_Int", menuName = "VoidMart/Events/Int")]
    public class IntChannel : GameEventChannel<int> { }
}
