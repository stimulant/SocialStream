using System;
using System.Globalization;
using System.IO;
using System.ServiceModel.Syndication;
using System.Web;
using System.Xml;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;

namespace FeedProcessor.Feeds
{
    /// <summary>
    /// Handles the request and processing of Twitter user queries.
    /// </summary>
    internal class TwitterUserFeed : TwitterSearchFeed
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterUserFeed"/> class.
        /// </summary>
        /// <param name="pollInterval">The requested polling interval.</param>
        /// <param name="minDate">The minimum allowed date of a returned item.</param>
        internal TwitterUserFeed(TimeSpan pollInterval, DateTime minDate)
            : base(TimeSpan.FromMilliseconds(Math.Max(pollInterval.TotalMilliseconds, MinPollingInterval.TotalMilliseconds)), minDate)
        {
            PageSize = 200;
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
                return new Uri(string.Format(CultureInfo.InvariantCulture, "http://api.twitter.com/1/statuses/user_timeline.atom?screen_name={0}&count={1}&page=1&since_id={2}", Query, PageSize, LastId));
            }
            else
            {
                return new Uri(string.Format(CultureInfo.InvariantCulture, "http://api.twitter.com/1/statuses/user_timeline.atom?screen_name={0}&count={1}&page=1", Query, PageSize));
            }
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

                        // The author name isn't actually in the response -- it's in the text as "endquote: tweet text"
                        Author = HttpUtility.HtmlDecode(syndicationItem.Title.Text.Substring(0, syndicationItem.Title.Text.IndexOf(':'))),
                        AvatarUri = syndicationItem.Links[1].Uri,
                        SourceType = SourceType.Twitter,
                        Status = HttpUtility.HtmlDecode(StripHtml(syndicationItem.Title.Text.Substring(syndicationItem.Title.Text.IndexOf(':') + 2)))
                    });
                }
            }
        }
    }
}
