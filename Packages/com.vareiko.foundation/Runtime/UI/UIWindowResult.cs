namespace Vareiko.Foundation.UI
{
    public readonly struct UIWindowResult
    {
        public readonly string WindowId;
        public readonly UIWindowResultStatus Status;
        public readonly string Payload;

        public UIWindowResult(string windowId, UIWindowResultStatus status, string payload = "")
        {
            WindowId = windowId ?? string.Empty;
            Status = status;
            Payload = payload ?? string.Empty;
        }

        public bool IsConfirmed => Status == UIWindowResultStatus.Confirmed;
    }
}
