using UnityEngine;

namespace Vareiko.Foundation.Input
{
    public interface IInputAdapter
    {
        InputScheme Scheme { get; }
        bool IsAvailable { get; }
        Vector2 Move { get; }
        bool DashPressedDown { get; }
        bool PausePressedDown { get; }
        bool SubmitPressedDown { get; }
        bool CancelPressedDown { get; }
    }
}
