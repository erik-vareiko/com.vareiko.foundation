using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.App;
using UnityEngine;
using Vareiko.Foundation.Signals;

namespace Vareiko.Foundation.Bootstrap
{
    public sealed class BootstrapRunner : VContainer.Unity.IInitializable, IDisposable
    {
        private readonly List<IBootstrapTask> _tasks;
        private readonly IFoundationSignalBus _signalBus;
        private readonly IAppStateMachine _appStateMachine;

        private CancellationTokenSource _lifecycleCts;
        private bool _started;

        public BootstrapRunner(
            List<IBootstrapTask> tasks = null,
            IFoundationSignalBus signalBus = null,
            IAppStateMachine appStateMachine = null)
        {
            _tasks = tasks ?? new List<IBootstrapTask>(0);
            _signalBus = signalBus;
            _appStateMachine = appStateMachine;
        }

        public void Initialize()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            _lifecycleCts = new CancellationTokenSource();
            RunAsync(_lifecycleCts.Token).Forget();
        }

        public void Dispose()
        {
            if (_lifecycleCts != null)
            {
                _lifecycleCts.Cancel();
                _lifecycleCts.Dispose();
                _lifecycleCts = null;
            }
        }

        private async UniTaskVoid RunAsync(CancellationToken cancellationToken)
        {
            List<IBootstrapTask> orderedTasks = CollectOrderedTasks();
            int totalTasks = orderedTasks.Count;

            _signalBus?.Publish(new ApplicationBootStartedSignal(totalTasks));

            for (int i = 0; i < orderedTasks.Count; i++)
            {
                IBootstrapTask task = orderedTasks[i];
                if (task == null)
                {
                    continue;
                }

                string taskName = string.IsNullOrWhiteSpace(task.Name) ? task.GetType().Name : task.Name;
                _signalBus?.Publish(new ApplicationBootTaskStartedSignal(taskName, task.Order, i + 1, totalTasks));

                try
                {
                    await task.ExecuteAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    _signalBus?.Publish(new ApplicationBootFailedSignal(taskName, exception.Message));
                    if (_appStateMachine != null && !_appStateMachine.IsIn(AppState.Error))
                    {
                        _appStateMachine.TryEnter(AppState.Error);
                    }
                    return;
                }

                _signalBus?.Publish(new ApplicationBootTaskCompletedSignal(taskName, task.Order, i + 1, totalTasks));
            }

            _signalBus?.Publish(new ApplicationBootCompletedSignal(totalTasks));
        }

        private List<IBootstrapTask> CollectOrderedTasks()
        {
            List<IBootstrapTask> orderedTasks = new List<IBootstrapTask>(_tasks.Count);
            for (int i = 0; i < _tasks.Count; i++)
            {
                if (_tasks[i] != null)
                {
                    orderedTasks.Add(_tasks[i]);
                }
            }

            orderedTasks.Sort(CompareTasks);
            return orderedTasks;
        }

        private static int CompareTasks(IBootstrapTask left, IBootstrapTask right)
        {
            int byOrder = left.Order.CompareTo(right.Order);
            if (byOrder != 0)
            {
                return byOrder;
            }

            string leftName = string.IsNullOrWhiteSpace(left.Name) ? left.GetType().Name : left.Name;
            string rightName = string.IsNullOrWhiteSpace(right.Name) ? right.GetType().Name : right.Name;
            return string.CompareOrdinal(leftName, rightName);
        }
    }
}
