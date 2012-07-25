using SocialStream.Helpers;

namespace SocialStream.ConfigTool.VO
{
    /// <summary>
    /// Enables change events for a single string field
    /// </summary>
    public class BindableStringVO : BindableBase
    {
        /// <summary>
        /// Backing store for StringValue.
        /// </summary>
        private string _StringValue = string.Empty;

        /// <summary>
        /// Gets or sets the StringValue.
        /// </summary>
        /// <value>The query.</value>
        public string StringValue
        {
            get
            {
                return _StringValue;
            }

            set
            {
                _StringValue = value;
                NotifyPropertyChanged("StringValue");
            }
        }
    }
}
