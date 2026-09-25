using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.Puzzle
{
    /// <summary>Pure model for the block-sorting board — no scene objects, fully unit-testable.</summary>
    public class PuzzleGrid
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        int[] m_Cells;              // 0 = empty, otherwise colour index + 1
        readonly List<int> m_FullRows = new List<int>(8);
        readonly List<int> m_FullColumns = new List<int>(8);

        public IReadOnlyList<int> LastFullRows => m_FullRows;
        public IReadOnlyList<int> LastFullColumns => m_FullColumns;

        public void Resize(int width, int height)
        {
            Width = Mathf.Max(2, width);
            Height = Mathf.Max(2, height);
            m_Cells = new int[Width * Height];
        }

        public void Clear()
        {
            if (m_Cells == null) return;
            for (int i = 0; i < m_Cells.Length; i++) m_Cells[i] = 0;
        }

        public bool InBounds(int x, int y) => x >= 0 && y >= 0 && x < Width && y < Height;

        public int Get(int x, int y) => InBounds(x, y) ? m_Cells[y * Width + x] : -1;

        public void Set(int x, int y, int value)
        {
            if (InBounds(x, y)) m_Cells[y * Width + x] = value;
        }

        public bool IsOccupied(int x, int y) => Get(x, y) > 0;

        public bool CanPlace(PuzzleShape shape, Vector2Int origin)
        {
            if (shape == null) return false;
            for (int i = 0; i < shape.cells.Count; i++)
            {
                var cell = shape.cells[i] + origin;
                if (!InBounds(cell.x, cell.y) || IsOccupied(cell.x, cell.y)) return false;
            }
            return true;
        }

        public bool Place(PuzzleShape shape, Vector2Int origin, int colourIndex)
        {
            if (!CanPlace(shape, origin)) return false;
            for (int i = 0; i < shape.cells.Count; i++)
            {
                var cell = shape.cells[i] + origin;
                Set(cell.x, cell.y, colourIndex + 1);
            }
            return true;
        }

        /// <summary>Clears every completed row and column, returning how many lines went.</summary>
        public int ResolveLines()
        {
            m_FullRows.Clear();
            m_FullColumns.Clear();

            for (int y = 0; y < Height; y++)
            {
                bool full = true;
                for (int x = 0; x < Width && full; x++) full &= IsOccupied(x, y);
                if (full) m_FullRows.Add(y);
            }

            for (int x = 0; x < Width; x++)
            {
                bool full = true;
                for (int y = 0; y < Height && full; y++) full &= IsOccupied(x, y);
                if (full) m_FullColumns.Add(x);
            }

            for (int i = 0; i < m_FullRows.Count; i++)
                for (int x = 0; x < Width; x++) Set(x, m_FullRows[i], 0);

            for (int i = 0; i < m_FullColumns.Count; i++)
                for (int y = 0; y < Height; y++) Set(m_FullColumns[i], y, 0);

            return m_FullRows.Count + m_FullColumns.Count;
        }

        public bool AnyPlacementExists(IReadOnlyList<PuzzleShape> shapes)
        {
            for (int s = 0; s < shapes.Count; s++)
            {
                var shape = shapes[s];
                if (shape == null) continue;
                for (int y = 0; y < Height; y++)
                    for (int x = 0; x < Width; x++)
                        if (CanPlace(shape, new Vector2Int(x, y))) return true;
            }
            return false;
        }
    }
}
