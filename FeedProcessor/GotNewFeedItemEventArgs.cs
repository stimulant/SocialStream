using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeedProcessor
{
    /// <summary>
    /// The EventArgs object passed along with the Feed.GotNewFeedItem event.
    /// </summary>
    internal class GotNewFeedItemEventArgs : EventArgs
    {
        /// <summary>
        /// The new feed item.
        /// </summary>
        internal readonly FeedItem FeedItem;

        /// <summary>
        /// Initializes a new instance of the <see cref="GotNewFeedItemEventArgs"/> class.
        /// </summary>
        /// <param name="feedItem">The new feed item.</param>
        internal GotNewFeedItemEventArgs(FeedItem feedItem)
        {
            FeedItem = feedItem;
        }
    }
}
