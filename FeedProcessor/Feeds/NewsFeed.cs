using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel.Syndication;
using System.Web;
using System.Xml;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;

namespace FeedProcessor.Feeds
{
    /// <summary>
    /// Handles the request and processing of news feeds.
    /// </summary>
    internal class NewsFeed : Feed
    {
        /// <summary>
        /// The minimum time allowed between requests to the service.
        /// </summary>
        private static readonly TimeSpan _minPollingInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Initializes a new instance of the <see cref="NewsFeed"/> class.
        /// </summary>
        /// <param name="pollInterval">The requested polling interval.</param>
        /// <param name="minDate">The minimum allowed date of a returned item.</param>
        internal NewsFeed(TimeSpan pollInterval, DateTime minDate)
            : base(TimeSpan.FromMilliseconds(Math.Max(pollInterval.TotalMilliseconds, _minPollingInterval.TotalMilliseconds)), minDate)
        {
            SourceType = SourceType.News;
        }

        /// <summary>
        /// Builds the query that is passed to the feed service.
        /// </summary>
        /// <returns>The query URI.</returns>
        internal override Uri BuildQuery()
        {
            return new Uri(Query);
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
                    // Don't keep trying on a 404
                    case HttpStatusCode.NotFound:
                        return DateTime.MaxValue;

                    // Some other error                    
                    default:
                        return DateTime.Now.AddHours(2);
                }
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Processes the response from the feed service.
        /// </summary>
        /// <param name="response">response from the feed service.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Sorry for the complexity."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Yes, really want to catch all exceptions here.")]
        internal override void ProcessResponse(string response)
        {
            try
            {
                using (StringReader stringReader = new StringReader(response))
                {
                    XmlReader xmlReader = XmlReader.Create(stringReader);
                    SyndicationFeed feed = SyndicationFeed.Load(xmlReader);
                    if (feed != null)
                    {
                        foreach (SyndicationItem syndicationItem in feed.Items)
                        {
                            NewsFeedItem feedItem = new NewsFeedItem { SourceType = SourceType.News };
                            feedItem.Title = HttpUtility.HtmlDecode(syndicationItem.Title.Text);
                            feedItem.Date = syndicationItem.PublishDate.LocalDateTime;

                            if (feedItem.Date < MinDate)
                            {
                                continue;
                            }

                            TextSyndicationContent content = syndicationItem.Content as TextSyndicationContent;
                            if (content != null)
                            {
                                // <content type="html">
                                feedItem.Body = content.Text;
                            }
                            else if (syndicationItem.Content != null && !string.IsNullOrEmpty(syndicationItem.Content.ToString()))
                            {
                                feedItem.Body = syndicationItem.Content.ToString();
                            }
                            else
                            {
                                var full = syndicationItem.ElementExtensions.Where(w => w.OuterName == "encoded").FirstOrDefault();
                                if (full != null)
                                {
                                    feedItem.Body = full.GetObject<XmlElement>().InnerText;
                                }
                                else if (syndicationItem.Summary != null)
                                {
                                    feedItem.Body = syndicationItem.Summary.Text;
                                }
                                else
                                {
                                    // throw this out because there's no content
                                    continue;
                                }
                            }

                            feedItem.Body = feedItem.Body.Trim();

                            feedItem.Summary = HttpUtility.HtmlDecode(StripHtml(syndicationItem.Summary != null ? syndicationItem.Summary.Text : feedItem.Body)).Trim();

                            Uri itemUri = null;
                            itemUri = syndicationItem.BaseUri;

                            if (itemUri == null)
                            {
                                // Get links out of feedburner feeds.
                                var originalLink = syndicationItem.ElementExtensions.Where(w => w.OuterName.ToUpper(CultureInfo.InvariantCulture) == "ORIGLINK").FirstOrDefault();
                                if (originalLink != null)
                                {
                                    Uri.TryCreate(originalLink.GetObject<XmlElement>().InnerText, UriKind.RelativeOrAbsolute, out itemUri);
                                }
                            }

                            if (itemUri == null)
                            {
                                // Get links from <link> elements.
                                var link = (from self in syndicationItem.Links where self.RelationshipType.ToUpper(CultureInfo.InvariantCulture) == "ALTERNATE" select self).FirstOrDefault();
                                if (link == null)
                                {
                                    link = syndicationItem.Links.FirstOrDefault();
                                }

                                if (link != null)
                                {
                                    itemUri = link.Uri;
                                }
                            }

                            if (itemUri == null)
                            {
                                // As a last resort, use the feed URL.
                                itemUri = new Uri(Query);
                            }

                            feedItem.Uri = itemUri;

                            var author = syndicationItem.ElementExtensions.Where(w => w.OuterName == "creator").FirstOrDefault();
                            if (author != null)
                            {
                                feedItem.Author = HttpUtility.HtmlDecode(author.GetObject<XmlElement>().InnerText);
                            }

                            RaiseGotNewFeedItem(feedItem);
                        }
                    }
                }
            }
            catch
            {
            }
        }
    }
}
