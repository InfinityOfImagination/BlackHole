using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Event channel carrying a <c>SaleInfo</c> payload.</summary>
    [CreateAssetMenu(fileName = "Evt_Sale", menuName = "VoidMart/Events/Sale")]
    public class SaleChannel : GameEventChannel<SaleInfo> { }
}
