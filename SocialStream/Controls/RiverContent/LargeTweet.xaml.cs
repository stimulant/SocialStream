using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using SocialStream.Properties;

namespace SocialStream.Controls.RiverContent
{
    /// <summary>
    /// A river item which shows the large version of tweets.
    /// </summary>
    public partial class LargeTweet : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LargeTweet"/> class.
        /// </summary>
        public LargeTweet()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }
        }

        /// <summary>
        /// Handles the Click event of the Close button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.RoutedEventArgs"/> instance containing the event data.</param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new UserSourceRoutedEventArgs(RiverItemBase.CloseRequestedEvent, _closeBtn));
        }

        /// <summary>
        /// Handles the Click event of the Flip button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.RoutedEventArgs"/> instance containing the event data.</param>
        private void Flip_Click(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new UserSourceRoutedEventArgs(RiverItemBase.FlipRequestedEvent, _flipBtn));
        }

        /// <summary>
        /// If resizing is enabled, size the content to a fixed pixel size according to the size of the container, and put it in the ViewBox.
        /// </summary>
        internal void SetUpResizing()
        {
            if (!Settings.Default.EnableContentResizing)
            {
                return;
            }

            _contentRoot.Width = _contentRoot.ActualWidth;
            _contentRoot.Height = _contentRoot.ActualHeight;
            _fixedContentContainer.Child = null;
            _scalingContentContainer.Child = _contentRoot;
        }

        /// <summary>
        /// If resizing is enabled, let the content size dynamically and put it in the normal container.
        /// </summary>
        internal void TearDownResizing()
        {
            if (!Settings.Default.EnableContentResizing)
            {
                return;
            }

            _scalingContentContainer.Child = null;
            _contentRoot.Width = double.NaN;
            _contentRoot.Height = double.NaN;
            _fixedContentContainer.Child = _contentRoot;
        }
    }
}
