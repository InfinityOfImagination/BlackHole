using UnityEngine;
#if VOIDMART_INPUTSYSTEM
using UnityEngine.InputSystem;
#endif

namespace VoidMart.Core
{
    /// <summary>
    /// Thin shim over whichever input backend the project is configured with.  Gameplay drag
    /// handling goes through the uGUI EventSystem (backend agnostic); this exists for the few
    /// places that must poll raw touches, such as the debug tweaker's 3-finger gesture.
    /// </summary>
    public static class InputProxy
    {
        public static int TouchCount
        {
            get
            {
#if VOIDMART_INPUTSYSTEM
                var screen = Touchscreen.current;
                if (screen == null) return 0;
                int count = 0;
                var touches = screen.touches;
                for (int i = 0; i < touches.Count; i++)
                    if (touches[i].press.isPressed) count++;
                return count;
#else
                return Input.touchCount;
#endif
            }
        }

        public static Vector2 PointerPosition
        {
            get
            {
#if VOIDMART_INPUTSYSTEM
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
                    return Touchscreen.current.primaryTouch.position.ReadValue();
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                if (Input.touchCount > 0) return Input.GetTouch(0).position;
                return Input.mousePosition;
#endif
            }
        }

        public static bool PointerPressed
        {
            get
            {
#if VOIDMART_INPUTSYSTEM
                if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
                return Mouse.current != null && Mouse.current.leftButton.isPressed;
#else
                return Input.GetMouseButton(0) || Input.touchCount > 0;
#endif
            }
        }

        /// <summary>Editor-only convenience keys (F1 opens the tweaker, F5 saves).</summary>
        public static bool FunctionKeyDown(int number)
        {
#if VOIDMART_INPUTSYSTEM
            var keyboard = Keyboard.current;
            if (keyboard == null) return false;
            switch (number)
            {
                case 1: return keyboard.f1Key.wasPressedThisFrame;
                case 2: return keyboard.f2Key.wasPressedThisFrame;
                case 5: return keyboard.f5Key.wasPressedThisFrame;
                default: return false;
            }
#else
            switch (number)
            {
                case 1: return Input.GetKeyDown(KeyCode.F1);
                case 2: return Input.GetKeyDown(KeyCode.F2);
                case 5: return Input.GetKeyDown(KeyCode.F5);
                default: return false;
            }
#endif
        }
    }
}
