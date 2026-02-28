using NUnit.Framework;
using Vareiko.Foundation.Input;

namespace Vareiko.Foundation.Tests.Input
{
    public sealed class InputRebindServiceTests
    {
        [Test]
        public void WithoutAdapter_ServiceReportsUnsupported()
        {
            InputRebindService service = new InputRebindService();

            Assert.That(service.IsSupported, Is.False);
            Assert.That(service.TryApplyBindingOverride("Submit", 0, "<Keyboard>/space"), Is.False);
            Assert.That(service.TryRemoveBindingOverride("Submit", 0), Is.False);
            Assert.That(service.ExportOverridesJson(), Is.EqualTo(string.Empty));
            Assert.That(service.ImportOverridesJson("{\"items\":[]}", true), Is.False);
        }

#if ENABLE_INPUT_SYSTEM
        [Test]
        public void ApplyImportReset_PersistsThroughStorage()
        {
            InMemoryInputRebindStorage storage = new InMemoryInputRebindStorage();

            NewInputSystemAdapter adapter = new NewInputSystemAdapter(storage);
            InputRebindService service = new InputRebindService(adapter);

            bool applied = service.TryApplyBindingOverride("Submit", 0, "<Keyboard>/space");
            string exported = service.ExportOverridesJson();

            Assert.That(service.IsSupported, Is.True);
            Assert.That(applied, Is.True);
            Assert.That(exported, Does.Contain("Submit"));
            Assert.That(exported, Does.Contain("<Keyboard>/space"));
            Assert.That(string.IsNullOrWhiteSpace(storage.LastSaved), Is.False);

            NewInputSystemAdapter restoredAdapter = new NewInputSystemAdapter(storage);
            InputRebindService restoredService = new InputRebindService(restoredAdapter);
            string restoredJson = restoredService.ExportOverridesJson();
            Assert.That(restoredJson, Does.Contain("<Keyboard>/space"));

            restoredService.ResetAllBindingOverrides();
            Assert.That(restoredService.ExportOverridesJson(), Is.EqualTo(string.Empty));
            Assert.That(storage.Cleared, Is.True);

            adapter.Dispose();
            restoredAdapter.Dispose();
        }

        private sealed class InMemoryInputRebindStorage : IInputRebindStorage
        {
            public string LastSaved { get; private set; } = string.Empty;
            public bool Cleared { get; private set; }

            public string Load()
            {
                return LastSaved;
            }

            public void Save(string overridesJson)
            {
                Cleared = false;
                LastSaved = overridesJson ?? string.Empty;
            }

            public void Clear()
            {
                Cleared = true;
                LastSaved = string.Empty;
            }
        }
#endif
    }
}
