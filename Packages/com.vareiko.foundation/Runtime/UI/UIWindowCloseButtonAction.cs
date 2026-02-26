using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIWindowCloseButtonAction : MonoBehaviour
    {
        [SerializeField] private UIButtonView _button;
        [SerializeField] private bool _closeSpecificWindow;
        [SerializeField] private string _windowId;
        [SerializeField] private bool _instant = true;

        private IUIWindowManager _windowManager;

        [Inject]
        public void Construct([InjectOptional] IUIWindowManager windowManager = null)
        {
            _windowManager = windowManager;
        }

        private void Awake()
        {
            if (_button == null)
            {
                _button = GetComponent<UIButtonView>();
            }
        }

        private void OnEnable()
        {
            if (_button != null)
            {
                _button.OnClicked.AddListener(OnClicked);
            }
        }

        private void OnDisable()
        {
            if (_button != null)
            {
                _button.OnClicked.RemoveListener(OnClicked);
            }
        }

        private void OnClicked()
        {
            if (_windowManager == null)
            {
                return;
            }

            if (_closeSpecificWindow)
            {
                if (string.IsNullOrWhiteSpace(_windowId))
                {
                    return;
                }

                _windowManager.TryClose(_windowId.Trim(), _instant);
                return;
            }

            _windowManager.TryCloseCurrent(_instant);
        }
    }
}
