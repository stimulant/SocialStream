using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.ServiceModel.Syndication;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;
using FeedProcessor.Net;

namespace FeedProcessor.Feeds
{
    /// <summary>
    /// Handles the request and processing of Twitter search queries.
    /// </summary>
    internal class TwitterSearchFeed : Feed
    {
        /// <summary>
        /// Gets or sets the number of items to request for earch search.
        /// </summary>
        /// <value>the number of items to request for earch search.</value>
        protected int PageSize { get; set; }

        /// <summary>
        /// Gets or sets the last item retrieved, used in the query to minimize duplicate results.
        /// </summary>
        /// <value>The last item retrieved, used in the query to minimize duplicate results.</value>
        public long LastId { get; set; }

        /// <summary>
        /// The minimum polling interval for the twitter service. http://dev.twitter.com/pages/rate-limiting
        /// </summary>
        protected static readonly TimeSpan MinPollingInterval = TimeSpan.FromHours(1.0 / 150.0);

        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterSearchFeed"/> class.
        /// </summary>
        /// <param name="pollInterval">The requested polling interval.</param>
        /// <param name="minDate">The minimum allowed date of a returned item.</param>
        internal TwitterSearchFeed(TimeSpan pollInterval, DateTime minDate)
            : base(TimeSpan.FromMilliseconds(Math.Max(pollInterval.TotalMilliseconds, MinPollingInterval.TotalMilliseconds)), minDate)
        {
            PageSize = 100;
            SourceType = SourceType.Twitter;
        }

