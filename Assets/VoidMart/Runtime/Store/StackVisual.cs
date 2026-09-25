using UnityEngine;

namespace VoidMart.Store
{
    /// <summary>
    /// Physical stack of goods (or cash) that grows and shrinks with a count.  Pre-instantiates a
    /// fixed pool of child renderers at build time so nothing allocates while the store runs.
    /// </summary>
    public class StackVisual : MonoBehaviour
    {
        [SerializeField] Vector3 m_Cell = new Vector3(0.42f, 0.3f, 0.42f);
        [SerializeField] int m_Columns = 2;
        [SerializeField] int m_Rows = 2;
        [SerializeField] int m_MaxVisible = 24;
        [SerializeField] float m_PopSeconds = 0.18f;

        Transform[] m_Items;
        float[] m_PopTimers;
        Vector3 m_ItemScale = Vector3.one;
        int m_Count;

        public int MaxVisible => m_MaxVisible;
        public int Count => m_Count;

        public void Build(Mesh mesh, Material material, int maxVisible, Vector3 cell, int columns, int rows, float itemScale = 1f)
        {
            Clear();
            m_MaxVisible = Mathf.Max(1, maxVisible);
            m_Cell = cell;
            m_Columns = Mathf.Max(1, columns);
            m_Rows = Mathf.Max(1, rows);
            m_ItemScale = Vector3.one * itemScale;
            m_Items = new Transform[m_MaxVisible];
            m_PopTimers = new float[m_MaxVisible];

            for (int i = 0; i < m_MaxVisible; i++)
            {
                var go = new GameObject("Item " + i);
                go.transform.SetParent(transform, false);
                go.transform.localPosition = SlotPosition(i);
                go.transform.localScale = m_ItemScale;
                if (mesh != null)
                {
                    var filter = go.AddComponent<MeshFilter>();
                    filter.sharedMesh = mesh;
                    var renderer = go.AddComponent<MeshRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                go.SetActive(false);
                m_Items[i] = go.transform;
            }
            m_Count = 0;
        }

        public void Clear()
        {
            if (m_Items == null) return;
            for (int i = 0; i < m_Items.Length; i++)
                if (m_Items[i] != null) Destroy(m_Items[i].gameObject);
            m_Items = null;
            m_PopTimers = null;
            m_Count = 0;
        }

        public Vector3 SlotPosition(int index)
        {
            int perLayer = m_Columns * m_Rows;
            int layer = index / perLayer;
            int inLayer = index % perLayer;
            int column = inLayer % m_Columns;
            int row = inLayer / m_Columns;
            float offsetX = (m_Columns - 1) * 0.5f;
            float offsetZ = (m_Rows - 1) * 0.5f;
            float jitter = ((index * 37) % 7 - 3) * 0.006f;
            return new Vector3((column - offsetX) * m_Cell.x + jitter, layer * m_Cell.y, (row - offsetZ) * m_Cell.z);
        }

        public Vector3 WorldSlotPosition(int index) => transform.TransformPoint(SlotPosition(Mathf.Clamp(index, 0, m_MaxVisible - 1)));

        public Vector3 TopWorldPosition() => WorldSlotPosition(Mathf.Clamp(m_Count, 0, m_MaxVisible - 1));

        public void SetCount(int count)
        {
            if (m_Items == null) return;
            count = Mathf.Clamp(count, 0, m_MaxVisible);
            if (count == m_Count) return;

            for (int i = 0; i < m_Items.Length; i++)
            {
                bool shouldBeActive = i < count;
                if (shouldBeActive && !m_Items[i].gameObject.activeSelf && i >= m_Count)
                    m_PopTimers[i] = m_PopSeconds;
                m_Items[i].gameObject.SetActive(shouldBeActive);
            }
            m_Count = count;
        }

        void Update()
        {
            if (m_Items == null) return;
            for (int i = 0; i < m_Items.Length; i++)
            {
                if (m_PopTimers[i] <= 0f) continue;
                m_PopTimers[i] = Mathf.Max(0f, m_PopTimers[i] - Time.deltaTime);
                float t = 1f - m_PopTimers[i] / m_PopSeconds;
                float scale = Mathf.Lerp(0.4f, 1f, Core.Easing.Evaluate(Core.Ease.OutBack, t));
                m_Items[i].localScale = m_ItemScale * scale;
            }
        }
    }
}
