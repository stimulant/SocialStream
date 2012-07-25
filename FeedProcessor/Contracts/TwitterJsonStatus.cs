using System.Runtime.Serialization;

namespace FeedProcessor.Contracts
{
    /// <summary>
    /// A datacontract for deserializing a status message from twitter.
    /// </summary>
    [DataContract]
    public class TwitterJsonStatus
    {
        /// <summary>
        /// Gets or sets the date the tweet was posted.
        /// </summary>
        /// <value>The created_at.</value>
        [DataMember]
        public string created_at { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        /// <value>The user.</value>
        [DataMember]
        public TwitterJsonUser user { get; set; }

        /// <summary>
        /// Gets or sets the text of the tweet.
        /// </summary>
        /// <value>The text.</value>
        [DataMember]
        public string text { get; set; }

        /// <summary>
        /// Gets or sets the id of the twwet.
        /// </summary>
        /// <value>The id.</value>
        [DataMember]
        public string id { get; set; }
    }
}
