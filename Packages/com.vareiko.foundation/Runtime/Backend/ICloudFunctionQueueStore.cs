using System.Collections.Generic;

namespace Vareiko.Foundation.Backend
{
    public interface ICloudFunctionQueueStore
    {
        IReadOnlyList<CloudFunctionQueueItem> Load();
        void Save(IReadOnlyList<CloudFunctionQueueItem> queue);
        void Clear();
    }
}
