using System.IO;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Save
{
    public sealed class FileSaveStorage : ISaveStorage
    {
        public async UniTask<bool> ExistsAsync(string path, CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();
            return File.Exists(path);
        }

        public async UniTask<string> ReadTextAsync(string path, CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();
            if (!File.Exists(path))
            {
                return null;
            }

            return File.ReadAllText(path, Encoding.UTF8);
        }

        public async UniTask WriteTextAsync(string path, string value, CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();

            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, value ?? string.Empty, Encoding.UTF8);
        }

        public async UniTask DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            await UniTask.SwitchToThreadPool();
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
