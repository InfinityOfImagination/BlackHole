using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>Payload-free signal.</summary>
    [CreateAssetMenu(fileName = "Evt_Signal", menuName = "VoidMart/Events/Signal")]
    public class SignalChannel : GameEventChannelBase
    {
        readonly List<Action> m_Listeners = new List<Action>(8);

        public int ListenerCount => m_Listeners.Count;

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
}
