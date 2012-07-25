namespace FeedProcessor.Enums
{
    /// <summary>
    /// Defines the order in which items are retrieved from the feed processor.
    /// </summary>
    public enum RetrievalOrder
    {
        /// <summary>
        /// Items will be displayed in roughly chronological order. Whenever a new item arrives, it will be displayed as soon as possible.
        /// </summary>
        Chronological,

        /// <summary>
        /// Items will be displayed randomly.
        /// </summary>
        Random
    }
}
