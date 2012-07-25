using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml.Linq;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;
using FeedProcessor.Net;

namespace FeedProcessor.Feeds
{
    /// <summary>
    /// Handles the request and processing of Flickr images which are specified by tag names.
    /// </summary>
    internal class FlickrSearchFeed : Feed
    {
        /// <summary>
        /// How many items to get per page of results.
        /// </summary>
        protected const int PageSize = 500;

        /// <summary>
        /// A mapping of Flickr user ids to user names.
        /// </summary>
        private static Dictionary<string, string> _flickrUserNameCache = new Dictionary<string, string>();

        /// <summary>
        /// The format of photo page URIs.
        /// </summary>
        private const string SourceUriFormatString = "http://www.flickr.com/photos/{0}/{1}";

        /// <summary>
        /// The format of avatar image URIs.
        /// </summary>
        private const string AvatarUriFormatString = "http://farm{0}.static.flickr.com/{1}/buddyicons/{2}.jpg";

        /// <summary>
        /// The format of thumbnail image URIs.
        /// </summary>
        private const string ThumbnailUriFormatString = @"http://farm{0}.static.flickr.com/{1}/{2}_{3}_m.jpg";

        /// <summary>
        /// The upload date for the last item retrieved, passed in the query to minimize duplicate results.
        /// </summary>
        private long _minUploadDate;

        /// <summary>
        /// The minimum polling interval for the Flickr service. http://developer.yahoo.com/flickr/
        /// </summary>
        protected static readonly TimeSpan MinPollingInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Initializes a new instance of the <see cref="FlickrSearchFeed"/> class.
        /// </summary>
        /// <param name="flickrApiKey">The flickr API key.</param>
        /// <param name="pollInterval">The requested polling interval.</param>
        /// <param name="minDate">The minimum allowed date of a returned item.</param>
        internal FlickrSearchFeed(string flickrApiKey, TimeSpan pollInterval, DateTime minDate)
            : base(TimeSpan.FromMilliseconds(Math.Max(pollInterval.TotalMilliseconds, MinPollingInterval.TotalMilliseconds)), minDate)
        {
            FlickrApiKey = flickrApiKey;
            SourceType = SourceType.Flickr;
        }

