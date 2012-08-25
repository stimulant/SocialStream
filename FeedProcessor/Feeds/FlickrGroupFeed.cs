using System;
using System.Globalization;

namespace FeedProcessor.Feeds
{
    /// <summary>
    /// Handles the request and processing of Flickr images which are specified by group names.
    /// </summary>
    internal class FlickrGroupFeed : FlickrSearchFeed
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FlickrGroupFeed"/> class.
        /// </summary>
        /// <param name="flickrApiKey">The flickr API key.</param>
        /// <param name="pollInterval">The requested polling interval.</param>
        /// <param name="minDate">The minimum allowed date of a returned item.</param>
        internal FlickrGroupFeed(string flickrApiKey, TimeSpan pollInterval, DateTime minDate)
            : base(flickrApiKey, TimeSpan.FromMilliseconds(Math.Max(pollInterval.TotalMilliseconds, MinPollingInterval.TotalMilliseconds)), minDate)
        {
        }

        /// <summary>
        /// Builds the query that is passed to the feed service.
        /// </summary>
        /// <returns>The query URI.</returns>
        internal override Uri BuildQuery()
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, "http://api.flickr.com/services/rest/?method=flickr.groups.pools.getPhotos&api_key={0}&page=1&sort=date-posted-desc&per_page={1}&extras=icon_server,description,date_upload,date_taken,owner_name,path_alias&group_id={2}", FlickrApiKey, PageSize, Query));
        }
    }
}