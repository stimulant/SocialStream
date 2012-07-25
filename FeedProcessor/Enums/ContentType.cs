using System;

namespace FeedProcessor.Enums
{
    /// <summary>
    /// Different types of content that a feed item can describe.
    /// </summary>
    [Flags]
    public enum ContentType
    {
        /// <summary>
        /// The item is an image.
        /// </summary>
        Image = 1,
        
        /// <summary>
        /// The item is a status message.
        /// </summary>
        Status = 2,
        
        /// <summary>
        /// The item is a news post.
        /// </summary>
        News = 4
    }  
}
