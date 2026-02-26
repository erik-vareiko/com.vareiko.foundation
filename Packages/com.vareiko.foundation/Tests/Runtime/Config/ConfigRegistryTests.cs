using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Config;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Config
{
    public sealed class ConfigRegistryTests
    {
        [Test]
        public async Task ExecuteAsync_RegistersValidEntries_AndNormalizesDefaultId()
        {
            GameObject gameObject = new GameObject("ConfigRegistryTests");
            TestConfigA configA = ScriptableObject.CreateInstance<TestConfigA>();
            TestConfigB configB = ScriptableObject.CreateInstance<TestConfigB>();
            try
            {
                ConfigRegistry registry = gameObject.AddComponent<ConfigRegistry>();
                SpyConfigService spy = new SpyConfigService();
                registry.Construct(spy);

                IList entries = CreateEntries(
                    CreateEntry("hud", configA),
                    CreateEntry(" ", configB),
                    CreateEntry("ignored", null));
                ReflectionTestUtil.SetPrivateField(registry, "_entries", entries);

                await registry.ExecuteAsync(CancellationToken.None);

                Assert.That(spy.Registered.Count, Is.EqualTo(2));
                Assert.That(spy.Registered[0].Type, Is.EqualTo(typeof(TestConfigA)));
                Assert.That(spy.Registered[0].Id, Is.EqualTo("hud"));
                Assert.That(spy.Registered[0].Config, Is.SameAs(configA));
                Assert.That(spy.Registered[1].Type, Is.EqualTo(typeof(TestConfigB)));
                Assert.That(spy.Registered[1].Id, Is.EqualTo("default"));
                Assert.That(spy.Registered[1].Config, Is.SameAs(configB));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(configA);
                UnityEngine.Object.DestroyImmediate(configB);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public async Task ExecuteAsync_WithCanceledToken_Throws()
        {
            GameObject gameObject = new GameObject("ConfigRegistryTests_Cancel");
            TestConfigA config = ScriptableObject.CreateInstance<TestConfigA>();
            try
            {
                ConfigRegistry registry = gameObject.AddComponent<ConfigRegistry>();
                SpyConfigService spy = new SpyConfigService();
                registry.Construct(spy);
                ReflectionTestUtil.SetPrivateField(registry, "_entries", CreateEntries(CreateEntry("id", config)));

                CancellationTokenSource cts = new CancellationTokenSource();
                cts.Cancel();

                Assert.ThrowsAsync<OperationCanceledException>(async () => await registry.ExecuteAsync(cts.Token));
                Assert.That(spy.Registered.Count, Is.EqualTo(0));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void Name_WhenTaskNameEmpty_UsesFallback()
        {
            GameObject gameObject = new GameObject("ConfigRegistryTests_Name");
            try
            {
                ConfigRegistry registry = gameObject.AddComponent<ConfigRegistry>();
                ReflectionTestUtil.SetPrivateField(registry, "_taskName", " ");

                Assert.That(registry.Name, Is.EqualTo(nameof(ConfigRegistry)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

        private static object CreateEntry(string id, ScriptableObject config)
        {
            Type entryType = typeof(ConfigRegistry).GetNestedType("Entry", BindingFlags.NonPublic);
            object entry = Activator.CreateInstance(entryType);
            entryType.GetField("Id", BindingFlags.Public | BindingFlags.Instance).SetValue(entry, id);
            entryType.GetField("Config", BindingFlags.Public | BindingFlags.Instance).SetValue(entry, config);
            return entry;
        }

        private static IList CreateEntries(params object[] entries)
        {
            Type entryType = typeof(ConfigRegistry).GetNestedType("Entry", BindingFlags.NonPublic);
            Type listType = typeof(List<>).MakeGenericType(entryType);
            IList list = (IList)Activator.CreateInstance(listType);
            for (int i = 0; i < entries.Length; i++)
            {
                list.Add(entries[i]);
            }

            return list;
        }

        private sealed class SpyConfigService : IConfigService
        {
            public readonly List<RegisteredEntry> Registered = new List<RegisteredEntry>(2);

            public void Register<T>(T config, string id = "default") where T : ScriptableObject
            {
                Registered.Add(new RegisteredEntry(typeof(T), id, config));
            }

            public bool TryGet<T>(out T config, string id = "default") where T : ScriptableObject
            {
                config = null;
                return false;
            }

            public T GetRequired<T>(string id = "default") where T : ScriptableObject
            {
                throw new InvalidOperationException("Not used in test");
            }

            public void Unregister<T>(string id = "default") where T : ScriptableObject
            {
            }
        }

        private readonly struct RegisteredEntry
        {
            public readonly Type Type;
            public readonly string Id;
            public readonly ScriptableObject Config;

            public RegisteredEntry(Type type, string id, ScriptableObject config)
            {
                Type = type;
                Id = id;
                Config = config;
            }
        }

        private sealed class TestConfigA : ScriptableObject
        {
        }

        private sealed class TestConfigB : ScriptableObject
        {
        }
    }
}