        /// <summary>
        /// Builds the query that is passed to the feed service.
        /// </summary>
        /// <returns>The query URI.</returns>
        internal override Uri BuildQuery()
        {
            if (LastId > 0)
            {
                return new Uri(string.Format(CultureInfo.InvariantCulture, "https://search.twitter.com/search.atom?q={0}&page=1&rpp={1}&since_id={2}&result_type=recent", Query, PageSize, LastId));
            }
            else
            {
                return new Uri(string.Format(CultureInfo.InvariantCulture, "https://search.twitter.com/search.atom?q={0}&page=1&rpp={1}&result_type=recent", Query, PageSize));
            }
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
            RaiseSourceStatusUpdated(httpStatusCode == (HttpStatusCode)420 || httpStatusCode == HttpStatusCode.OK);

            if (httpStatusCode != HttpStatusCode.OK)
            {
                switch (httpStatusCode)
                {
                    // Twitter's amusingly named "Enhance Your Calm" response code telling us we're being rate limited
                    case (HttpStatusCode)420:

                    // Twitter is down or being upgraded
                    case HttpStatusCode.BadGateway:
                    case HttpStatusCode.RequestTimeout:
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
        internal override void ProcessResponse(object responseObject)
        {
            string response = responseObject.ToString();

            using (StringReader stringReader = new StringReader(response))
            {
                SyndicationFeed feed = SyndicationFeed.Load(XmlReader.Create(stringReader));
                foreach (SyndicationItem syndicationItem in feed.Items)
                {
                    long id = long.Parse(syndicationItem.Links[0].Uri.AbsoluteUri.Substring(syndicationItem.Links[0].Uri.AbsoluteUri.LastIndexOf('/') + 1), CultureInfo.InvariantCulture);
                    LastId = Math.Max(id, LastId);

                    DateTime publishDate = syndicationItem.PublishDate.DateTime.ToLocalTime();

                    if (publishDate < MinDate)
                    {
                        continue;
                    }

                    RaiseGotNewFeedItem(new StatusFeedItem
                    {
                        Uri = syndicationItem.Links[0].Uri,
                        Date = publishDate,

                        // The author name is like "endquote (Josh Santangelo)", so parse the author from the URI.
                        Author = HttpUtility.HtmlDecode(syndicationItem.Authors[0].Uri.Substring(syndicationItem.Authors[0].Uri.LastIndexOf('/') + 1)),
                        AvatarUri = syndicationItem.Links[1].Uri,
                        SourceType = SourceType.Twitter,
                        Status = HttpUtility.HtmlDecode(StripHtml(syndicationItem.Title.Text))
                    });
                }
            }
        }

        #region Utility Methods

        #region GetImageLink

        /// <summary>
        /// Represents the method that is called when GetImageLink completes.
        /// </summary>
        /// <param name="imageLink">The link to the image.</param>
        /// <param name="newStatus">The modified status message, with the image service link removed.</param>
        internal delegate void GetImageLinkCallback(Uri imageLink, string newStatus);

        /// <summary>
        /// Parses a status message for known URLs for image hosting services, and returns a URL to the image.
        /// </summary>
        /// <param name="status">The status text.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>Whether or not an image URL was found.</returns>
        internal static bool GetImageLink(string status, GetImageLinkCallback callback)
        {
            if (ExtractYFrogImageLink(status, callback))
            {
                return true;
            }

            if (ExtractTwitPicImageLink(status, callback))
            {
                return true;
            }

            if (ExtractTweetPhotoImageLink(status, callback))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Extracts image links to the TweetPhoto service from status messages.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>True if an image link was found, false otherwise.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want all exceptions.")]
        private static bool ExtractTweetPhotoImageLink(string status, GetImageLinkCallback callback)
        {
            Match match = Regex.Match(status, @"http://(www\.)?tweetphoto.com/(\d+)\b", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            status = status.Replace(match.Groups[0].Value, string.Empty).Trim();
            AsyncWebRequest request = new AsyncWebRequest();
            request.Request(new Uri("http://tweetphotoapi.com/api/tpapi.svc/photos/" + match.Groups[2].Value));
            request.Result += (sender, e) =>
            {
                if (e.Status != HttpStatusCode.OK)
                {
                    callback(null, status);
                }

                try
                {
                    using (StringReader stringReader = new StringReader(e.Response))
                    {
                        XmlTextReader reader = new XmlTextReader(stringReader);
                        while (reader.Read())
                        {
                            if (reader.MoveToContent() == XmlNodeType.Element && reader.Name == "BigImageUrl")
                            {
                                callback(new Uri(reader.ReadString()), status);
                            }
                        }
                    }
                }
                catch
                {
                    callback(null, status);
                }
            };

            return true;
        }

        /// <summary>
        /// Extracts image links to the TwitPic service from status messages.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>True if an image link was found, false otherwise.</returns>
        private static bool ExtractTwitPicImageLink(string status, GetImageLinkCallback callback)
        {
            Match match = Regex.Match(status, @"http://(www\.)?twitpic.com/(\w+)\b", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            string twitpic = match.Groups[0].Value;
            string twitpicId = match.Groups[2].Value;
            status = status.Replace(twitpic, string.Empty).Trim();

            AsyncWebRequest request = new AsyncWebRequest();
            request.Request(new Uri(string.Format(CultureInfo.InvariantCulture, "http://api.twitpic.com/2/media/show.xml?id={0}", twitpicId)));
            request.Result += (sender, e) =>
            {
                if (e.Status != HttpStatusCode.OK)
                {
                    callback(null, status);
                }
                else
                {
                    callback(new Uri(@"http://twitpic.com/show/large/" + match.Groups[2].Value), status);
                }
            };

            return true;
        }

        /// <summary>
        /// Extracts image links to the YFrog service from status messages.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="callback">The callback.</param>
        /// <returns>True if an image link was found, false otherwise.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want all exceptions.")]
        private static bool ExtractYFrogImageLink(string status, GetImageLinkCallback callback)
        {
            Match match = Regex.Match(status, @"http://(www\.)?yfrog.com/(\w+)\b", RegexOptions.IgnoreCase);
            if (!match.Success)
            {
                return false;
            }

            status = status.Replace(match.Groups[0].Value, string.Empty).Trim();
            AsyncWebRequest request = new AsyncWebRequest();
            request.Request(new Uri("http://yfrog.com/api/xmlInfo?path=" + match.Groups[2].Value));
            request.Result += (sender, e) =>
            {
                if (e.Status != HttpStatusCode.OK)
                {
                    callback(null, status);
                }

                try
                {
                    using (StringReader stringReader = new StringReader(e.Response))
                    {
                        XmlTextReader xmlTextReader = new XmlTextReader(stringReader);
                        while (xmlTextReader.Read())
                        {
                            if (xmlTextReader.MoveToContent() == XmlNodeType.Element && xmlTextReader.Name == "image_link")
                            {
                                string uriString = xmlTextReader.ReadString();
                                string extension = Path.GetExtension(uriString).ToUpper(CultureInfo.InvariantCulture);
                                if (extension == ".JPG" || extension == ".PNG" || extension == ".GIF")
                                {
                                    // sometimes there will be video files, and we don't want that.
                                    callback(new Uri(uriString), status);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    callback(null, status);
                }
            };

            return true;
        }

        #endregion

        #endregion
    }
}
