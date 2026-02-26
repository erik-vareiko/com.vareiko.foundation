using UnityEngine;

namespace Vareiko.Foundation.Input
{
    public interface IInputService
    {
        InputScheme CurrentScheme { get; }
        Vector2 Move { get; }
        bool DashPressedDown { get; }
        bool PausePressedDown { get; }
        bool SubmitPressedDown { get; }
        bool CancelPressedDown { get; }
        void SetPreferredScheme(InputScheme scheme);
    }
}
