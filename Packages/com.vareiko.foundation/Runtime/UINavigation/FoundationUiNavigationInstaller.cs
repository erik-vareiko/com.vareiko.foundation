using System;
using Zenject;

namespace Vareiko.Foundation.UINavigation
{
    [Obsolete("Use FoundationUINavigationInstaller instead.")]
    public static class FoundationUiNavigationInstaller
    {
        public static void Install(DiContainer container)
        {
            FoundationUINavigationInstaller.Install(container);
        }
    }
}
