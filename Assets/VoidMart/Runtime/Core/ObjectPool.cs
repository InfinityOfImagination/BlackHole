using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.Core
{
    /// <summary>
    /// Pre-allocated GameObject pool.  The spec demands zero runtime allocations inside the
    /// gameplay loop, so every prop, bill, particle burst and floating label comes from here.
    /// </summary>
    public class ObjectPool
    {
        readonly GameObject m_Prefab;
        readonly Transform m_Parent;
        readonly Stack<GameObject> m_Idle;
        readonly List<GameObject> m_Live;
        readonly int m_MaxSize;

        public string Key { get; }
        public int LiveCount => m_Live.Count;
        public int IdleCount => m_Idle.Count;
        public GameObject Prefab => m_Prefab;

        public ObjectPool(string key, GameObject prefab, Transform parent, int prewarm, int maxSize = 512)
        {
            Key = key;
            m_Prefab = prefab;
            m_Parent = parent;
            m_MaxSize = Mathf.Max(1, maxSize);
            m_Idle = new Stack<GameObject>(Mathf.Max(4, prewarm));
            m_Live = new List<GameObject>(Mathf.Max(4, prewarm));
            Prewarm(prewarm);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var go = CreateInstance();
                go.SetActive(false);
                m_Idle.Push(go);
            }
        }

        GameObject CreateInstance()
        {
            var go = Object.Instantiate(m_Prefab, m_Parent);
            go.name = m_Prefab.name;
            return go;
        }

        public GameObject Spawn(Vector3 position, Quaternion rotation)
        {
            GameObject go = m_Idle.Count > 0 ? m_Idle.Pop() : CreateInstance();
            var t = go.transform;
            t.SetPositionAndRotation(position, rotation);
            go.SetActive(true);
            m_Live.Add(go);

            var handlers = ListPool<IPoolable>.Get();
            go.GetComponentsInChildren(true, handlers);
            for (int i = 0; i < handlers.Count; i++) handlers[i].OnSpawned();
            ListPool<IPoolable>.Release(handlers);
            return go;
        }

        public void Despawn(GameObject go)
        {
            if (go == null) return;
            int index = m_Live.IndexOf(go);
            if (index >= 0) m_Live.RemoveAt(index);

            var handlers = ListPool<IPoolable>.Get();
            go.GetComponentsInChildren(true, handlers);
            for (int i = 0; i < handlers.Count; i++) handlers[i].OnDespawned();
            ListPool<IPoolable>.Release(handlers);

            go.SetActive(false);
            if (m_Idle.Count < m_MaxSize)
            {
                go.transform.SetParent(m_Parent, false);
                m_Idle.Push(go);
            }
            else
            {
                Object.Destroy(go);
            }
        }

        public void DespawnAll()
        {
            for (int i = m_Live.Count - 1; i >= 0; i--) Despawn(m_Live[i]);
        }
    }

    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

    /// <summary>Tiny reusable List cache so helper queries never allocate mid-frame.</summary>
    public static class ListPool<T>
    {
        static readonly Stack<List<T>> s_Pool = new Stack<List<T>>(8);

        public static List<T> Get()
        {
            var list = s_Pool.Count > 0 ? s_Pool.Pop() : new List<T>(16);
            list.Clear();
            return list;
        }

        public static void Release(List<T> list)
        {
            if (list == null) return;
            list.Clear();
            if (s_Pool.Count < 16) s_Pool.Push(list);
        }
    }
}
