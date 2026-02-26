using UnityEngine;

namespace Vareiko.Foundation.Input
{
    public sealed class LegacyKeyboardInputAdapter : IInputAdapter
    {
        public InputScheme Scheme => InputScheme.KeyboardMouse;
        public bool IsAvailable => true;

        public Vector2 Move
        {
            get
            {
                float x = 0f;
                float y = 0f;

                if (UnityEngine.Input.GetKey(KeyCode.A) || UnityEngine.Input.GetKey(KeyCode.LeftArrow))
                {
                    x -= 1f;
                }

                if (UnityEngine.Input.GetKey(KeyCode.D) || UnityEngine.Input.GetKey(KeyCode.RightArrow))
                {
                    x += 1f;
                }

                if (UnityEngine.Input.GetKey(KeyCode.S) || UnityEngine.Input.GetKey(KeyCode.DownArrow))
                {
                    y -= 1f;
                }

                if (UnityEngine.Input.GetKey(KeyCode.W) || UnityEngine.Input.GetKey(KeyCode.UpArrow))
                {
                    y += 1f;
                }

                Vector2 value = new Vector2(x, y);
                return value.sqrMagnitude > 1f ? value.normalized : value;
            }
        }

        public bool DashPressedDown => UnityEngine.Input.GetKeyDown(KeyCode.Space);
        public bool PausePressedDown => UnityEngine.Input.GetKeyDown(KeyCode.Escape);
        public bool SubmitPressedDown => UnityEngine.Input.GetKeyDown(KeyCode.Return) || UnityEngine.Input.GetKeyDown(KeyCode.KeypadEnter);
        public bool CancelPressedDown => UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetMouseButtonDown(1);
    }
}
