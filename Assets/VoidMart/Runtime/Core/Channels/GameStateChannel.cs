using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Event channel carrying a <c>GameState</c> payload.</summary>
    [CreateAssetMenu(fileName = "Evt_GameState", menuName = "VoidMart/Events/GameState")]
    public class GameStateChannel : GameEventChannel<GameState> { }
}
