using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Timers;
using FeedProcessor.Enums;
using FeedProcessor.Net;

namespace FeedProcessor
{
    /// <summary>
    /// A feed is responsible for retrieving and parsing items from one particular source.
    /// </summary>
    internal abstract class Feed : IDisposable
    {
        /// <summary>
        /// The time after which it's ok to make another query.
        /// </summary>
        private DateTime _retryDateTime = DateTime.MinValue;

        /// <summary>
        /// A timer which ticks when it's time to make another query.
        /// </summary>
        private Timer _timer;

        /// <summary>
        /// A regular expression used for removing HTML tags from a string.
        /// http://stackoverflow.com/questions/787932/using-c-regular-expressions-to-remove-html-tags/788140#788140
        /// </summary>
        private static Regex _stripHtml = new Regex(@"(?></?\w+)(?>(?:[^>'""]+|'[^']*'|""[^""]*"")*)>");

        /// <summary>
        /// Initializes a new instance of the <see cref="Feed"/> class.
        /// </summary>
        /// <param name="pollInterval">The requested polling interval.</param>
        /// <param name="minDate">The minimum allowed date of a returned item.</param>
        internal Feed(TimeSpan pollInterval, DateTime minDate)
        {
            MinDate = minDate;
            _timer = new Timer();
            _timer.Interval = pollInterval.TotalMilliseconds;
            _timer.Elapsed += (sender, e) => Poll();
            _timer.AutoReset = true;
        }

        /// <summary>
        /// Builds the query that is passed to the feed service.
        /// </summary>
        /// <returns>The query URI.</returns>
        internal abstract Uri BuildQuery();

        /// <summary>
        /// Returns a time after which it's ok to make another query.
        /// </summary>
        /// <param name="httpStatusCode">The HTTP status code returned from the last attempt.</param>
        /// <returns>The time after which it's ok to make another query.</returns>
        internal abstract DateTime RetryTime(HttpStatusCode httpStatusCode);

        /// <summary>
        /// Processes the response from the feed service.
        /// </summary>
        /// <param name="response">response from the feed service.</param>
        internal abstract void ProcessResponse(string response);

        /// <summary>
        /// Initiates a request to the feed service.
        /// </summary>
        protected virtual void Poll()
        {
            if (_retryDateTime > DateTime.Now)
            {
                return;
            }

            _timer.Stop();

            AsyncWebRequest request = new AsyncWebRequest();
            request.Result += Request_Result;
            request.Request(BuildQuery());
        }

        /// <summary>
        /// Handles the result event from AsyncWebRequest to process the result.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="FeedProcessor.Net.AsyncWebResultEventArgs"/> instance containing the event data.</param>
        private void Request_Result(AsyncWebRequest sender, AsyncWebResultEventArgs e)
        {
            if (e.Status == HttpStatusCode.OK)
            {
                ProcessResponse(e.Response);
            }

            _retryDateTime = RetryTime(e.Status);

            _timer.Start();
        }

        #region SourceType

        /// <summary>
        /// Backing store for SourceType.
        /// </summary>
        private SourceType _sourceType = SourceType.News;

        /// <summary>
        /// Gets or sets the type of this feed's source.
        /// </summary>
        /// <value>The type of this feed's source.</value>
        internal SourceType SourceType
        {
            get
            {
                return _sourceType;
            }

            set
            {
                _sourceType = value;
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the query.
        /// </summary>
        /// <value>The query.</value>
        internal string Query { get; set; }

        /// <summary>
        /// Gets the minimum feed date.
        /// </summary>
        /// <value>The min date.</value>
        protected DateTime MinDate { get; private set; }

        /// <summary>
        /// Starts polling this instance.
        /// </summary>
        internal void Start()
        {
            _timer.Stop();
            Poll();
            _timer.Start();
        }

        /// <summary>
        /// Stops polling this instance.
        /// </summary>
        internal void Stop()
        {
            _timer.Stop();
        }

        #region GotNewFeedItem

        /// <summary>
        /// Represents the method that will handle the GotNewFeedItem event.
        /// </summary>
        /// <param name="sender">The Feed object.</param>
        /// <param name="e">The GotNewFeedItemEventArgs object.</param>
        internal delegate void GotNewFeedItemEventHandler(Feed sender, GotNewFeedItemEventArgs e);

        /// <summary>
        /// Occurs when a new feed item arrives.
        /// </summary>
        internal event GotNewFeedItemEventHandler GotNewFeedItem;

        /// <summary>
        /// Raises the GotNewFeedItem event.
        /// </summary>
        /// <param name="feedItem">The feed item.</param>
        protected void RaiseGotNewFeedItem(FeedItem feedItem)
        {
            if (GotNewFeedItem != null)
            {
                GotNewFeedItem(this, new GotNewFeedItemEventArgs(feedItem));
            }
        }

        #endregion

        #region SourceStatusUpdated

        /// <summary>
        /// Represents the method that will handle the SourceStatusUpdated event.
        /// </summary>
        /// <param name="sender">The Feed object.</param>
        /// <param name="e">The SourceStatusUpdatedEventArgs object.</param>
        internal delegate void SourceStatusUpdatedEventHandler(Feed sender, SourceStatusUpdatedEventArgs e);

        /// <summary>
        /// Occurs when a source goes down or comes back up.
        /// </summary>
        internal event SourceStatusUpdatedEventHandler SourceStatusUpdated;

        /// <summary>
        /// Raises the SourceStatusUpdated event.
        /// </summary>
        /// <param name="isSourceUp">if set to <c>true</c> [is source up].</param>
        protected void RaiseSourceStatusUpdated(bool isSourceUp)
        {
            if (SourceStatusUpdated != null)
            {
                SourceStatusUpdated(this, new SourceStatusUpdatedEventArgs(isSourceUp));
            }
        }

        #endregion

        /// <summary>
        /// Strips HTML tags from a string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The stripped string.</returns>
        protected static string StripHtml(string input)
        {
            return _stripHtml.Replace(input, string.Empty);
        }

        #region IDisposable

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _timer.Dispose();
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
