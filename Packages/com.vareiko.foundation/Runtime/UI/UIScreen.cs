using UnityEngine;

namespace Vareiko.Foundation.UI
{
    public class UIScreen : MonoBehaviour
    {
        [SerializeField] private string _id;
        [SerializeField] private bool _hideOnAwake = true;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private bool _disableRaycastsWhenHidden = true;

        public string Id => _id;
        public bool IsVisible => gameObject.activeSelf;

        protected virtual void Awake()
        {
            if (_hideOnAwake)
            {
                SetVisible(false, true);
            }
        }

        public virtual void Show(bool instant = true)
        {
            SetVisible(true, instant);
        }

        public virtual void Hide(bool instant = true)
        {
            SetVisible(false, instant);
        }

        protected virtual void SetVisible(bool visible, bool instant)
        {
            if (_canvasGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            gameObject.SetActive(true);
            _canvasGroup.alpha = visible ? 1f : 0f;
            _canvasGroup.interactable = visible;

            if (_disableRaycastsWhenHidden)
            {
                _canvasGroup.blocksRaycasts = visible;
            }

            if (!visible)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
