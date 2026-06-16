using System;
using System.Collections.Generic;
using NUnit.Framework;
using Vareiko.Foundation.Pooling;

namespace Vareiko.Foundation.Tests.Pooling
{
    public sealed class ObjectPoolTests
    {
        private sealed class Item
        {
        }

        [Test]
        public void Get_CreatesThroughFactory_AndReleaseGetReuses()
        {
            int created = 0;
            ObjectPool<Item> pool = new ObjectPool<Item>(() =>
            {
                created++;
                return new Item();
            });

            Item first = pool.Get();
            pool.Release(first);
            Item second = pool.Get();

            Assert.That(created, Is.EqualTo(1));
            Assert.That(second, Is.SameAs(first));
        }

        [Test]
        public void Callbacks_FireOnGetReleaseDestroy()
        {
            List<string> calls = new List<string>();
            ObjectPool<Item> pool = new ObjectPool<Item>(
                () => new Item(),
                onGet: _ => calls.Add("get"),
                onRelease: _ => calls.Add("release"),
                onDestroy: _ => calls.Add("destroy"),
                maxSize: 1);

            Item first = pool.Get();
            Item second = pool.Get();
            pool.Release(first);
            // Pool already holds one inactive item -> overflow is destroyed.
            pool.Release(second);

            Assert.That(calls, Is.EqualTo(new[] { "get", "get", "release", "release", "destroy" }));
        }

        [Test]
        public void Release_UnknownItem_Throws()
        {
            ObjectPool<Item> pool = new ObjectPool<Item>(() => new Item());
            Assert.Throws<InvalidOperationException>(() => pool.Release(new Item()));
        }

        [Test]
        public void Release_Twice_Throws()
        {
            ObjectPool<Item> pool = new ObjectPool<Item>(() => new Item());
            Item item = pool.Get();
            pool.Release(item);
            Assert.Throws<InvalidOperationException>(() => pool.Release(item));
        }

        [Test]
        public void Counts_TrackActiveAndInactive()
        {
            ObjectPool<Item> pool = new ObjectPool<Item>(() => new Item());
            Item item = pool.Get();
            Assert.That(pool.CountActive, Is.EqualTo(1));
            Assert.That(pool.CountInactive, Is.Zero);

            pool.Release(item);
            Assert.That(pool.CountActive, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void Prewarm_FillsInactive()
        {
            int created = 0;
            ObjectPool<Item> pool = new ObjectPool<Item>(() =>
            {
                created++;
                return new Item();
            }, prewarmCount: 3);

            Assert.That(created, Is.EqualTo(3));
            Assert.That(pool.CountInactive, Is.EqualTo(3));
        }

        [Test]
        public void GetScoped_ReleasesOnDispose()
        {
            ObjectPool<Item> pool = new ObjectPool<Item>(() => new Item());
            Item item;
            using (pool.GetScoped(out item))
            {
                Assert.That(pool.CountActive, Is.EqualTo(1));
            }

            Assert.That(pool.CountActive, Is.Zero);
            Assert.That(pool.CountInactive, Is.EqualTo(1));
        }

        [Test]
        public void Clear_DestroysInactiveOnly()
        {
            int destroyed = 0;
            ObjectPool<Item> pool = new ObjectPool<Item>(
                () => new Item(),
                onDestroy: _ => destroyed++,
                prewarmCount: 2);
            Item active = pool.Get();

            pool.Clear();

            Assert.That(destroyed, Is.EqualTo(1), "Prewarmed 2, one taken — only the remaining inactive one is destroyed.");
            Assert.That(pool.CountActive, Is.EqualTo(1));
            pool.Release(active);
        }
    }
}
