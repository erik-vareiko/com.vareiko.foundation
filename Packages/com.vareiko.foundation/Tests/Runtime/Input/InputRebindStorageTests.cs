using System;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Input;

namespace Vareiko.Foundation.Tests.Input
{
    public sealed class InputRebindStorageTests
    {
        [Test]
        public void SaveLoadClear_WorksWithPlayerPrefsStorage()
        {
            string key = "vareiko.foundation.tests.input.rebind." + Guid.NewGuid().ToString("N");
            PlayerPrefsInputRebindStorage storage = new PlayerPrefsInputRebindStorage(key);

            try
            {
                storage.Clear();
                Assert.That(storage.Load(), Is.EqualTo(string.Empty));

                const string payload = "{\"items\":[{\"action\":\"Submit\"}]}";
                storage.Save(payload);
                Assert.That(storage.Load(), Is.EqualTo(payload));

                storage.Clear();
                Assert.That(storage.Load(), Is.EqualTo(string.Empty));
            }
            finally
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
            }
        }
    }
}
