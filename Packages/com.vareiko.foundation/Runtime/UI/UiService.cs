using System;
using Zenject;

namespace Vareiko.Foundation.UI
{
    [Obsolete("Use UIService instead.")]
    public sealed class UiService : UIService
    {
        [Inject]
        public UiService(
            [InjectOptional] UIRegistry registry = null,
            [InjectOptional] UIScreenRegistry legacyRegistry = null,
            [InjectOptional] SignalBus signalBus = null)
            : base(registry, legacyRegistry, signalBus)
        {
        }
    }
}
