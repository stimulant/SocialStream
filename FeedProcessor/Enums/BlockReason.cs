
namespace FeedProcessor.Enums
{
    /// <summary>
    /// Reasons an item can be blocked from the feed.
    /// </summary>
    public enum BlockReason
    {
        /// <summary>
        /// The item is not blocked.
        /// </summary>
        None,

        /// <summary>
        /// The item is blocked due to profanity.
        /// </summary>
        Profanity,

        /// <summary>
        /// The item is blocked due to a negative keyword match.
        /// </summary>
        Keyword,

        /// <summary>
        /// The item is blocked due to a negative author match.
        /// </summary>
        Author,

        /// <summary>
        /// The item is blocked due to a negative URI match.
        /// </summary>
        Uri
    }
}
