using UnityEngine;
using VoidMart.Core;

namespace VoidMart.Store
{
    /// <summary>Swaps the shopper's mesh on spawn so the queue is not a row of clones.</summary>
    public class CustomerVariant : MonoBehaviour, IPoolable
    {
        [SerializeField] MeshFilter m_Filter;
        [SerializeField] Mesh[] m_Meshes;

        public void OnSpawned()
        {
            if (m_Filter == null || m_Meshes == null || m_Meshes.Length == 0) return;
            var mesh = m_Meshes[Random.Range(0, m_Meshes.Length)];
            if (mesh != null) m_Filter.sharedMesh = mesh;
            transform.localScale = Vector3.one * Random.Range(0.92f, 1.08f);
        }

        public void OnDespawned() { }
    }
}
