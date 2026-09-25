using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>
    /// Base class for the ScriptableObject event bus described in the tech spec.  Systems raise
    /// and listen through these assets, so the UI / audio layers never hold a hard reference to a
    /// gameplay controller.  Listener lists are pre-sized and iterated backwards, which keeps the
    /// hot path free of allocations and safe against un-registration during dispatch.
    /// </summary>
    public abstract class GameEventChannelBase : ScriptableObject
    {
        [TextArea(2, 4)]
        public string description;
    }

    public abstract class GameEventChannel<T> : GameEventChannelBase
    {
        readonly List<Action<T>> m_Listeners = new List<Action<T>>(8);

        public int ListenerCount => m_Listeners.Count;

        public void Register(Action<T> listener)
        {
            if (listener == null || m_Listeners.Contains(listener)) return;
            m_Listeners.Add(listener);
        }

        public void Unregister(Action<T> listener)
        {
            if (listener == null) return;
            m_Listeners.Remove(listener);
        }

        public void Raise(T payload)
        {
            for (int i = m_Listeners.Count - 1; i >= 0; i--)
            {
                var listener = m_Listeners[i];
                if (listener == null) { m_Listeners.RemoveAt(i); continue; }
                try { listener(payload); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        protected virtual void OnDisable() => m_Listeners.Clear();
    }

    /// <summary>Payload-free signal.</summary>
    [CreateAssetMenu(fileName = "Evt_Signal", menuName = "VoidMart/Events/Signal")]
    public class SignalChannel : GameEventChannelBase
    {
        readonly List<Action> m_Listeners = new List<Action>(8);

        public void Register(Action listener)
        {
            if (listener == null || m_Listeners.Contains(listener)) return;
            m_Listeners.Add(listener);
        }

        public void Unregister(Action listener)
        {
            if (listener == null) return;
            m_Listeners.Remove(listener);
        }

        public void Raise()
        {
            for (int i = m_Listeners.Count - 1; i >= 0; i--)
            {
                var listener = m_Listeners[i];
                if (listener == null) { m_Listeners.RemoveAt(i); continue; }
                try { listener(); }
                catch (Exception e) { Debug.LogException(e, this); }
            }
        }

        void OnDisable() => m_Listeners.Clear();
    }

    [CreateAssetMenu(fileName = "Evt_Int", menuName = "VoidMart/Events/Int")]
    public class IntChannel : GameEventChannel<int> { }

    [CreateAssetMenu(fileName = "Evt_Float", menuName = "VoidMart/Events/Float")]
    public class FloatChannel : GameEventChannel<float> { }

    [CreateAssetMenu(fileName = "Evt_Double", menuName = "VoidMart/Events/Double")]
    public class DoubleChannel : GameEventChannel<double> { }

    [CreateAssetMenu(fileName = "Evt_String", menuName = "VoidMart/Events/String")]
    public class StringChannel : GameEventChannel<string> { }

    [CreateAssetMenu(fileName = "Evt_Swallow", menuName = "VoidMart/Events/Swallow")]
    public class SwallowChannel : GameEventChannel<SwallowInfo> { }

    [CreateAssetMenu(fileName = "Evt_Sale", menuName = "VoidMart/Events/Sale")]
    public class SaleChannel : GameEventChannel<SaleInfo> { }

    [CreateAssetMenu(fileName = "Evt_Puzzle", menuName = "VoidMart/Events/PuzzleResult")]
    public class PuzzleChannel : GameEventChannel<PuzzleResult> { }

    [CreateAssetMenu(fileName = "Evt_State", menuName = "VoidMart/Events/GameState")]
    public class GameStateChannel : GameEventChannel<GameState> { }

    [CreateAssetMenu(fileName = "Evt_Toast", menuName = "VoidMart/Events/Toast")]
    public class ToastChannel : GameEventChannel<ToastRequest> { }

    // ---------------------------------------------------------------- payloads

    public struct SwallowInfo
    {
        public float mass;
        public double value;
        public int tier;
        public int streak;
        public Vector3 worldPosition;
    }

    public struct SaleInfo
    {
        public int productIndex;
        public int quantity;
        public double revenue;
        public Vector3 worldPosition;
    }

    public struct PuzzleResult
    {
        public bool solved;
        public int linesCleared;
        public int blocksPlaced;
        public double reward;
        public string machineId;
    }

    public struct ToastRequest
    {
        public string message;
        public ToastStyle style;

        public ToastRequest(string message, ToastStyle style = ToastStyle.Info)
        {
            this.message = message;
            this.style = style;
        }
    }

    public enum ToastStyle { Info, Success, Warning, Reward }

    /// <summary>High level lifecycle states coordinated by <see cref="GameManager"/>.</summary>
    public enum GameState
    {
        Boot,
        Splash,
        Gathering,   // player is outside eating the city
        Unloading,   // player is emptying the hole into a hopper
        Storefront,  // player is inside running the shop
        Puzzle,      // block-sort overlay is up
        Paused
    }

    /// <summary>Which half of the world the player currently occupies (drives music + palette).</summary>
    public enum WorldArea { Street = 0, Store = 1 }
}
