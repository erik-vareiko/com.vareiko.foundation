using System;
using Vareiko.Foundation.UI;
using Zenject;

namespace Vareiko.Foundation.UINavigation
{
    [Obsolete("Use UINavigationService instead.")]
    public sealed class UiNavigationService : UINavigationService
    {
        [Inject]
        public UiNavigationService(IUIService uiService, [InjectOptional] SignalBus signalBus = null)
            : base(uiService, signalBus)
        {
        }
    }
}
