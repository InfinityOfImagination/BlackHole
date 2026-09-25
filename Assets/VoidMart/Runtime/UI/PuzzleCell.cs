using UnityEngine;
using UnityEngine.UI;

namespace VoidMart.UI
{
    /// <summary>One square on the unjam board.</summary>
    public class PuzzleCell : MonoBehaviour
    {
        [SerializeField] Image m_Image;
        public int X { get; private set; }
        public int Y { get; private set; }
        public Image Image => m_Image;

        public void Setup(Image image, int x, int y)
        {
            m_Image = image;
            X = x;
            Y = y;
        }

        public void Paint(Color color) { if (m_Image != null) m_Image.color = color; }
    }
}
