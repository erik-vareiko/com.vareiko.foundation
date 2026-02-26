using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Vareiko.Foundation.Audio;

namespace Vareiko.Foundation.Tests.PlayMode
{
    public sealed class AudioServicePlayModeSmokeTests
    {
        [TearDown]
        public void TearDown()
        {
            CleanupAudioServiceRoots();
        }

        [UnityTest]
        public IEnumerator AudioService_CreatesAndDisposesRuntimeRoot()
        {
            AudioService service = new AudioService(null, null);
            service.Initialize();

            yield return null;
            Assert.That(GameObject.Find("[Foundation] AudioService"), Is.Not.Null);

            service.Dispose();
            yield return null;
            Assert.That(GameObject.Find("[Foundation] AudioService"), Is.Null);
        }

        private static void CleanupAudioServiceRoots()
        {
            GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
            for (int i = 0; i < objects.Length; i++)
            {
                GameObject obj = objects[i];
                if (obj != null && obj.name == "[Foundation] AudioService")
                {
                    Object.DestroyImmediate(obj);
                }
            }
        }
    }
}
