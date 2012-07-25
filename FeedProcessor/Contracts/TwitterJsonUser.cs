using System.Runtime.Serialization;

namespace FeedProcessor.Contracts
{
    /// <summary>
    /// A datacontract for deserializing users from twitter.
    /// </summary>
    [DataContract]
    public class TwitterJsonUser
    {
        /// <summary>
        /// Gets or sets the user's screen name.
        /// </summary>
        /// <value>The screen_name.</value>
        [DataMember]
        public string screen_name { get; set; }

        /// <summary>
        /// Gets or sets the url to the user's profile image.
        /// </summary>
        /// <value>The profile_image_url.</value>
        [DataMember]
        public string profile_image_url { get; set; }
    }
}
