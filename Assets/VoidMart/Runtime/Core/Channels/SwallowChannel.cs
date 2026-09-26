using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Event channel carrying a <c>SwallowInfo</c> payload.</summary>
    [CreateAssetMenu(fileName = "Evt_Swallow", menuName = "VoidMart/Events/Swallow")]
    public class SwallowChannel : GameEventChannel<SwallowInfo> { }
}
