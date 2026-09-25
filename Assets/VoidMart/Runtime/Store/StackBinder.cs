using UnityEngine;
using VoidMart.Core;
using VoidMart.Data;

namespace VoidMart.Store
{
    /// <summary>
    /// Builds a <see cref="StackVisual"/>'s renderers from generated asset keys at load time,
    /// so prefabs reference art by name instead of holding hard links to meshes.
    /// </summary>
    [RequireComponent(typeof(StackVisual))]
    public class StackBinder : MonoBehaviour
    {
        [SerializeField] string m_MeshKey = "mesh_product_box";
        [SerializeField] string m_MaterialKey = "mat_clay";
        [SerializeField] int m_MaxVisible = 12;
        [SerializeField] Vector3 m_Cell = new Vector3(0.42f, 0.34f, 0.42f);
        [SerializeField] int m_Columns = 2;
        [SerializeField] int m_Rows = 2;
        [SerializeField] float m_ItemScale = 1f;

        void Awake()
        {
            var stack = GetComponent<StackVisual>();
            if (stack == null) return;

            var assets = ServiceLocator.Get<GameAssets>();
            if (assets == null && ServiceInstaller.Active != null && ServiceInstaller.Active.Config != null)
                assets = ServiceInstaller.Active.Config.assets;
            if (assets == null) return;

            stack.Build(assets.GetMesh(m_MeshKey), assets.GetMaterial(m_MaterialKey),
                m_MaxVisible, m_Cell, m_Columns, m_Rows, m_ItemScale);
        }

        public void Configure(string meshKey, string materialKey, int maxVisible, Vector3 cell, int columns, int rows, float itemScale)
        {
            m_MeshKey = meshKey;
            m_MaterialKey = materialKey;
            m_MaxVisible = maxVisible;
            m_Cell = cell;
            m_Columns = columns;
            m_Rows = rows;
            m_ItemScale = itemScale;
        }
    }
}
