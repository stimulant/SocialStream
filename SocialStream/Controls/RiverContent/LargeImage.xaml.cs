using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;
using Microsoft.Surface.Presentation.Controls;
using SocialStream.Helpers;
using SocialStream.Properties;

namespace SocialStream.Controls.RiverContent
{
    /// <summary>
    /// A river item which shows the large version of images.
    /// </summary>
    public partial class LargeImage : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LargeImage"/> class.
        /// </summary>
        public LargeImage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Determines the size of the image to load and displays it.
        /// </summary>
        internal void RenderContent()
        {
            ImageFeedItem image = DataContext as ImageFeedItem;
            _viewport.Reset();
            if (image == null)
            {
                _image.UriSource = null;
                return;
            }

            if (image.SourceType == SourceType.Flickr)
            {
                ImageFeedItem.GetFlickrImageSizes(
                    image,
                    Settings.Default.FlickrApiKey,
                    new GetFlickrImageSizesCallback((imageFeedItem) => SetImageSource()));
            }
            else
            {
                SetImageSource();
            }
        }

        /// <summary>
        /// When the image sizes are loaded, set the image source to the largest image available.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "This is really so complex?")]
        private void SetImageSource()
        {
            ImageFeedItem image = DataContext as ImageFeedItem;
            if (image == null)
            {
                return;
            }

            _viewport.ScatterViewItem = this.FindVisualParent<ScatterViewItem>();

            if (image.SourceType == SourceType.Twitter || image.Sizes == null)
            {
                _image.UriSource = image.ThumbnailUri;
                return;
            }

            Uri original = (from kvp in image.Sizes where kvp.Key.Width > 1024 || kvp.Key.Height > 1024 select kvp.Value).FirstOrDefault();
            Uri large = (from kvp in image.Sizes where kvp.Key.Width == 1024 || kvp.Key.Height == 1024 select kvp.Value).FirstOrDefault();
            Uri medium = (from kvp in image.Sizes where kvp.Key.Width == 500 || kvp.Key.Height == 500 select kvp.Value).FirstOrDefault();

            Uri chosen = null;

            if (large == null && original != null)
            {
                chosen = original;
            }
            else if (large != null)
            {
                chosen = large;
            }
            else if (medium != null)
            {
                chosen = medium;
            }

            _image.UriSource = chosen != null ? chosen : image.ThumbnailUri;
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
        /// Handles the AreAnyTouchesCapturedWithinChanged event of the Viewport control. Shows the manipulation border.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Event handler is added in XAML.")]
        private void Viewport_AreAnyTouchesCapturedWithinChanged(object sender, RoutedEventArgs e)
        {
            VisualStateManager.GoToState(this, _viewport.AreAnyTouchesCapturedWithin ? IsManipulatingState.Name : NotManipulatingState.Name, true);
        }
    }
}
