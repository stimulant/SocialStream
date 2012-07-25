using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Windows;
using System.Xml.Linq;
using FeedProcessor.Enums;
using FeedProcessor.Net;

namespace FeedProcessor.FeedItems
{
    /// <summary>
    /// Represents a feed item whose content is an image.
    /// </summary>
    public class ImageFeedItem : FeedItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFeedItem"/> class.
        /// </summary>
        public ImageFeedItem()
        {
            ContentType = ContentType.Image;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageFeedItem"/> class.
        /// </summary>
        /// <param name="feedItem">A base FeedItem to convert from.</param>
        internal ImageFeedItem(FeedItem feedItem)
        {
            Uri = feedItem.Uri;
            Date = feedItem.Date;
            Author = feedItem.Author;
            AvatarUri = feedItem.AvatarUri;
            SourceType = feedItem.SourceType;
            BlockReason = feedItem.BlockReason;
            ContentType = ContentType.Image;
        }

        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }

        /// <summary>
        /// Gets or sets the caption.
        /// </summary>
        /// <value>The caption.</value>
        public string Caption { get; set; }

        /// <summary>
        /// Gets or sets the thumbnail URI.
        /// </summary>
        /// <value>The thumbnail URI.</value>
        public Uri ThumbnailUri { get; set; }

        /// <summary>
        /// Gets or sets the size of the thumbnail bitmap.
        /// </summary>
        /// <value>The size of the thumbnail bitmap.</value>
        public Size ThumbnailSize { get; set; }

        /// <summary>
        /// Gets or sets the sizes available for the image.
        /// </summary>
        /// <value>The sizes available for the image.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "No.")]
        public Dictionary<Size, Uri> Sizes { get; set; }

        #region Utility Methods

        #region GetFlickrImageSizes

        /// <summary>
        /// Gets the image sizes available for a Flickr image.
        /// </summary>
        /// <param name="imageFeedItem">The image feed item.</param>
        /// <param name="flickrApiKey">The flickr API key.</param>
        /// <param name="callback">The callback.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Yes, really want to catch all exceptions here.")]
        public static void GetFlickrImageSizes(ImageFeedItem imageFeedItem, string flickrApiKey, GetFlickrImageSizesCallback callback)
        {
            if (imageFeedItem == null || (imageFeedItem.Sizes != null && imageFeedItem.Sizes.Count > 0) || imageFeedItem.SourceType != SourceType.Flickr)
            {
                callback(imageFeedItem);
            }

            string query = string.Format(CultureInfo.InvariantCulture, "http://api.flickr.com/services/rest/?method=flickr.photos.getSizes&api_key={0}&photo_id={1}", flickrApiKey, imageFeedItem.ServiceId);
            AsyncWebRequest request = new AsyncWebRequest();
            request.Request(new Uri(query));
            request.Result += (sender, e) =>
            {
                if (e.Status != HttpStatusCode.OK)
                {
                    callback(imageFeedItem);
                }

                try
                {
                    XDocument doc = XDocument.Parse(e.Response);
                    imageFeedItem.Sizes = new Dictionary<Size, Uri>();
                    foreach (XElement size in doc.Descendants("size"))
                    {
                        imageFeedItem.Sizes[new Size(int.Parse(size.Attribute("width").Value, CultureInfo.InvariantCulture), int.Parse(size.Attribute("height").Value, CultureInfo.InvariantCulture))] = new Uri(size.Attribute("source").Value);
                    }

                    callback(imageFeedItem);
                }
                catch
                {
                    callback(imageFeedItem);
                }
            };
        }

        #endregion

        #endregion
    }

    /// <summary>
    /// Represents the method that is called when GetFlickrImageSizes completes.
    /// </summary>
    /// <param name="imageFeedItem">The image feed item.</param>
    public delegate void GetFlickrImageSizesCallback(ImageFeedItem imageFeedItem);
}
