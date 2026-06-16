using System;
using NUnit.Framework;

namespace Vareiko.Foundation.Tests.Primitives
{
    public sealed class DisposablesTests
    {
        [Test]
        public void DisposableAction_RunsOnce()
        {
            int calls = 0;
            DisposableAction disposable = new DisposableAction(() => calls++);

            disposable.Dispose();
            disposable.Dispose();

            Assert.That(calls, Is.EqualTo(1));
        }

        [Test]
        public void DisposableAction_NullAction_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _ = new DisposableAction(null));
        }

        [Test]
        public void CompositeDisposable_DisposesAllItems()
        {
            int calls = 0;
            CompositeDisposable bag = new CompositeDisposable();
            bag.Add(new DisposableAction(() => calls++));
            bag.Add(new DisposableAction(() => calls++));

            bag.Dispose();

            Assert.That(calls, Is.EqualTo(2));
            Assert.That(bag.IsDisposed, Is.True);
            Assert.That(bag.Count, Is.Zero);
        }

        [Test]
        public void CompositeDisposable_AddAfterDispose_DisposesImmediately()
        {
            CompositeDisposable bag = new CompositeDisposable();
            bag.Dispose();

            int calls = 0;
            bag.Add(new DisposableAction(() => calls++));

            Assert.That(calls, Is.EqualTo(1));
            Assert.That(bag.Count, Is.Zero);
        }

        [Test]
        public void CompositeDisposable_Clear_DisposesButStaysUsable()
        {
            int calls = 0;
            CompositeDisposable bag = new CompositeDisposable();
            bag.Add(new DisposableAction(() => calls++));

            bag.Clear();
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(bag.IsDisposed, Is.False);

            bag.Add(new DisposableAction(() => calls++));
            Assert.That(bag.Count, Is.EqualTo(1));
        }

        [Test]
        public void CompositeDisposable_IgnoresNull()
        {
            CompositeDisposable bag = new CompositeDisposable();
            bag.Add(null);
            Assert.That(bag.Count, Is.Zero);
        }
    }
}
