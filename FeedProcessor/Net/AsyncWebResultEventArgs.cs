using System;
using System.Net;

namespace FeedProcessor.Net
{
    /// <summary>
    /// The EventArgs object passed in the AsyncWebRequest.Result event.
    /// </summary>
    internal class AsyncWebResultEventArgs : EventArgs
    {
        /// <summary>
        /// The response from the web service.
        /// </summary>
        internal readonly string Response;

        /// <summary>
        /// The status code from the web service.
        /// </summary>
        internal readonly HttpStatusCode Status;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncWebResultEventArgs"/> class.
        /// </summary>
        /// <param name="response">The response from the web service.</param>
        /// <param name="status">The status code from the web service.</param>
        internal AsyncWebResultEventArgs(string response, HttpStatusCode status)
        {
            Response = response;
            Status = status;
        }
    }
}
