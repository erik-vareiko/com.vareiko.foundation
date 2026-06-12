using System;
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

            string payload = value ?? string.Empty;
            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, payload, Encoding.UTF8);

            if (File.Exists(path))
            {
                string backupPath = path + ".swap";
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                try
                {
                    File.Replace(tempPath, path, backupPath, true);
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                }
                catch (PlatformNotSupportedException)
                {
                    File.Delete(path);
                    File.Move(tempPath, path);
                }
                catch (IOException)
                {
                    File.Delete(path);
                    File.Move(tempPath, path);
                }
            }
            else
            {
                File.Move(tempPath, path);
            }
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
