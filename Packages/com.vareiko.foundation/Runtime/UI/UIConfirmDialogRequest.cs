namespace Vareiko.Foundation.UI
{
    public readonly struct UIConfirmDialogRequest
    {
        public readonly string Title;
        public readonly string Message;
        public readonly string ConfirmButtonLabel;
        public readonly string CancelButtonLabel;
        public readonly string ConfirmPayload;
        public readonly string CancelPayload;

        public UIConfirmDialogRequest(
            string title,
            string message,
            string confirmButtonLabel = "",
            string cancelButtonLabel = "",
            string confirmPayload = "",
            string cancelPayload = "")
        {
            Title = title ?? string.Empty;
            Message = message ?? string.Empty;
            ConfirmButtonLabel = confirmButtonLabel ?? string.Empty;
            CancelButtonLabel = cancelButtonLabel ?? string.Empty;
            ConfirmPayload = confirmPayload ?? string.Empty;
            CancelPayload = cancelPayload ?? string.Empty;
        }
    }
}
