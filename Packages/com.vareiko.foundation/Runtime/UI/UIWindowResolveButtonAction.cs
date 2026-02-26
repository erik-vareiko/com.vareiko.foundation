using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIWindowResolveButtonAction : MonoBehaviour
    {
        [SerializeField] private UIButtonView _button;
        [SerializeField] private bool _resolveSpecificWindow;
        [SerializeField] private string _windowId;
        [SerializeField] private UIWindowResultStatus _status = UIWindowResultStatus.Confirmed;
        [SerializeField] private string _payload;
        [SerializeField] private bool _instant = true;

        private IUIWindowResultService _windowResultService;

        [Inject]
        public void Construct([InjectOptional] IUIWindowResultService windowResultService = null)
        {
            _windowResultService = windowResultService;
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
            if (_windowResultService == null)
            {
                return;
            }

            if (_resolveSpecificWindow)
            {
                if (string.IsNullOrWhiteSpace(_windowId))
                {
                    return;
                }

                _windowResultService.TryResolve(_windowId.Trim(), _status, _payload, _instant);
                return;
            }

            _windowResultService.TryResolveCurrent(_status, _payload, _instant);
        }
    }
}
