using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Event channel carrying a <c>double</c> payload.</summary>
    [CreateAssetMenu(fileName = "Evt_Double", menuName = "VoidMart/Events/Double")]
    public class DoubleChannel : GameEventChannel<double> { }
}
