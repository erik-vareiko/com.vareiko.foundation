using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Bootstrap;

namespace Vareiko.Foundation.Tests.Boot
{
    public sealed class BootstrapRunnerTests
    {
        [Test]
        public async Task Initialize_ExecutesTasksInOrder()
        {
            List<string> executed = new List<string>();
            List<IBootstrapTask> tasks = new List<IBootstrapTask>
            {
                new RecordingTask(20, "TaskB", executed),
                new RecordingTask(10, "TaskA", executed),
                new RecordingTask(10, "TaskC", executed)
            };

            FakeAppStateMachine stateMachine = new FakeAppStateMachine();
            BootstrapRunner runner = new BootstrapRunner(tasks, null, stateMachine);
            runner.Initialize();

            await UniTask.DelayFrame(2);

            CollectionAssert.AreEqual(new[] { "TaskA", "TaskC", "TaskB" }, executed);
            Assert.That(stateMachine.Current, Is.EqualTo(AppState.None));
            runner.Dispose();
        }

        [Test]
        public async Task Initialize_WhenTaskThrows_TransitionsToErrorAndStopsPipeline()
        {
            List<string> executed = new List<string>();
            List<IBootstrapTask> tasks = new List<IBootstrapTask>
            {
                new ThrowingTask(0, "Explode", executed),
                new RecordingTask(1, "AfterError", executed)
            };

            FakeAppStateMachine stateMachine = new FakeAppStateMachine();
            BootstrapRunner runner = new BootstrapRunner(tasks, null, stateMachine);
            runner.Initialize();

            await UniTask.DelayFrame(2);

            CollectionAssert.AreEqual(new[] { "Explode" }, executed);
            Assert.That(stateMachine.Current, Is.EqualTo(AppState.Error));
            runner.Dispose();
        }

        private sealed class RecordingTask : IBootstrapTask
        {
            private readonly List<string> _executed;

            public RecordingTask(int order, string name, List<string> executed)
            {
                Order = order;
                Name = name;
                _executed = executed;
            }

            public int Order { get; }
            public string Name { get; }

            public UniTask ExecuteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _executed.Add(Name);
                return UniTask.CompletedTask;
            }
        }

        private sealed class ThrowingTask : IBootstrapTask
        {
            private readonly List<string> _executed;

            public ThrowingTask(int order, string name, List<string> executed)
            {
                Order = order;
                Name = name;
                _executed = executed;
            }

            public int Order { get; }
            public string Name { get; }

            public UniTask ExecuteAsync(CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                _executed.Add(Name);
                throw new InvalidOperationException("Boot task failed.");
            }
        }

        private sealed class FakeAppStateMachine : IAppStateMachine
        {
            private AppState _current = AppState.None;

            public AppState Current => _current;

            public bool IsIn(AppState state)
            {
                return _current == state;
            }

            public bool TryEnter(AppState next)
            {
                if (next == _current || next == AppState.None)
                {
                    return false;
                }

                _current = next;
                return true;
            }

            public void ForceEnter(AppState next)
            {
                _current = next;
            }
        }
    }
}
