using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VoidMart.Puzzle;

namespace VoidMart.UI
{
    /// <summary>
    /// Draggable cluster in the tray.  Lifts on pick-up, snaps its ghost to the board while over
    /// it, and reports the drop to the overlay.
    /// </summary>
    public class PuzzleTrayPiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [SerializeField] RectTransform m_Root;
        [SerializeField] Image m_Template;

        readonly List<Image> m_Blocks = new List<Image>(6);
        PuzzleOverlayUI m_Overlay;
        PuzzleShape m_Shape;
        Vector2 m_HomePosition;
        float m_CellSize = 64f;
        int m_Index;

        public PuzzleShape Shape => m_Shape;
        public int Index => m_Index;

        public void Setup(PuzzleOverlayUI overlay, int index, RectTransform root, Image template, float cellSize)
        {
            m_Overlay = overlay;
            m_Index = index;
            m_Root = root;
            m_Template = template;
            m_CellSize = cellSize;
            m_HomePosition = root != null ? root.anchoredPosition : Vector2.zero;
        }

        public void SetShape(PuzzleShape shape, Color color)
        {
            m_Shape = shape;
            EnsureBlocks(shape != null ? shape.cells.Count : 0);

            if (shape == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            float width = shape.Width;
            float height = shape.Height;
            for (int i = 0; i < m_Blocks.Count; i++)
            {
                bool used = i < shape.cells.Count;
                m_Blocks[i].gameObject.SetActive(used);
                if (!used) continue;
                var cell = shape.cells[i];
                var rect = m_Blocks[i].rectTransform;
                rect.sizeDelta = new Vector2(m_CellSize * 0.92f, m_CellSize * 0.92f);
                rect.anchoredPosition = new Vector2(
                    (cell.x - (width - 1) * 0.5f) * m_CellSize,
                    (cell.y - (height - 1) * 0.5f) * m_CellSize);
                m_Blocks[i].color = color;
            }
            if (m_Root != null) m_Root.anchoredPosition = m_HomePosition;
        }

        void EnsureBlocks(int count)
        {
            while (m_Blocks.Count < count && m_Template != null)
            {
                var instance = Instantiate(m_Template, m_Root != null ? m_Root : transform as RectTransform);
                instance.gameObject.SetActive(true);
                m_Blocks.Add(instance);
            }
        }

        public void OnPointerDown(PointerEventData eventData) { }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (m_Shape == null) return;
            if (m_Root != null) m_Root.localScale = Vector3.one * 1.12f;
            m_Overlay?.BeginDrag(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (m_Shape == null || m_Root == null) return;
            var parent = m_Root.parent as RectTransform;
            if (parent != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, eventData.position, eventData.pressEventCamera, out var local))
                m_Root.anchoredPosition = local + new Vector2(0f, 120f);
            m_Overlay?.UpdateDrag(this, eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (m_Root != null) m_Root.localScale = Vector3.one;
            m_Overlay?.EndDrag(this, eventData);
            if (m_Root != null) m_Root.anchoredPosition = m_HomePosition;
        }

        public void ResetPosition()
        {
            if (m_Root != null)
            {
                m_Root.anchoredPosition = m_HomePosition;
                m_Root.localScale = Vector3.one;
            }
        }
    }
}
