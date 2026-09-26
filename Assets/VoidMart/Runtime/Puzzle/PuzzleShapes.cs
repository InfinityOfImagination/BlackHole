using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.Puzzle
{
    /// <summary>One block cluster the player can drop onto the board.</summary>
    [Serializable]
    public class PuzzleShape
    {
        public string id = "shape";
        public List<Vector2Int> cells = new List<Vector2Int>();

        public int Width
        {
            get
            {
                int max = 0;
                for (int i = 0; i < cells.Count; i++) max = Mathf.Max(max, cells[i].x);
                return max + 1;
            }
        }

        public int Height
        {
            get
            {
                int max = 0;
                for (int i = 0; i < cells.Count; i++) max = Mathf.Max(max, cells[i].y);
                return max + 1;
            }
        }

        public static PuzzleShape Make(string id, params int[] xy)
        {
            var shape = new PuzzleShape { id = id };
            for (int i = 0; i + 1 < xy.Length; i += 2) shape.cells.Add(new Vector2Int(xy[i], xy[i + 1]));
            return shape;
        }
    }
}
