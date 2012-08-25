using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using Facebook;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;

namespace FeedProcessor.Feeds
{
    /// <summary>
    /// A feed which loads posts from specific facebook pages
    /// </summary>
    internal class FacebookPageFeed : Feed
    {
        /// <summary>
        /// A mapping of facebook page ids to their names.
        /// </summary>
        private static Dictionary<string, string> _facebookPageNameCache = new Dictionary<string, string>();

        /// <summary>
        /// Facebook Client object.
        /// </summary>
        /// <value>The Facebook Client.</value>
        private FacebookClient _fb;

        /// <summary>
        /// Determine whether to display others' posts on a page.
        /// </summary>
        private bool _displayFbContentFromOthers;

        /// <summary>
        /// Index for owner's photo data in a Facebook query result.
        /// </summary>
        private int _ownerPhotoDataIndex;

        /// <summary>
        /// Index for owner's photo author data in a Facebook query result.
        /// </summary>
        private int _ownerPhotoAuthorDataIndex;

        /// <summary>
        /// Index for others' photo data in a Facebook query result.
        /// </summary>
        private int _othersPhotoDataIndex;

        /// <summary>
        /// Index for others' photo author data in a Facebook query result.
        /// </summary>
        private int _othersPhotoAuthorDataIndex;

        /// <summary>
        /// Index for owner's status stream in a Facebook query result.
        /// </summary>
        private int _ownerStatusStreamIndex;

        /// <summary>
        /// Index for owner's status author data in a Facebook query result.
        /// </summary>
        private int _ownerStatusAuthorDataIndex;

        /// <summary>
        /// Index for others' status stream in a Facebook query result.
        /// </summary>
        private int _othersStatusStreamIndex;

        /// <summary>
        /// Index for others' status author data in a Facebook query result.
        /// </summary>
        private int _othersStatusAuthorDataIndex;

        /// <summary>
        /// Maximum number of entries to query.
        /// </summary>
        private int _queryLimit = 5000;

        /// <summary>
        /// Gets or sets the Facebook client id.
        /// </summary>
        /// <value>The Facebook client id.</value>
        protected string FacebookClientId { get; set; }

        /// <summary>
        /// Gets or sets the Facebook client secret.
        /// </summary>
        /// <value>The Facebook client secret.</value>
        protected string FacebookClientSecret { get; set; }

        /// <summary>
        /// The minimum polling interval for the Facebook service. (?)
        /// </summary>
        protected static readonly TimeSpan MinPollingInterval = TimeSpan.FromMinutes(1);

