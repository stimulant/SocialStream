
namespace FeedProcessor.Enums
{
    /// <summary>
    /// Different types of sources that a news item can come from.
    /// </summary>
    public enum SourceType
    {
        /// <summary>
        /// The item came from Flickr.
        /// </summary>
        Flickr,

        /// <summary>
        /// The item came from twitter.
        /// </summary>
        Twitter,

        /// <summary>
        /// The item came from a news feed.
        /// </summary>
        News,

        /// <summary>
        /// The item came from Facebook.
        /// </summary>
        Facebook
    }
}
