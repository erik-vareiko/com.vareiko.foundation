using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Pooling;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Signals;
using Vareiko.Foundation.Time;
using VContainer;

namespace Vareiko.Foundation.Samples.VerticalSlice
{
    /// <summary>
    /// The "gameplay" of the slice: once boot completes it enters a host-defined app state,
    /// spawns pooled cubes on a timer, rotates them through a tick listener and autosaves the
    /// profile — no Update(), no coroutines, no manual instantiate/destroy.
    /// </summary>
    public sealed class SliceGameplayDriver : MonoBehaviour
    {
        private const int MaxAliveCubes = 8;

        private static readonly AppState RunState = new AppState("Run");

        private IAppStateMachine _appStateMachine;
        private ITickService _tickService;
        private ISaveService _saveService;
        private IFoundationSignalBus _signalBus;

        private LoadProfileBootstrapTask _profileTask;
        private ComponentPool<Transform> _cubePool;
        private readonly Queue<Transform> _aliveCubes = new Queue<Transform>();
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        [Inject]
        public void Construct(
            IAppStateMachine appStateMachine,
            ITickService tickService,
            ISaveService saveService,
            IFoundationSignalBus signalBus)
        {
            _appStateMachine = appStateMachine;
            _tickService = tickService;
            _saveService = saveService;
            _signalBus = signalBus;
        }

        public void SetProfileTask(LoadProfileBootstrapTask profileTask)
        {
            _profileTask = profileTask;
        }

        private void Start()
        {
            if (_signalBus == null)
            {
                Debug.LogError("[VerticalSlice] Not injected — is a FoundationSceneInstaller present in the scene?");
                return;
            }

            Transform cubeTemplate = GameObject.CreatePrimitive(PrimitiveType.Cube).transform;
            cubeTemplate.gameObject.SetActive(false);
            cubeTemplate.name = "CubeTemplate";
            _cubePool = new ComponentPool<Transform>(cubeTemplate, transform, maxSize: MaxAliveCubes);

            _subscriptions.Add(_signalBus.Subscribe<ApplicationBootCompletedSignal>(_ => OnBootCompleted()));
            _subscriptions.Add(_signalBus.Subscribe<AppStateChangedSignal>(signal =>
                Debug.Log($"[VerticalSlice] AppState: {signal.Previous} -> {signal.Current}")));
        }

        private void OnBootCompleted()
        {
            if (!_appStateMachine.TryEnter(RunState))
            {
                return;
            }

            _subscriptions.Add(_tickService.RegisterTick(RotateCubes));
            _subscriptions.Add(_tickService.Repeat(0.5f, SpawnCube));
            _subscriptions.Add(_tickService.Repeat(5f, AutosaveProfile));
            Debug.Log("[VerticalSlice] Run started.");
        }

        private void RotateCubes(float deltaTime)
        {
            foreach (Transform cube in _aliveCubes)
            {
                cube.Rotate(0f, 90f * deltaTime, 0f);
            }
        }

        private void SpawnCube()
        {
            if (_aliveCubes.Count >= MaxAliveCubes)
            {
                _cubePool.Release(_aliveCubes.Dequeue());
            }

            Transform cube = _cubePool.Get();
            cube.position = new Vector3(_aliveCubes.Count * 1.5f - MaxAliveCubes * 0.75f, 0f, 0f);
            _aliveCubes.Enqueue(cube);
        }

        private void AutosaveProfile()
        {
            SliceProfile profile = _profileTask != null ? _profileTask.Profile : new SliceProfile();
            profile.Currencies.TryGetValue("gold", out int gold);
            profile.Currencies["gold"] = gold + 10;
            _saveService.SaveAsync("slice", "profile", profile).Forget();
            Debug.Log($"[VerticalSlice] Autosaved: gold={profile.Currencies["gold"]}");
        }

        private void OnDestroy()
        {
            if (_profileTask != null && _appStateMachine != null && _appStateMachine.IsIn(RunState))
            {
                SliceProfile profile = _profileTask.Profile;
                profile.RunsCompleted++;
                _saveService.SaveAsync("slice", "profile", profile).Forget();
            }

            _subscriptions.Dispose();
            _cubePool?.Dispose();
        }
    }
}
