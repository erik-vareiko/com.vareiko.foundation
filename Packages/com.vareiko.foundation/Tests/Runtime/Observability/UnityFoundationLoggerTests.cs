using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Observability
{
    public sealed class UnityFoundationLoggerTests
    {
        [Test]
        public void Log_WithBoundSink_WritesSinkAndSignal()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            LogMessageEmittedSignal emitted = default;
            bool fired = false;
            signalBus.Subscribe<LogMessageEmittedSignal>(signal =>
            {
                emitted = signal;
                fired = true;
            });

            SpySink sink = new SpySink();
            UnityFoundationLogger logger = new UnityFoundationLogger(null, signalBus, new List<IFoundationLogSink> { sink });

            logger.Info("Boot finished", "Boot");

            Assert.That(sink.Entries.Count, Is.EqualTo(1));
            Assert.That(sink.Entries[0].Level, Is.EqualTo(FoundationLogLevel.Info));
            Assert.That(sink.Entries[0].Message, Is.EqualTo("Boot finished"));
            Assert.That(sink.Entries[0].Category, Is.EqualTo("Boot"));
            Assert.That(fired, Is.True);
            Assert.That(emitted.Level, Is.EqualTo(FoundationLogLevel.Info));
            Assert.That(emitted.Message, Is.EqualTo("Boot finished"));
            Assert.That(emitted.Category, Is.EqualTo("Boot"));
        }

        [Test]
        public void Log_WhenBelowMinimumLevel_IsIgnored()
        {
            ObservabilityConfig config = ScriptableObject.CreateInstance<ObservabilityConfig>();
            try
            {
                ReflectionTestUtil.SetPrivateField(config, "_minimumLogLevel", FoundationLogLevel.Warning);
                FakeSignalBus signalBus = new FakeSignalBus();
                bool fired = false;
                signalBus.Subscribe<LogMessageEmittedSignal>(_ => fired = true);

                SpySink sink = new SpySink();
                UnityFoundationLogger logger = new UnityFoundationLogger(config, signalBus, new List<IFoundationLogSink> { sink });

                logger.Info("not logged", "Test");

                Assert.That(sink.Entries.Count, Is.EqualTo(0));
                Assert.That(fired, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Log_WithNullMessageAndCategory_UsesSafeDefaults()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            LogMessageEmittedSignal emitted = default;
            bool fired = false;
            signalBus.Subscribe<LogMessageEmittedSignal>(signal =>
            {
                emitted = signal;
                fired = true;
            });

            SpySink sink = new SpySink();
            UnityFoundationLogger logger = new UnityFoundationLogger(null, signalBus, new List<IFoundationLogSink> { sink });

            logger.Error(null, null);

            Assert.That(sink.Entries.Count, Is.EqualTo(1));
            Assert.That(sink.Entries[0].Message, Is.EqualTo(string.Empty));
            Assert.That(sink.Entries[0].Category, Is.EqualTo("Foundation"));
            Assert.That(fired, Is.True);
            Assert.That(emitted.Message, Is.EqualTo(string.Empty));
            Assert.That(emitted.Category, Is.EqualTo("Foundation"));
        }

        private sealed class SpySink : IFoundationLogSink
        {
            public readonly List<FoundationLogEntry> Entries = new List<FoundationLogEntry>(4);

            public void Write(FoundationLogEntry entry)
            {
                Entries.Add(entry);
            }
        }
    }
}
