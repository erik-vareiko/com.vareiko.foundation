using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIWindowOpenButtonAction : MonoBehaviour
    {
        [SerializeField] private UIButtonView _button;
        [SerializeField] private string _windowId;
        [SerializeField] private bool _instant = true;
        [SerializeField] private int _priority;
        [SerializeField] private bool _allowDuplicate;

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
            if (_windowManager == null || string.IsNullOrWhiteSpace(_windowId))
            {
                return;
            }

            _windowManager.Enqueue(_windowId.Trim(), _instant, _priority, _allowDuplicate);
        }
    }
}
