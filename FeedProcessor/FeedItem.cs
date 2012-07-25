using System;
using FeedProcessor.Enums;

namespace FeedProcessor
{
    /// <summary>
    /// Represents an item in a feed.
    /// </summary>
    public abstract class FeedItem
    {
        /// <summary>
        /// Gets or sets the source URI of the item.
        /// </summary>
        /// <value>The source URI of the item.</value>
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the date on which the item was published.
        /// </summary>
        /// <value>The date on which the item was published.</value>
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the author of the item.
        /// </summary>
        /// <value>The author of the item.</value>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets the URI to the author's avatar image.
        /// </summary>
        /// <value>The URI to the author's avatar image.</value>
        public Uri AvatarUri { get; set; }

        /// <summary>
        /// Gets or sets the type of the source.
        /// </summary>
        /// <value>The type of the source.</value>
        public SourceType SourceType { get; set; }

        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>The type of the content.</value>
        public ContentType ContentType { get; set; }

        /// <summary>
        /// Gets or sets the reason this item is blocked from the results.
        /// </summary>
        /// <value>The reason this item is blocked from the results.</value>
        public BlockReason BlockReason { get; set; }

        /// <summary>
        /// Gets or sets the unique ID provided by the web service for this item.
        /// </summary>
        /// <value>The unique ID provided by the web service for this item.</value>
        public string ServiceId { get; set; }
    }
}
