using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Pooling;

namespace Vareiko.Foundation.Tests.Pooling
{
    public sealed class ComponentPoolTests
    {
        private readonly List<GameObject> _cleanup = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _cleanup.Count; i++)
            {
                if (_cleanup[i] != null)
                {
                    UnityEngine.Object.DestroyImmediate(_cleanup[i]);
                }
            }

            _cleanup.Clear();
        }

        private Transform CreatePrefabAndRoot(out BoxCollider prefab)
        {
            GameObject prefabRoot = new GameObject("prefab");
            _cleanup.Add(prefabRoot);
            prefab = prefabRoot.AddComponent<BoxCollider>();

            GameObject poolRoot = new GameObject("pool-root");
            _cleanup.Add(poolRoot);
            return poolRoot.transform;
        }

        [Test]
        public void Get_ActivatesInstance_ReleaseDeactivatesAndParents()
        {
            Transform root = CreatePrefabAndRoot(out BoxCollider prefab);
            ComponentPool<BoxCollider> pool = new ComponentPool<BoxCollider>(prefab, root);

            BoxCollider instance = pool.Get();
            _cleanup.Add(instance.gameObject);
            Assert.That(instance.gameObject.activeSelf, Is.True);

            pool.Release(instance);
            Assert.That(instance.gameObject.activeSelf, Is.False);
            Assert.That(instance.transform.parent, Is.SameAs(root));
        }

        [Test]
        public void ReleaseGet_ReusesInstance()
        {
            Transform root = CreatePrefabAndRoot(out BoxCollider prefab);
            ComponentPool<BoxCollider> pool = new ComponentPool<BoxCollider>(prefab, root);

            BoxCollider first = pool.Get();
            _cleanup.Add(first.gameObject);
            pool.Release(first);
            BoxCollider second = pool.Get();

            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void Release_Twice_Throws()
        {
            Transform root = CreatePrefabAndRoot(out BoxCollider prefab);
            ComponentPool<BoxCollider> pool = new ComponentPool<BoxCollider>(prefab, root);
            BoxCollider instance = pool.Get();
            _cleanup.Add(instance.gameObject);

            pool.Release(instance);
            Assert.Throws<InvalidOperationException>(() => pool.Release(instance));
        }

        [Test]
        public void Overflow_BeyondMaxSize_IsDestroyed()
        {
            Transform root = CreatePrefabAndRoot(out BoxCollider prefab);
            ComponentPool<BoxCollider> pool = new ComponentPool<BoxCollider>(prefab, root, maxSize: 1);

            BoxCollider first = pool.Get();
            BoxCollider second = pool.Get();
            _cleanup.Add(first.gameObject);
            _cleanup.Add(second.gameObject);

            pool.Release(first);
            pool.Release(second);

            Assert.That(pool.CountInactive, Is.EqualTo(1));
            Assert.That(second == null, Is.True, "Overflow instance must be destroyed.");
        }

        [Test]
        public void Get_SkipsExternallyDestroyedInactiveInstances()
        {
            Transform root = CreatePrefabAndRoot(out BoxCollider prefab);
            ComponentPool<BoxCollider> pool = new ComponentPool<BoxCollider>(prefab, root);

            BoxCollider instance = pool.Get();
            pool.Release(instance);
            UnityEngine.Object.DestroyImmediate(instance.gameObject);

            BoxCollider replacement = pool.Get();
            _cleanup.Add(replacement.gameObject);

            Assert.That(replacement != null, Is.True);
            Assert.That(replacement.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void Prewarm_CreatesInactiveInstances()
        {
            Transform root = CreatePrefabAndRoot(out BoxCollider prefab);
            ComponentPool<BoxCollider> pool = new ComponentPool<BoxCollider>(prefab, root, prewarmCount: 2);

            Assert.That(pool.CountInactive, Is.EqualTo(2));
            Assert.That(root.childCount, Is.EqualTo(2));
            for (int i = 0; i < root.childCount; i++)
            {
                Assert.That(root.GetChild(i).gameObject.activeSelf, Is.False);
            }
        }

        [Test]
        public void Clear_DestroysInactiveInstances()
        {
            Transform root = CreatePrefabAndRoot(out BoxCollider prefab);
            ComponentPool<BoxCollider> pool = new ComponentPool<BoxCollider>(prefab, root, prewarmCount: 2);

            pool.Clear();

            Assert.That(pool.CountInactive, Is.Zero);
            Assert.That(root.childCount, Is.Zero);
        }
    }
}
