using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using FeedProcessor.Contracts;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;
using FeedProcessor.Net;

namespace FeedProcessor.Feeds
{
    /// <summary>
    /// A feed which loads tweets from the twitter straming API.
    /// </summary>
    internal class TwitterStreamingFeed : Feed
    {
        /// <summary>
        /// A mapping of twitter user ids to user names.
        /// </summary>
        private static Dictionary<string, string> _twitterUserNameCache = new Dictionary<string, string>();

        /// <summary>
        /// The username to use for the twitter streaming API.
        /// </summary>
        private string _twitterUsername;

        /// <summary>
        /// The password to use for the twitter streaming API.
        /// </summary>
        private string _twitterPassword;

        /// <summary>
        /// A JSON deserializer.
        /// </summary>
        private DataContractJsonSerializer _json = new DataContractJsonSerializer(typeof(TwitterJsonStatus));

        /// <summary>
        /// The background task which is connected to the streaming API.
        /// </summary>
        private Task _task;

        /// <summary>
        /// Initializes a new instance of the <see cref="TwitterStreamingFeed"/> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        internal TwitterStreamingFeed(string username, string password)
            : base(TimeSpan.FromDays(1), DateTime.MinValue)
        {
            _twitterUsername = username;
            _twitterPassword = password;
            SourceType = SourceType.Twitter;
        }

        /// <summary>
        /// Builds the query that is passed to the feed service.
        /// </summary>
        /// <returns>The query URI.</returns>
        internal override Uri BuildQuery()
        {
            return new Uri(string.Format(CultureInfo.InvariantCulture, "http://stream.twitter.com/1/statuses/filter.json?{0}", Query));
        }

        /// <summary>
        /// Initiates a request to the feed service.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want all exceptions."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Debug.WriteLine(System.String)", Justification = "It's just a log message.")]
        protected override void Poll()
        {
            if (_task != null)
            {
                return;
            }

            _task = Task.Factory.StartNew(new Action(() =>
            {
                HttpWebResponse webResponse = null;
                StreamReader responseStream = null;
                HttpWebRequest webRequest = null;
                int wait = 250;

                try
                {
                    while (true)
                    {
                        try
                        {
                            // Connect
                            webRequest = (HttpWebRequest)WebRequest.Create(BuildQuery());
                            webRequest.Credentials = new NetworkCredential(_twitterUsername, _twitterPassword);
                            webRequest.Timeout = -1;
                            webResponse = (HttpWebResponse)webRequest.GetResponse();
                            responseStream = new StreamReader(webResponse.GetResponseStream(), Encoding.GetEncoding("utf-8"));

                            // Read the stream.
                            while (true)
                            {
                                wait = 250;
                                ProcessResponse(responseStream.ReadLine());
                                RetryTime(HttpStatusCode.OK);
                            }
                        }
                        catch (WebException ex)
                        {
                            Debug.WriteLine(ex.Message);
                            if (ex.Status == WebExceptionStatus.ProtocolError)
                            {
                                // -- From Twitter Docs --
                                // When a HTTP error (> 200) is returned, back off exponentially.
                                // Perhaps start with a 10 second wait, double on each subsequent failure,
                                // and finally cap the wait at 240 seconds.
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
                                // -- From Twitter Docs --
                                // When a network error (TCP/IP level) is encountered, back off linearly.
                                // Perhaps start at 250 milliseconds and cap at 16 seconds.
                                // Linear Backoff
                                if (wait < 16000)
                                {
                                    wait += 250;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex.Message);
                        }
                        finally
                        {
                            if (webRequest != null)
                            {
                                webRequest.Abort();
                            }

                            if (responseStream != null)
                            {
                                responseStream.Close();
                                responseStream = null;
                            }

                            if (webResponse != null)
                            {
                                webResponse.Close();
                                webResponse = null;
                            }

                            Debug.WriteLine("Waiting: " + wait);
                            RetryTime(HttpStatusCode.NotAcceptable);
                            Thread.Sleep(wait);
                        }
                    }
                }
                catch (Exception ex)
                {
                    RetryTime(HttpStatusCode.NotAcceptable);
                    Debug.WriteLine(ex.Message);
                    Debug.WriteLine("Waiting: " + wait);
                    Thread.Sleep(wait);
                }
            }));
        }

        /// <summary>
        /// Processes the response from the feed service.
        /// </summary>
        /// <param name="response">response from the feed service.</param>
        internal override void ProcessResponse(object responseObject)
        {
            string response = responseObject.ToString();
            byte[] byteArray = Encoding.UTF8.GetBytes(response);
            TwitterJsonStatus status = null;
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                status = _json.ReadObject(stream) as TwitterJsonStatus;
            }

            StatusFeedItem feedItem = new StatusFeedItem
            {
                Author = status.user.screen_name,
                AvatarUri = new Uri(status.user.profile_image_url),
                Date = DateTimeOffset.ParseExact(status.created_at, "ddd MMM dd HH:mm:ss zzz yyyy", CultureInfo.InvariantCulture).DateTime,
                ServiceId = status.id,
                Uri = new Uri(string.Format(CultureInfo.InvariantCulture, "http://twitter.com/#!/{0}/status/{1}", status.user.screen_name, status.id)),
                Status = status.text
            };

            RaiseGotNewFeedItem(feedItem);
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
            return DateTime.MinValue;
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        #region Utility Methods

        #region GetFlickrUserIdFromUserName

        /// <summary>
        /// Callback for GetTwitterUserIdFromUserName.
        /// </summary>
        /// <param name="userId">The userId returned by the twitter API.</param>
        internal delegate void GetTwitterUserIdFromUserNameCallback(string userId);

        /// <summary>
        /// Gets the twitter user id from a username.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="callback">The callback.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want all exceptions.")]
        internal static void GetTwitterUserIdFromUserName(string username, GetTwitterUserIdFromUserNameCallback callback)
        {
            if (_twitterUserNameCache.ContainsValue(username))
            {
                callback((from kvp in _twitterUserNameCache where kvp.Value == username select kvp.Key).FirstOrDefault());
                return;
            }

            string query = string.Format(CultureInfo.InvariantCulture, "http://api.twitter.com/1/users/show.xml?screen_name={0}", username);
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
                    string userid = XDocument.Parse(e.Response).Element("user").Element("id").Value;
                    _twitterUserNameCache[userid] = username;
                    callback(userid);
                }
                catch
                {
                    callback(null);
                }
            };
        }

        #endregion

        #endregion
    }
}
