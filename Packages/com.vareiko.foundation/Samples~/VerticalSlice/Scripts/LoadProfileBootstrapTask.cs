using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Save;
using VContainer;

namespace Vareiko.Foundation.Samples.VerticalSlice
{
    /// <summary>
    /// Loads the slice profile during boot so gameplay starts with persisted progress.
    /// Registered on the scene installer's bootstrap-task list; the container injects
    /// Construct when the scene scope builds.
    /// </summary>
    public sealed class LoadProfileBootstrapTask : MonoBehaviour, IBootstrapTask
    {
        private ISaveService _saveService;

        public int Order => 100;
        public string Name => "Load Slice Profile";

        public SliceProfile Profile { get; private set; } = new SliceProfile();

        [Inject]
        public void Construct(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public async UniTask ExecuteAsync(CancellationToken cancellationToken)
        {
            Profile = await _saveService.LoadAsync("slice", "profile", new SliceProfile(), cancellationToken);
            Debug.Log($"[VerticalSlice] Profile loaded: runs={Profile.RunsCompleted}, gold={Profile.Currencies.GetValueOrDefault("gold")}");
        }
    }
}
