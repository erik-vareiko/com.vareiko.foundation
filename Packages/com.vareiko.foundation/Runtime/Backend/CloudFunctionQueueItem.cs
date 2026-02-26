namespace Vareiko.Foundation.Backend
{
    public readonly struct CloudFunctionQueueItem
    {
        public readonly string FunctionName;
        public readonly string PayloadJson;

        public CloudFunctionQueueItem(string functionName, string payloadJson)
        {
            FunctionName = functionName ?? string.Empty;
            PayloadJson = payloadJson ?? string.Empty;
        }
    }
}
