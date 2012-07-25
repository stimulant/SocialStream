using FeedProcessor.Enums;

namespace FeedProcessor.FeedItems
{
    /// <summary>
    /// Represents a feed item whose content is a status message.
    /// </summary>
    public class StatusFeedItem : FeedItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StatusFeedItem"/> class.
        /// </summary>
        public StatusFeedItem()
        {
            ContentType = ContentType.Status;
        }

        /// <summary>
        /// Gets or sets the status message.
        /// </summary>
        /// <value>The status message.</value>
        public string Status { get; set; }
    }
}
