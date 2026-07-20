namespace SupportPulse.Data.Enums.Message
{
    /// <summary>
    /// Defines the type of a message content.
    /// </summary>
    public enum MessageTypes
    {
        /// <summary>
        /// Plain text message.
        /// </summary>
        PlainText = 1,

        /// <summary>
        /// Message containing only attached files.
        /// </summary>
        AttachFile = 2,

        /// <summary>
        /// Message containing both plain text and attached files.
        /// </summary>
        PlainTextAndAttachFile = 3
    }
}