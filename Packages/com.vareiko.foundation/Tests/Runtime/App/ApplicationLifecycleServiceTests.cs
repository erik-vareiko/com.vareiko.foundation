using NUnit.Framework;
using Vareiko.Foundation.App;

namespace Vareiko.Foundation.Tests.App
{
    public sealed class ApplicationLifecycleServiceTests
    {
        [Test]
        public void SourceEvents_UpdateState_AndRaiseServiceEvents()
        {
            FakeLifecycleSource source = new FakeLifecycleSource();
            ApplicationLifecycleService service = new ApplicationLifecycleService(null, source);

            int pauseCalls = 0;
            int focusCalls = 0;
            int quitCalls = 0;

            service.PauseChanged += _ => pauseCalls++;
            service.FocusChanged += _ => focusCalls++;
            service.QuitRequested += () => quitCalls++;

            service.Initialize();

            source.EmitPause(true);
            source.EmitPause(true);
            source.EmitFocus(false);
            source.EmitFocus(false);
            source.EmitQuit();
            source.EmitQuit();

            Assert.That(service.IsPaused, Is.True);
            Assert.That(service.HasFocus, Is.False);
            Assert.That(service.IsQuitting, Is.True);
            Assert.That(pauseCalls, Is.EqualTo(1));
            Assert.That(focusCalls, Is.EqualTo(1));
            Assert.That(quitCalls, Is.EqualTo(1));

            service.Dispose();
            source.EmitPause(false);
            Assert.That(service.IsPaused, Is.True);
        }

        private sealed class FakeLifecycleSource : IApplicationLifecycleSource
        {
            public event System.Action<bool> PauseChanged;
            public event System.Action<bool> FocusChanged;
            public event System.Action QuitRequested;

            public void Initialize()
            {
            }

            public void Dispose()
            {
            }

            public void EmitPause(bool isPaused)
            {
                PauseChanged?.Invoke(isPaused);
            }

            public void EmitFocus(bool hasFocus)
            {
                FocusChanged?.Invoke(hasFocus);
            }

            public void EmitQuit()
            {
                QuitRequested?.Invoke();
            }
        }
    }
}
