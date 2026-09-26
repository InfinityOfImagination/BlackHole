using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Event channel carrying a <c>string</c> payload.</summary>
    [CreateAssetMenu(fileName = "Evt_String", menuName = "VoidMart/Events/String")]
    public class StringChannel : GameEventChannel<string> { }
}
