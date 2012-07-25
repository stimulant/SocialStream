using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using SocialStream.Helpers;

namespace SocialStream.Controls
{
    /// <summary>
    /// A wrapper around the Image control which displays a loader and fades the image in when loaded.
    /// </summary>
    public class ImageLoader : Control, IDisposable
    {
        /// <summary>
        /// The image control which displays the image.
        /// </summary>
        private Image _image;

        /// <summary>
        /// A background thread for loading and decoding images.
        /// </summary>
        private BackgroundWorker _worker;

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            if (DesignerProperties.GetIsInDesignMode(this) == false)
            {
                _image = GetTemplateChild("PART_Image") as Image;
                Loaded += ImageLoader_Loaded;
            }

            base.OnApplyTemplate();
        }

        /// <summary>
        /// Handles the Loaded event of the ImageLoader control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ImageLoader_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateUriSource(null);
            Loaded -= ImageLoader_Loaded;
        }

        #region UriSource

        /// <summary>
        /// Gets or sets the image UriSource.
        /// </summary>
        /// <value>The image UriSource.</value>
        public Uri UriSource
        {
            get { return (Uri)GetValue(UriSourceProperty); }
            set { SetValue(UriSourceProperty, value); }
        }

        /// <summary>
        /// Backing store for UriSource.
        /// </summary>
        public static readonly DependencyProperty UriSourceProperty = DependencyProperty.Register(
            "UriSource",
            typeof(Uri),
            typeof(ImageLoader),
            new PropertyMetadata(null, (sender, e) => (sender as ImageLoader).UpdateUriSource(e.OldValue as Uri)));

        /// <summary>
        /// Updates the UriSource.
        /// </summary>
        /// <param name="oldValue">The old value.</param>
        private void UpdateUriSource(Uri oldValue)
        {
            if (_image == null || !IsLoaded)
            {
                return;
            }

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                _image.Opacity = 1;
                return;
            }

            if (oldValue != null && UriSource != null && oldValue.OriginalString == UriSource.OriginalString)
            {
                return;
            }

            IsImageLoading = true;
            _image.Source = null;

            if (UriSource == null)
            {
                return;
            }

            if (_worker != null)
            {
                _worker.DoWork -= Worker_DoWork;
                _worker.RunWorkerCompleted -= Worker_RunWorkerCompleted;
            }

            _worker = new BackgroundWorker();
            _worker.DoWork += Worker_DoWork;
            _worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

            ImageLoaderOptions options = new ImageLoaderOptions { Uri = UriSource, DecodePixelWidth = -1 };
            if (RenderAtSize)
            {
                UpdateLayout();
                options.DecodePixelWidth = (int)Math.Max(ActualWidth, ActualHeight);
            }

            _worker.RunWorkerAsync(options);
        }

        /// <summary>
        /// Load and decode the image on a background thread.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.DoWorkEventArgs"/> instance containing the event data.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want to catch all exceptions")]
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                using (WebClient client = new WebClient() { CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable) })
                {
                    ImageLoaderOptions options = e.Argument as ImageLoaderOptions;
                    BitmapImage bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = new MemoryStream(client.DownloadData(options.Uri));
                    bmp.CacheOption = BitmapCacheOption.OnLoad;

                    if (options.DecodePixelWidth != -1)
                    {
                        bmp.DecodePixelWidth = options.DecodePixelWidth;
                    }

                    bmp.EndInit();
                    bmp.Freeze();
                    e.Result = bmp;
                }
            }
            catch
            {
                e.Result = null;
            }
        }

        /// <summary>
        /// Display the image.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.RunWorkerCompletedEventArgs"/> instance containing the event data.</param>
        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _image.Source = e.Result as BitmapImage;
            IsImageLoading = false;
            if (ImageLoaded != null)
            {
                ImageLoaded(this, EventArgs.Empty);
            }
        }

        #endregion

        #region Label

        /// <summary>
        /// Gets or sets the label.
        /// </summary>
        /// <value>The label.</value>
        public string Label
        {
            get { return (string)GetValue(LabelProperty); }
            set { SetValue(LabelProperty, value); }
        }

        /// <summary>
        /// The identifier for the Label dependency property.
        /// </summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.Register("Label", typeof(string), typeof(ImageLoader), new PropertyMetadata(string.Empty));

        #endregion

        #region IsImageLoading

        /// <summary>
        /// Gets or sets a value indicating whether the image is loading.
        /// </summary>
        /// <value>
        /// <c>true</c> if the image is loading; otherwise, <c>false</c>.
        /// </value>
        public bool IsImageLoading
        {
            get { return (bool)GetValue(IsImageLoadingProperty); }
            set { SetValue(IsImageLoadingProperty, value); }
        }

        /// <summary>
        /// The identifier for the IsImageLoading dependency property.
        /// </summary>
        public static readonly DependencyProperty IsImageLoadingProperty = DependencyProperty.Register("IsImageLoading", typeof(bool), typeof(ImageLoader), new PropertyMetadata(false));

        #endregion

        #region RenderAtSize

        /// <summary>
        /// Gets or sets a value indicating whether the image should be rendered at the size of the loader. Set to true if the image will never be scaled up.
        /// </summary>
        /// <value><c>true</c> if [render at size]; otherwise, <c>false</c>.</value>
        public bool RenderAtSize
        {
            get { return (bool)GetValue(RenderAtSizeProperty); }
            set { SetValue(RenderAtSizeProperty, value); }
        }

        /// <summary>
        /// The identifier for the RenderAtSize dependency property.
        /// </summary>
        public static readonly DependencyProperty RenderAtSizeProperty = DependencyProperty.Register("RenderAtSize", typeof(bool), typeof(ImageLoader), new PropertyMetadata(false));

        #endregion

        /// <summary>
        /// Gets the image displayed in this control.
        /// </summary>
        /// <value>The image displayed in this control.</value>
        internal BitmapSource BitmapImage
        {
            get
            {
                return _image == null ? null : _image.Source as BitmapSource;
            }
        }

        /// <summary>
        /// Occurs when the image has loaded, or failed loading.
        /// </summary>
        public event EventHandler ImageLoaded;

        #region IDisposable

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _worker.Dispose();
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
