using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VoidMart.Core
{
    /// <summary>Owns every <see cref="ObjectPool"/> in the running scene.</summary>
    public class PoolService : MonoBehaviour
    {
        readonly Dictionary<string, ObjectPool> m_Pools = new Dictionary<string, ObjectPool>(32);
        readonly Dictionary<int, ObjectPool> m_Owners = new Dictionary<int, ObjectPool>(512);
        Transform m_Root;

        void Awake()
        {
            m_Root = transform;
            ServiceLocator.Register(this);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (ServiceLocator.Get<PoolService>() == this) ServiceLocator.Unregister<PoolService>();
        }

        /// <summary>
        /// The pool survives scene loads with the service installer, so anything still live from
        /// the previous scene has to go back in the bag before the new one repopulates it.
        /// </summary>
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single) DespawnAll();
        }

        public ObjectPool Register(string key, GameObject prefab, int prewarm, int maxSize = 512)
        {
            if (prefab == null)
            {
                Debug.LogWarning($"[VoidMart] Pool '{key}' has no prefab.");
                return null;
            }
            if (m_Pools.TryGetValue(key, out var existing)) return existing;

            var holder = new GameObject($"Pool [{key}]").transform;
            holder.SetParent(m_Root, false);
            var pool = new ObjectPool(key, prefab, holder, prewarm, maxSize);
            m_Pools.Add(key, pool);
            return pool;
        }

        public bool TryGetPool(string key, out ObjectPool pool) => m_Pools.TryGetValue(key, out pool);

        public GameObject Spawn(string key, Vector3 position, Quaternion rotation)
        {
            if (!m_Pools.TryGetValue(key, out var pool)) return null;
            var go = pool.Spawn(position, rotation);
            m_Owners[go.GetInstanceID()] = pool;
            return go;
        }

        public void Despawn(GameObject go)
        {
            if (go == null) return;
            int id = go.GetInstanceID();
            if (m_Owners.TryGetValue(id, out var pool))
            {
                m_Owners.Remove(id);
                pool.Despawn(go);
            }
            else
            {
                Destroy(go);
            }
        }

        public void DespawnAll()
        {
            foreach (var kv in m_Pools) kv.Value.DespawnAll();
            m_Owners.Clear();
        }

        public IEnumerable<KeyValuePair<string, ObjectPool>> Pools => m_Pools;
    }
}
