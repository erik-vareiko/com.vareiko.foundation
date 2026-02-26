using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIConfirmDialogPresenter : MonoBehaviour
    {
        [SerializeField] private Text _titleText;
        [SerializeField] private Text _messageText;
        [SerializeField] private Text _confirmButtonLabelText;
        [SerializeField] private Text _cancelButtonLabelText;
        [SerializeField] private UIButtonView _confirmButton;
        [SerializeField] private UIButtonView _cancelButton;
        [SerializeField] private UIButtonView _closeButton;
        [SerializeField] private bool _closeAsCancel = true;
        [SerializeField] private bool _instant = true;

        private IUIWindowResultService _windowResultService;
        private string _confirmPayload = string.Empty;
        private string _cancelPayload = string.Empty;
        private string _defaultTitle = string.Empty;
        private string _defaultMessage = string.Empty;
        private string _defaultConfirmLabel = string.Empty;
        private string _defaultCancelLabel = string.Empty;

        [Inject]
        public void Construct([InjectOptional] IUIWindowResultService windowResultService = null)
        {
            _windowResultService = windowResultService;
        }

        private void Awake()
        {
            _defaultTitle = _titleText != null ? _titleText.text : string.Empty;
            _defaultMessage = _messageText != null ? _messageText.text : string.Empty;
            _defaultConfirmLabel = _confirmButtonLabelText != null ? _confirmButtonLabelText.text : string.Empty;
            _defaultCancelLabel = _cancelButtonLabelText != null ? _cancelButtonLabelText.text : string.Empty;
        }

        private void OnEnable()
        {
            if (_confirmButton != null)
            {
                _confirmButton.OnClicked.AddListener(OnConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.OnClicked.AddListener(OnCancelClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.OnClicked.AddListener(OnCloseClicked);
            }
        }

        private void OnDisable()
        {
            if (_confirmButton != null)
            {
                _confirmButton.OnClicked.RemoveListener(OnConfirmClicked);
            }

            if (_cancelButton != null)
            {
                _cancelButton.OnClicked.RemoveListener(OnCancelClicked);
            }

            if (_closeButton != null)
            {
                _closeButton.OnClicked.RemoveListener(OnCloseClicked);
            }
        }

        public void Apply(UIConfirmDialogRequest request)
        {
            _confirmPayload = request.ConfirmPayload;
            _cancelPayload = request.CancelPayload;

            if (_titleText != null)
            {
                _titleText.text = string.IsNullOrEmpty(request.Title) ? _defaultTitle : request.Title;
            }

            if (_messageText != null)
            {
                _messageText.text = string.IsNullOrEmpty(request.Message) ? _defaultMessage : request.Message;
            }

            if (_confirmButtonLabelText != null)
            {
                _confirmButtonLabelText.text = string.IsNullOrEmpty(request.ConfirmButtonLabel)
                    ? _defaultConfirmLabel
                    : request.ConfirmButtonLabel;
            }

            if (_cancelButtonLabelText != null)
            {
                _cancelButtonLabelText.text = string.IsNullOrEmpty(request.CancelButtonLabel)
                    ? _defaultCancelLabel
                    : request.CancelButtonLabel;
            }
        }

        private void OnConfirmClicked()
        {
            _windowResultService?.TryResolveCurrent(UIWindowResultStatus.Confirmed, _confirmPayload, _instant);
        }

        private void OnCancelClicked()
        {
            _windowResultService?.TryResolveCurrent(UIWindowResultStatus.Canceled, _cancelPayload, _instant);
        }

        private void OnCloseClicked()
        {
            if (_closeAsCancel)
            {
                _windowResultService?.TryResolveCurrent(UIWindowResultStatus.Canceled, _cancelPayload, _instant);
                return;
            }

            _windowResultService?.TryResolveCurrent(UIWindowResultStatus.Closed, string.Empty, _instant);
        }
    }
}
