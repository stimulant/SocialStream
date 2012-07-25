using System.Windows;

namespace SocialStream.Controls
{
    /// <summary>
    /// A RoutedEventArgs type which allows the specification of a custom source object.
    /// </summary>
    public class UserSourceRoutedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Backing store for UserSource.
        /// </summary>
        private DependencyObject _userSource;

        /// <summary>
        /// Gets the custom source object.
        /// </summary>
        /// <value>The custom source object.</value>
        public DependencyObject UserSource
        {
            get { return _userSource; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserSourceRoutedEventArgs"/> class.
        /// </summary>
        /// <param name="routedEvent">The routed event.</param>
        /// <param name="source">The custom source object.</param>
        public UserSourceRoutedEventArgs(RoutedEvent routedEvent, DependencyObject source)
            : base(routedEvent)
        {
            _userSource = source;
        }
    }
}
