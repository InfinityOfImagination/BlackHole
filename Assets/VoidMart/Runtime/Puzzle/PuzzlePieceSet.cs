using System.Collections.Generic;
using UnityEngine;

namespace VoidMart.Puzzle
{
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
