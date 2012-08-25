using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;

namespace FeedProcessor.Net
{
    /// <summary>
    /// Encapsulates the behavior of asynronously loading data from a web service and returning that data along with an HTTP status code.
    /// </summary>
    internal class AsyncWebRequest
    {
        /// <summary>
        /// Represents the method that is called when the Result event fires.
        /// </summary>
        /// <param name="sender">The AsyncWebRequest object.</param>
        /// <param name="e">The AsyncWebResultEventArgs object.</param>
        internal delegate void AsyncWebRequestResultHandler(AsyncWebRequest sender, AsyncWebResultEventArgs e);

        /// <summary>
        /// Occurs when a result arrives from the request.
        /// </summary>
        internal event AsyncWebRequestResultHandler Result;

        /// <summary>
        /// Requests the specified URI.
        /// </summary>
        /// <param name="uri">The URI to request.</param>
        internal void Request(Uri uri)
        {
            Debug.WriteLine("Requesting " + uri);
            using (WebClient client = new WebClient())
            {
                client.Headers.Add("user-agent", "Social Stream for Microsoft® Surface® Feed Processor");
                client.OpenReadCompleted += Client_OpenReadCompleted;
                client.OpenReadAsync(uri);
            }
        }

        /// <summary>
        /// Handles the OpenReadCompleted event of the WebClient.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Net.OpenReadCompletedEventArgs"/> instance containing the event data.</param>
        private void Client_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (Result == null)
            {
                return;
            }

            try
            {
                HttpStatusCode code = HttpStatusCode.OK;
                string response = null;

                if (e.Error != null)
                {
                    WebException ex = e.Error as WebException;
                    HttpWebResponse webResponse = ex.Response == null ? null : ex.Response as HttpWebResponse;
                    code = webResponse == null ? HttpStatusCode.ServiceUnavailable : webResponse.StatusCode;
                }
                else
                {
                    response = new StreamReader(e.Result, Encoding.UTF8, true).ReadToEnd();
                }

                Result(this, new AsyncWebResultEventArgs(response, code));
            }
            catch
            {
#if DEBUG
                throw;
#endif
                Result(this, new AsyncWebResultEventArgs(null, HttpStatusCode.ServiceUnavailable));
            }
        }
    }
}
