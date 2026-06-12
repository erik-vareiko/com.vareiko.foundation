using System;
using System.Collections.Generic;

namespace Vareiko.Foundation.Samples.VerticalSlice
{
    /// <summary>
    /// Save payload for the slice. The dictionary is intentional: it round-trips through the
    /// 3.0 Newtonsoft default serializer (JsonUtility could not handle it).
    /// </summary>
    [Serializable]
    public sealed class SliceProfile
    {
        public int RunsCompleted;
        public Dictionary<string, int> Currencies = new Dictionary<string, int>();
    }
}
