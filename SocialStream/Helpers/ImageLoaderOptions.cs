using System;

namespace SocialStream.Helpers
{
    /// <summary>
    /// Options to pass to the background worker to tell it how to load the image.
    /// </summary>
    internal class ImageLoaderOptions
    {
        /// <summary>
        /// Gets or sets the URI to load.
        /// </summary>
        /// <value>The URI to load.</value>
        public Uri Uri { get; set; }

        /// <summary>
        /// Gets or sets the width to decode the image to. If -1, use the natural size of the image.
        /// </summary>
        /// <value>The width to decode the image to. If -1, use the natural size of the image.</value>
        public int DecodePixelWidth { get; set; }
    }
}
