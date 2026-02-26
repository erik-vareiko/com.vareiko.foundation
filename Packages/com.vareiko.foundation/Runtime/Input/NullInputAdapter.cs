using UnityEngine;

namespace Vareiko.Foundation.Input
{
    public sealed class NullInputAdapter : IInputAdapter
    {
        public InputScheme Scheme => InputScheme.Unknown;
        public bool IsAvailable => true;
        public Vector2 Move => Vector2.zero;
        public bool DashPressedDown => false;
        public bool PausePressedDown => false;
        public bool SubmitPressedDown => false;
        public bool CancelPressedDown => false;
    }
}
