using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FeedProcessor.FeedItems;

namespace SocialStream.Controls.RiverContent
{
    /// <summary>
    /// A river item which shows the small version of image feed items.
    /// </summary>
    public partial class SmallImage : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmallImage"/> class.
        /// </summary>
        public SmallImage()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            SizeChanged += (sender, e) => _posted.Visibility = _author.Visibility = ActualWidth > 100 ? Visibility.Visible : Visibility.Collapsed;
            Unloaded += (sender, e) => Image.ImageLoaded -= Image_ImageLoaded;
        }

        /// <summary>
        /// Handles the ImageLoaded event of the Image control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Image_ImageLoaded(object sender, EventArgs e)
        {
            Image.ImageLoaded -= Image_ImageLoaded;
            if (Image.BitmapImage != null)
            {
                (DataContext as ImageFeedItem).ThumbnailSize = new Size(Image.BitmapImage.PixelWidth, Image.BitmapImage.PixelHeight);
            }
        }
    }
}