        /// <summary>
        /// Builds the query that is passed to the feed service.
        /// </summary>
        /// <returns>The query URI.</returns>
        internal override Uri BuildQuery()
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, "http://api.flickr.com/services/rest/?method=flickr.photos.search&api_key={0}&page=1&sort=date-posted-desc&per_page={1}&extras=icon_server,description,date_upload,date_taken,owner_name,path_alias&tags={2}&min_upload_date={3}", FlickrApiKey, PageSize, Query, _minUploadDate));
        }

        /// <summary>
        /// Returns a time after which it's ok to make another query.
        /// </summary>
        /// <param name="httpStatusCode">The HTTP status code returned from the last attempt.</param>
        /// <returns>
        /// The time after which it's ok to make another query.
        /// </returns>
        internal override DateTime RetryTime(HttpStatusCode httpStatusCode)
        {
            RaiseSourceStatusUpdated(httpStatusCode == HttpStatusCode.OK);

            if (httpStatusCode != HttpStatusCode.OK)
            {
                switch (httpStatusCode)
                {
                    // Flickr is down or being upgraded
                    case HttpStatusCode.BadGateway:
                        return DateTime.Now.AddMinutes(5);

                    // Servers are overloaded
                    case HttpStatusCode.ServiceUnavailable:

                    // Error on their end
                    case HttpStatusCode.InternalServerError:
                        return DateTime.Now.AddMinutes(2);

                    // Some other error                    
                    default:
                        return DateTime.Now.AddMinutes(10);
                }
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Processes the response from the feed service.
        /// </summary>
        /// <param name="response">response from the feed service.</param>
        internal override void ProcessResponse(string response)
        {
            if (response.Contains("<rsp stat=\"fail\">"))
            {
#if DEBUG
                throw new InvalidOperationException(response);
#else
                return;
#endif
            }

            XDocument document = XDocument.Parse(response);
            foreach (XElement node in document.Element("rsp").Element("photos").Elements("photo"))
            {
                _flickrUserNameCache[node.Attribute("owner").Value] = node.Attribute("ownername").Value;
                _minUploadDate = Math.Max(_minUploadDate, long.Parse(node.Attribute("dateupload").Value, CultureInfo.InvariantCulture));

                ImageFeedItem feedItem = new ImageFeedItem
                {
                    // Build the page URI with the user photo url, since that's what a user would put in the ban list.
                    Uri = new Uri(string.Format(
                        CultureInfo.InvariantCulture,
                        SourceUriFormatString,
                        string.IsNullOrEmpty(node.Attribute("pathalias").Value) ? node.Attribute("owner").Value : node.Attribute("pathalias").Value,
                        node.Attribute("id").Value)),
                    
                    Date = FlickrSearchFeed.ConvertFromUnixTimestamp(int.Parse(node.Attribute("dateupload").Value, CultureInfo.InvariantCulture)),
                    
                    Author = HttpUtility.HtmlDecode(_flickrUserNameCache[node.Attribute("owner").Value]),
                    
                    AvatarUri = new Uri(string.Format(
                        CultureInfo.InstalledUICulture,
                        AvatarUriFormatString,
                        node.Attribute("iconfarm").Value,
                        node.Attribute("iconserver").Value,
                        node.Attribute("owner").Value)),
                    
                    SourceType = SourceType.Flickr,
                    
                    Title = HttpUtility.HtmlDecode(node.Attribute("title").Value),
                    
                    Caption = HttpUtility.HtmlDecode(StripHtml(node.Element("description") != null ? node.Element("description").Value : string.Empty)),
                    
                    ThumbnailUri = new Uri(string.Format(
                        CultureInfo.InvariantCulture,
                        ThumbnailUriFormatString,
                        node.Attribute("farm").Value,
                        node.Attribute("server").Value,
                        node.Attribute("id").Value,
                        node.Attribute("secret").Value)),
                    
                    ServiceId = node.Attribute("id").Value
                };

                if (feedItem.Date < MinDate)
                {
                    continue;
                }

                RaiseGotNewFeedItem(feedItem);
            }
        }

        /// <summary>
        /// Gets or sets the flickr API key.
        /// </summary>
        /// <value>The flickr API key.</value>
        protected string FlickrApiKey { get; set; }

        #region Utility Methods

        #region GetFlickrUserIdFromUserName

        /// <summary>
        /// Represents the method that is called when GetFlickrUserIdFromUserName completes.
        /// </summary>
        /// <param name="userId">The user id.</param>
        internal delegate void GetFlickrUserIdFromUserNameCallback(string userId);

        /// <summary>
        /// Given a Flickr user name, return their user id.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="apiKey">The API key.</param>
        /// <param name="callback">The callback.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Yes, really want to catch all exceptions here.")]
        internal static void GetFlickrUserIdFromUserName(string username, string apiKey, GetFlickrUserIdFromUserNameCallback callback)
        {
            if (_flickrUserNameCache.ContainsValue(username))
            {
                callback((from kvp in _flickrUserNameCache where kvp.Value == username select kvp.Key).FirstOrDefault());
                return;
            }

            string query = string.Format(CultureInfo.InvariantCulture, "http://api.flickr.com/services/rest/?method=flickr.people.findByUsername&api_key={0}&username={1}", apiKey, username);
            AsyncWebRequest request = new AsyncWebRequest();
            request.Request(new Uri(query));
            request.Result += (sender, e) =>
            {
                if (e.Status != HttpStatusCode.OK)
                {
                    callback(null);
                }

                try
                {
                    string userid = XDocument.Parse(e.Response).Element("rsp").Element("user").Attribute("nsid").Value;
                    _flickrUserNameCache[userid] = username;
                    callback(userid);
                }
                catch
                {
                    callback(null);
                }
            };
        }

        #endregion

        /// <summary>
        /// Converts a UNIX timestamp to a DateTime.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns>The timestamp converted to a DateTime.</returns>
        internal static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timestamp);
        }

        #endregion
    }
}
