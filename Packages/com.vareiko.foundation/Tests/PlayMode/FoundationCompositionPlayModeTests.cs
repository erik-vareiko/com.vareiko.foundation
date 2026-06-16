using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Installers;
using Vareiko.Foundation.Signals;
using Vareiko.Foundation.UI;
using VContainer;

namespace Vareiko.Foundation.Tests.PlayMode
{
    /// <summary>
    /// Boots the real project + scene scopes in play mode and asserts the three
    /// composition guarantees the EditMode suite cannot cover: bootstrap runs exactly
    /// once (scene scope only), the scene-wide inject pass reaches scene MonoBehaviours
    /// (including inactive ones), and the scene scope parents to the project scope.
    /// </summary>
    public sealed class FoundationCompositionPlayModeTests
    {
        private GameObject _projectRoot;
        private GameObject _sceneRoot;
        private GameObject _activeBinderRoot;
        private GameObject _inactiveBinderRoot;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            DestroyIfAlive(_sceneRoot);
            DestroyIfAlive(_activeBinderRoot);
            DestroyIfAlive(_inactiveBinderRoot);
            DestroyIfAlive(_projectRoot);
        }

        [UnityTest]
        public IEnumerator ProjectAndSceneScopes_BootOnce_AndInjectSceneObjects()
        {
            // Foundation services log by design when no backend/configs are bound
            // (startup validation, simulated providers); this is a composition smoke
            // test, not a log-cleanliness test.
            LogAssert.ignoreFailingMessages = true;

            _projectRoot = new GameObject("FoundationProjectScope");
            FoundationProjectInstaller projectScope = _projectRoot.AddComponent<FoundationProjectInstaller>();
            Assert.That(projectScope.Container, Is.Not.Null, "Project scope must build in Awake.");

            IFoundationSignalBus projectSignalBus = projectScope.Container.Resolve<IFoundationSignalBus>();
            int bootStartedCount = 0;
            IDisposable subscription = projectSignalBus.Subscribe<ApplicationBootStartedSignal>(_ => bootStartedCount++);

            // Scene objects exist before the scene scope so its inject pass must reach them.
            _activeBinderRoot = new GameObject("ActiveBinder");
            UIBoolGameObjectBinder activeBinder = _activeBinderRoot.AddComponent<UIBoolGameObjectBinder>();
            _inactiveBinderRoot = new GameObject("InactiveBinder");
            _inactiveBinderRoot.SetActive(false);
            UIBoolGameObjectBinder inactiveBinder = _inactiveBinderRoot.AddComponent<UIBoolGameObjectBinder>();

            _sceneRoot = new GameObject("FoundationSceneScope");
            FoundationSceneInstaller sceneScope = _sceneRoot.AddComponent<FoundationSceneInstaller>();
            Assert.That(sceneScope.Container, Is.Not.Null, "Scene scope must build in Awake (parented to the project scope).");

            // Scene scope resolves the project-scope singletons (parentReference default).
            Assert.That(
                ReferenceEquals(sceneScope.Container.Resolve<IFoundationSignalBus>(), projectSignalBus),
                Is.True,
                "Scene scope must resolve project-scope services through its parent.");

            // Scene-wide inject pass reached both active and inactive MonoBehaviours.
            Assert.That(GetPrivateField(activeBinder, "_signalBus"), Is.Not.Null, "Active binder not injected.");
            Assert.That(GetPrivateField(activeBinder, "_valueService"), Is.Not.Null, "Active binder value service not injected.");
            Assert.That(GetPrivateField(inactiveBinder, "_signalBus"), Is.Not.Null, "Inactive binder not injected.");

            // Bootstrap publishes asynchronously after the scene scope builds; give it frames.
            yield return null;
            yield return null;
            Assert.That(bootStartedCount, Is.EqualTo(1), "Bootstrap must run exactly once (scene scope only).");

            subscription.Dispose();
        }

        private static object GetPrivateField(object instance, string fieldName)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field '{fieldName}' not found on {instance.GetType().Name}.");
            return field.GetValue(instance);
        }

        private static void DestroyIfAlive(GameObject gameObject)
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
        }
    }
}
