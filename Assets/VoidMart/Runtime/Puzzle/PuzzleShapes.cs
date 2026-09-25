using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.Puzzle
{
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

    /// <summary>
    /// The tray's vocabulary of block clusters.  Kept as a ScriptableObject so the Master Tool can
    /// add, remove or reweight shapes without touching code.
    /// </summary>
    [CreateAssetMenu(fileName = "PuzzlePieceSet", menuName = "VoidMart/Puzzle Piece Set")]
    public class PuzzlePieceSet : ScriptableObject
    {
        public List<PuzzleShape> shapes = new List<PuzzleShape>();

        public PuzzleShape Random(System.Random random) =>
            shapes.Count == 0 ? DefaultShapes()[0] : shapes[random.Next(0, shapes.Count)];

        public void ResetToDefaults()
        {
            shapes.Clear();
            shapes.AddRange(DefaultShapes());
        }

        public static List<PuzzleShape> DefaultShapes() => new List<PuzzleShape>
        {
            PuzzleShape.Make("dot", 0, 0),
            PuzzleShape.Make("duo_h", 0, 0, 1, 0),
            PuzzleShape.Make("duo_v", 0, 0, 0, 1),
            PuzzleShape.Make("tri_h", 0, 0, 1, 0, 2, 0),
            PuzzleShape.Make("tri_v", 0, 0, 0, 1, 0, 2),
            PuzzleShape.Make("square", 0, 0, 1, 0, 0, 1, 1, 1),
            PuzzleShape.Make("corner", 0, 0, 1, 0, 0, 1),
            PuzzleShape.Make("corner_b", 0, 0, 1, 0, 1, 1),
            PuzzleShape.Make("ell", 0, 0, 0, 1, 0, 2, 1, 2),
            PuzzleShape.Make("tee", 0, 0, 1, 0, 2, 0, 1, 1),
            PuzzleShape.Make("ess", 1, 0, 2, 0, 0, 1, 1, 1)
        };
    }
}
