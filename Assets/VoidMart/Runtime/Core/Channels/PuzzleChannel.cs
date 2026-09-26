using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Event channel carrying a <c>PuzzleResult</c> payload.</summary>
    [CreateAssetMenu(fileName = "Evt_PuzzleResult", menuName = "VoidMart/Events/PuzzleResult")]
    public class PuzzleChannel : GameEventChannel<PuzzleResult> { }
}
