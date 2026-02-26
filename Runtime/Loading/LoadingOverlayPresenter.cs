using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Loading
{
    public sealed class LoadingOverlayPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private bool _hideWhenIdle = true;
        [SerializeField] private bool _useProgressAsAlpha;

        private SignalBus _signalBus;
        private bool _subscribed;

        [Inject]
        public void Construct([InjectOptional] SignalBus signalBus = null)
        {
            _signalBus = signalBus;
        }

        private void OnEnable()
        {
            if (_signalBus == null || _subscribed)
            {
                return;
            }

            _signalBus.Subscribe<LoadingStateChangedSignal>(OnLoadingChanged);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (_signalBus == null || !_subscribed)
            {
                return;
            }

            _signalBus.Unsubscribe<LoadingStateChangedSignal>(OnLoadingChanged);
            _subscribed = false;
        }

        private void OnLoadingChanged(LoadingStateChangedSignal signal)
        {
            GameObject root = _root != null ? _root : gameObject;
            if (_hideWhenIdle)
            {
                root.SetActive(signal.IsLoading);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = _useProgressAsAlpha ? Mathf.Clamp01(signal.Progress) : (signal.IsLoading ? 1f : 0f);
                _canvasGroup.blocksRaycasts = signal.IsLoading;
                _canvasGroup.interactable = signal.IsLoading;
            }
        }
    }
}
