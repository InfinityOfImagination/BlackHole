using UnityEngine;

namespace VoidMart.Gameplay
{
    /// <summary>
    /// One-finger control surface.  The on-screen joystick (a uGUI widget, so it works with either
    /// input backend) writes here and the hole controller reads it - no direct coupling.
    /// </summary>
    public static class PlayerInputRouter
    {
        public static Vector2 MoveAxis { get; private set; }
        public static bool HasInput { get; private set; }

        public static void SetMove(Vector2 axis)
        {
            MoveAxis = Vector2.ClampMagnitude(axis, 1f);
            HasInput = MoveAxis.sqrMagnitude > 0.0001f;
        }

        public static void Clear()
        {
            MoveAxis = Vector2.zero;
            HasInput = false;
        }
    }
}
