using System;
using Zenject;

namespace Vareiko.Foundation.UI
{
    [Obsolete("Use FoundationUIInstaller instead.")]
    public static class FoundationUiInstaller
    {
        public static void Install(DiContainer container)
        {
            FoundationUIInstaller.Install(container);
        }
    }
}