        /// <summary>
        /// Builds the query that is passed to the feed service.
        /// </summary>
        /// <returns>The query URI.</returns>
        internal override Uri BuildQuery()
        {
            return new Uri(Query);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacebookPageFeed"/> class.
        /// </summary>
        /// <param name="flickrApiKey">The flickr API key.</param>
        /// <param name="pollInterval">The requested polling interval.</param>
        /// <param name="minDate">The minimum allowed date of a returned item.</param>
        internal FacebookPageFeed(bool displayFbContentFromOthers, string facebookClientId, string facebookClientSecret, TimeSpan pollInterval, DateTime minDate)
            : base(TimeSpan.FromMilliseconds(Math.Max(pollInterval.TotalMilliseconds, MinPollingInterval.TotalMilliseconds)), minDate)
        {
            FacebookClientId = facebookClientId;
            FacebookClientSecret = facebookClientSecret;
            _displayFbContentFromOthers = displayFbContentFromOthers;

            // create a new facebook client and get an access token
            _fb = new FacebookClient();

            SourceType = SourceType.Facebook;
        }

        /// <summary>
        /// Initiates a request to the feed service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want all exceptions."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Debug.WriteLine(System.String)", Justification = "It's just a log message.")]
        protected override void Poll()
        {
            int wait = 250;

            // get access token
            try
            {
                    try
                    {
                        dynamic accessToken = _fb.Get("oauth/access_token", new
                        {
                            client_id = FacebookClientId,
                            client_secret = FacebookClientSecret,
                            grant_type = "client_credentials"
                        });
                        _fb.AccessToken = accessToken[0];

                        dynamic result = null;

                        // get page stream data 
                        // --- from Facebook Query Language (FQL) documentation ---
                        // https://developers.facebook.com/docs/reference/fql/stream/
                        // filter these profile view posts by specifying filter_key 'others' (return only posts that are 
                        // by someone other than the specified user) or 'owner' (return only posts made by the specified user)
                        // further filter by 'type' to get Status update (46), Post on wall from another user (56) or Photos posted (247)
                        // Each query of the stream table is limited to the previous 30 days or 50 posts, whichever is greater, however you can use 
                        // time-specific fields such as created_time to retrieve a much greater range of posts.

                        // check whether to display content only from owner or also from other users
                        if (_displayFbContentFromOthers)
                        {
                            result = PollWithOthers();
                        }

                        else
                        {
                            result = PollWithOwner();
                        }

                        if (result != null)
                            ProcessResponse(result);

                        RetryTime(HttpStatusCode.OK);
                    }
                    catch (WebException ex)
                    {
                        Debug.WriteLine(ex.Message);
                        if (ex.Status == WebExceptionStatus.ProtocolError)
                        {
                            // Exponential Backoff
                            if (wait < 10000)
                            {
                                wait = 10000;
                            }
                            else
                            {
                                if (wait < 240000)
                                {
                                    wait = wait * 2;
                                }
                            }
                        }
                        else
                        {
                            // Linear Backoff
                            if (wait < 16000)
                            {
                                wait += 250;
                            }
                        }
                        Debug.WriteLine("Waiting: " + wait);
                        Thread.Sleep(wait);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                        Thread.Sleep(wait);
                    }
                }
            
            catch (Exception ex)
            {
                RetryTime(HttpStatusCode.NotAcceptable);
                Debug.WriteLine(ex.Message);
                Debug.WriteLine("Waiting: " + wait);
                Thread.Sleep(wait);
            }
        }

        /// <summary>
        /// Poll content from owner as well as other users' posts on the facebook page.
        /// </summary>
        /// <returns>
        /// The result of the query.
        /// </returns>
        internal dynamic PollWithOthers()
        {
            long currentTime = (long)ConvertToUnixTimestamp(DateTime.Now);

            try
            {
                // Make asynchronous request to Facebook so that threads don't get blocked
                var fbGetTask = _fb.GetTaskAsync("fql", new
                {
                    q = new
                    {
                        id = string.Format(CultureInfo.InvariantCulture, "SELECT page_id FROM page WHERE username = '{0}'", Query),
                        othersphotostream = string.Format(CultureInfo.InvariantCulture, "SELECT attachment, post_id, created_time, type, message, permalink, actor_id FROM stream WHERE source_id IN (SELECT page_id FROM #id) AND filter_key = 'others' AND type = 247 AND created_time < {0} LIMIT 5000", currentTime),
                        othersstatusstream = string.Format(CultureInfo.InvariantCulture, "SELECT attachment, post_id, created_time, type, message, permalink, actor_id FROM stream WHERE source_id IN (SELECT page_id FROM #id) AND filter_key = 'others' AND type = 56 AND created_time < {0} LIMIT 5000", currentTime),
                        ownerphotostream = string.Format(CultureInfo.InvariantCulture, "SELECT attachment, post_id, created_time, type, message, permalink, actor_id FROM stream WHERE source_id IN (SELECT page_id FROM #id) AND filter_key = 'owner' AND type = 247 AND created_time < {0} LIMIT 5000", currentTime),
                        ownerstatusstream = string.Format(CultureInfo.InvariantCulture, "SELECT attachment, post_id, created_time, type, message, permalink, actor_id FROM stream WHERE source_id IN (SELECT page_id FROM #id) AND filter_key = 'owner' AND type = 46 AND created_time < {0} LIMIT 5000", currentTime),
                        othersphotoauthordata = "SELECT uid, name, pic_small FROM user WHERE uid IN (SELECT actor_id FROM #othersphotostream)",
                        othersphotodata = "SELECT pid, owner, src_big, created, link, caption FROM photo WHERE pid IN (SELECT  attachment.media.photo.pid FROM #othersphotostream)",
                        othersstatusauthordata = "SELECT uid, name, pic_small FROM user WHERE uid IN (SELECT actor_id FROM #othersstatusstream)",
                        ownerphotoauthordata = "SELECT page_id, name, pic_small FROM page WHERE page_id IN (SELECT actor_id FROM #ownerphotostream)",
                        ownerphotodata = "SELECT pid, owner, src_big, created, link, caption FROM photo WHERE pid IN (SELECT  attachment.media.photo.pid FROM #ownerphotostream)",
                        ownerstatusauthordata = "SELECT page_id, name, pic_small FROM page WHERE page_id IN (SELECT actor_id FROM #ownerstatusstream)",
                    }
                });

                // assign indices based on returned query order
                _othersStatusStreamIndex = 2;
                _ownerStatusStreamIndex = 4;
                _othersPhotoAuthorDataIndex = 5;
                _othersPhotoDataIndex = 6;
                _othersStatusAuthorDataIndex = 7;
                _ownerPhotoAuthorDataIndex = 8;
                _ownerPhotoDataIndex = 9;
                _ownerStatusAuthorDataIndex = 10;

                return fbGetTask.Result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                // if errors out due to error 500 then wait a little bit and try again
                System.Threading.Thread.Sleep(300);

                return null;
            }
        }

        /// <summary>
        /// Poll content only from owner of the facebook page.
        /// </summary>
        /// <returns>
        /// The result of the query.
        /// </returns>
        internal dynamic PollWithOwner()
        {
            long currentTime = (long)ConvertToUnixTimestamp(DateTime.Now);

            try
            {
                var fbGetTask = _fb.GetTaskAsync("fql", new
                {
                    q = new
                    {
                        id = string.Format(CultureInfo.InvariantCulture, "SELECT page_id FROM page WHERE username = '{0}'", Query),
                        ownerphotostream = string.Format(CultureInfo.InvariantCulture, "SELECT attachment, post_id, created_time, type, message, permalink, actor_id FROM stream WHERE source_id IN (SELECT page_id FROM #id) AND filter_key = 'owner' AND type = 247 AND created_time < {0} LIMIT {1}", currentTime, _queryLimit),
                        ownerstatusstream = string.Format(CultureInfo.InvariantCulture, "SELECT attachment, post_id, created_time, type, message, permalink, actor_id FROM stream WHERE source_id IN (SELECT page_id FROM #id) AND filter_key = 'owner' AND type = 46 AND created_time < {0} LIMIT {1}", currentTime, _queryLimit),
                        ownerphotoauthordata = "SELECT page_id, name, pic_small FROM page WHERE page_id IN (SELECT actor_id FROM #ownerphotostream)",
                        ownerphotodata = "SELECT pid, owner, src_big, created, link, caption FROM photo WHERE pid IN (SELECT  attachment.media.photo.pid FROM #ownerphotostream)",
                        ownerstatusauthordata = "SELECT page_id, name, pic_small FROM page WHERE page_id IN (SELECT actor_id FROM #ownerstatusstream)",
                    }
                });

                // assign indices based on returned query order
                _ownerStatusStreamIndex = 2;
                _ownerPhotoAuthorDataIndex = 3;
                _ownerPhotoDataIndex = 4;
                _ownerStatusAuthorDataIndex = 5;

                return fbGetTask.Result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                // if errors out due to error 500 then wait a little bit and try again
                System.Threading.Thread.Sleep(300);

                return null;
            }
        }

        /// <summary>
        /// Processes the response from the feed service.
        /// </summary>
        /// <param name="response">response from the feed service.</param>
        internal override void ProcessResponse(object response)
        {
            // process others' items and convert them to status or image feeds
            if (_displayFbContentFromOthers)
            {
                ProcessResponseWithOthers(response);
            }

            // process owner's items and convert them to status or image feeds
            ProcessResponseWithOwner(response);
        }

        #region Process Others' Items
        /// <summary>
        /// Processes the response to convert other users' posts into feed items.
        /// </summary>
        /// <param name="response">response from the feed service.</param>
        internal void ProcessResponseWithOthers(dynamic response)
        {
            #region Process Others' Image Items

            if (response.data[_othersPhotoDataIndex]["fql_result_set"].Count > 0)
            {
                foreach (dynamic photo in response.data[_othersPhotoDataIndex]["fql_result_set"])   // othersphotodata
                {
                    foreach (dynamic author in response.data[_othersPhotoAuthorDataIndex]["fql_result_set"]) // othersphotoauthordata
                    {
                        DateTime created = ConvertFromUnixTimestamp(double.Parse(photo["created"].ToString()));

                        if (created < MinDate)
                        {
                            continue;
                        }

                        // add a feed if matching author is found for the post
                        if (photo.owner == author.page_id)
                        {
                            ImageFeedItem feedItem = new ImageFeedItem
                            {
                                Author = (string)author["name"],
                                AvatarUri = new Uri((string)author["pic_small"]),
                                Date = created,
                                ServiceId = (string)photo["pid"],
                                Uri = new Uri(string.Format(CultureInfo.InvariantCulture, (string)photo["link"])),
                                SourceType = SourceType.Facebook,
                                Caption = (string)photo["caption"],
                                ThumbnailUri = new Uri(string.Format(CultureInfo.InvariantCulture, (string)photo["src_big"])),
                            };

                            RaiseGotNewFeedItem(feedItem);
                        }

                        break;
                    }
                }
            }

            #endregion

            #region Process Others' Status Items

            if (response.data[_othersStatusStreamIndex]["fql_result_set"].Count > 0)
            {
                foreach (dynamic post in response.data[_othersStatusStreamIndex]["fql_result_set"])    // othersstatusstream
                {
                    foreach (dynamic author in response.data[_othersStatusAuthorDataIndex]["fql_result_set"])  // othersstatusauthordata
                    {
                        if (post.actor_id == author.uid)
                        {
                            // create status feed item if matching author data is found for this post
                            DateTime created = ConvertFromUnixTimestamp(double.Parse(post["created_time"].ToString()));

                            if (created < MinDate)
                            {
                                continue;
                            }

                            StatusFeedItem feedItem = new StatusFeedItem
                            {
                                Author = (string)author["name"],
                                AvatarUri = new Uri((string)author["pic_small"]),
                                Date = created,
                                ServiceId = (string)post["post_id"],
                                Uri = new Uri(string.Format(CultureInfo.InvariantCulture, (string)post["permalink"])),
                                Status = (string)post["message"],
                                SourceType = SourceType.Facebook,
                            };

                            RaiseGotNewFeedItem(feedItem);

                            break;
                        }
                    }
                }
            }

            #endregion
        }
        #endregion

        #region Process Owner's Items
        /// <summary>
        /// Processes the response to convert owner's posts into feed items.
        /// </summary>
        /// <param name="response">response from the feed service.</param>
        internal void ProcessResponseWithOwner(dynamic response)
        {
            #region Process Owner's Image Items

            if (response.data[_ownerPhotoDataIndex]["fql_result_set"].Count > 0)   // ownerphotodata
            {
                foreach (dynamic photo in response.data[_ownerPhotoDataIndex]["fql_result_set"])   // ownerphotodata
                {
                    foreach (dynamic author in response.data[_ownerPhotoAuthorDataIndex]["fql_result_set"]) // ownerphotoauthordata
                    {
                        DateTime created = ConvertFromUnixTimestamp(double.Parse(photo["created"].ToString()));

                        if (created < MinDate)
                        {
                            continue;
                        }

                        // add a feed if matching author is found for the post
                        if (photo.owner == author.page_id)
                        {
                            ImageFeedItem feedItem = new ImageFeedItem
                            {
                                Author = (string)author["name"],
                                AvatarUri = new Uri((string)author["pic_small"]),
                                Date = created,
                                ServiceId = (string)photo["pid"],
                                Uri = new Uri(string.Format(CultureInfo.InvariantCulture, (string)photo["link"])),
                                SourceType = SourceType.Facebook,
                                Caption = (string)photo["caption"],
                                ThumbnailUri = new Uri(string.Format(CultureInfo.InvariantCulture, (string)photo["src_big"])),
                            };

                            RaiseGotNewFeedItem(feedItem);
                        }

                        break;
                    }
                }
            }

            #endregion

            #region Process Owner's Status Items

            if (response.data[_ownerStatusStreamIndex]["fql_result_set"].Count > 0)
            {
                foreach (dynamic post in response.data[_ownerStatusStreamIndex]["fql_result_set"])    // ownerstatusstream
                {
                    foreach (dynamic author in response.data[_ownerStatusAuthorDataIndex]["fql_result_set"]) // ownerstatusauthordata
                    {
                        if (post.actor_id == author.page_id)
                        {
                            // create status feed item if matching author data is found for this post
                            DateTime created = ConvertFromUnixTimestamp(double.Parse(post["created_time"].ToString()));

                            if (created < MinDate)
                            {
                                continue;
                            }

                            StatusFeedItem feedItem = new StatusFeedItem
                            {
                                Author = (string)author["name"],
                                AvatarUri = new Uri((string)author["pic_small"]),
                                Date = created,
                                ServiceId = (string)post["post_id"],
                                Uri = new Uri(string.Format(CultureInfo.InvariantCulture, (string)post["permalink"])),
                                Status = (string)post["message"],
                                SourceType = SourceType.Facebook,
                            };

                            RaiseGotNewFeedItem(feedItem);

                            break;
                        }
                    }
                }
            }

            #endregion
        }
        #endregion

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
            return DateTime.MinValue;
        }

        #region Utility Methods

        /// <summary>
        /// Converts a UNIX timestamp to a DateTime.
        /// </summary>
        /// <param name="timestamp">The timestamp.</param>
        /// <returns>The timestamp converted to a DateTime.</returns>
        internal static DateTime ConvertFromUnixTimestamp(double timestamp)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0).AddSeconds(timestamp);
        }

        /// <summary>
        /// Converts a DateTime to a UNIX timestamp.
        /// </summary>
        /// <param name="timestamp">The DateTime.</param>
        /// <returns>The DateTime converted to a UNIX timestamp.</returns>
        internal static double ConvertToUnixTimestamp(DateTime dateTime)
        {
            TimeSpan span = dateTime - new DateTime(1970, 1, 1).ToLocalTime();
            return span.TotalSeconds;
        }

        #endregion

    }
}
