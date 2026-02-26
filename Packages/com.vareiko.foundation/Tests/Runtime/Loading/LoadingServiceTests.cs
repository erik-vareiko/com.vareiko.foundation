using NUnit.Framework;
using Vareiko.Foundation.Loading;

namespace Vareiko.Foundation.Tests.Loading
{
    public sealed class LoadingServiceTests
    {
        [Test]
        public void ManualFlow_UpdatesStateAndProgress()
        {
            LoadingService service = new LoadingService(null);

            service.BeginManual("Boot");
            Assert.That(service.IsLoading, Is.True);
            Assert.That(service.ActiveOperation, Is.EqualTo("Boot"));
            Assert.That(service.Progress, Is.EqualTo(0f));

            service.SetManualProgress(0.4f);
            Assert.That(service.Progress, Is.EqualTo(0.4f));

            service.SetManualProgress(5f);
            Assert.That(service.Progress, Is.EqualTo(1f));

            service.CompleteManual();
            Assert.That(service.IsLoading, Is.False);
            Assert.That(service.Progress, Is.EqualTo(1f));
            Assert.That(service.ActiveOperation, Is.EqualTo(string.Empty));
        }
    }
}
