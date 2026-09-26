using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Event channel carrying a <c>float</c> payload.</summary>
    [CreateAssetMenu(fileName = "Evt_Float", menuName = "VoidMart/Events/Float")]
    public class FloatChannel : GameEventChannel<float> { }
}
