using UnityEngine;
using UnityEngine.Events;

namespace Vareiko.Foundation.UI
{
    public class UIButtonView : UIElement
    {
        [SerializeField] private bool _interactable = true;
        [SerializeField] private UnityEvent _onClicked = new UnityEvent();

        public bool Interactable => _interactable;
        public UnityEvent OnClicked => _onClicked;

        public virtual void SetInteractable(bool interactable)
        {
            _interactable = interactable;
        }

        public virtual void Click()
        {
            if (!_interactable)
            {
                return;
            }

            _onClicked?.Invoke();
        }
    }
}
