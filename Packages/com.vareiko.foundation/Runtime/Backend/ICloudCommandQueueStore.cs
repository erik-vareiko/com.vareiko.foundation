using System.Collections.Generic;

namespace Vareiko.Foundation.Backend
{
    public interface ICloudCommandQueueStore
    {
        IReadOnlyList<CloudCommandQueueItem> Load();
        void Save(IReadOnlyList<CloudCommandQueueItem> queue);
        void Clear();
    }
}
