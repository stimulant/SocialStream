using FeedProcessor.Enums;

namespace FeedProcessor.FeedItems
{
    /// <summary>
    /// Represnets a feed item coming from an RSS or ATOM news feed.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "NewsFeed", Justification = "We like it this way.")]
    public class NewsFeedItem : FeedItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewsFeedItem"/> class.
        /// </summary>
        public NewsFeedItem()
        {
            ContentType = ContentType.News;
        }

        /// <summary>
        /// Gets or sets the title of the post.
        /// </summary>
        /// <value>The title of the post.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the summary of the post.
        /// </summary>
        /// <value>The summary of the post.</value>
        public string Summary { get; set; }

        /// <summary>
        /// Gets or sets the body of the post. This can contain HTML.
        /// </summary>
        /// <value>The body of the post.</value>
        public string Body { get; set; }
    }
}
