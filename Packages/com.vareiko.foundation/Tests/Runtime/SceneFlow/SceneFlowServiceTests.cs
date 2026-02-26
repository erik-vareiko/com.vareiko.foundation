using System;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using Vareiko.Foundation.SceneFlow;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.SceneFlow
{
    public sealed class SceneFlowServiceTests
    {
        [Test]
        public void LoadSceneAsync_WhenSceneNameEmpty_ThrowsArgumentException()
        {
            FakeTimeProvider time = new FakeTimeProvider();
            SceneFlowService service = new SceneFlowService(time, null);

            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.LoadSceneAsync(string.Empty, LoadSceneMode.Single);
            });
        }

        [Test]
        public void UnloadSceneAsync_WhenSceneNameEmpty_ThrowsArgumentException()
        {
            FakeTimeProvider time = new FakeTimeProvider();
            SceneFlowService service = new SceneFlowService(time, null);

            Assert.ThrowsAsync<ArgumentException>(async () =>
            {
                await service.UnloadSceneAsync(string.Empty);
            });
        }
    }
}
