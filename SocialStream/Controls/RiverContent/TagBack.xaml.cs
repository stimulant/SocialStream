using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FeedProcessor;
using FeedProcessor.Enums;
using SocialStream.MicrosoftTagService;
using SocialStream.Properties;

namespace SocialStream.Controls.RiverContent
{
    /// <summary>
    /// Interaction logic for TagBack.xaml
    /// </summary>
    public partial class TagBack : UserControl
    {
        /// <summary>
        /// The credential passed along to tag service requests.
        /// </summary>
        private static UserCredential _tagCredential = new UserCredential { AccessToken = Settings.Default.MicrosoftTagApiKey };

        /// <summary>
        /// The category name used for creating new tags.
        /// </summary>
        private static string _tagCategory = "Main";

        /// <summary>
        /// Whether or not the last named tag has been loaded.
        /// </summary>
        private bool _isTagLoaded;

        /// <summary>
        /// Thread to build the tags.
        /// </summary>
        private BackgroundWorker _TagWorker;

        /// <summary>
        /// Initializes a new instance of the <see cref="TagBack"/> class.
        /// </summary>
        public TagBack()
        {
            InitializeComponent();

            // The use of Plane causes problems in resolving resources in the content that appears on the back, so we have
            // to do this and include Resources.xaml in the the TagBack control.
            Resources["ThemeColor"] = Settings.Default.ThemeColor;
            Resources["ForegroundColor"] = Settings.Default.ForegroundColor;

            DataContextChanged += new DependencyPropertyChangedEventHandler(TagBack_DataContextChanged);
        }

        /// <summary>
        /// Handles the DataContextChanged event of the TagBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void TagBack_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            FeedItem feedItem = DataContext as FeedItem;

            if (feedItem == null)
            {
                return;
            }

            string labelState = NoLabel.Name;
            string contentTypeState = NotStatus.Name;
            if (feedItem != null)
            {
                if (feedItem.ContentType == ContentType.Image)
                {
                    labelState = ImageLabel.Name;
                }
                else if (feedItem.ContentType == ContentType.News)
                {
                    labelState = NewsLabel.Name;
                }
                else if (feedItem.ContentType == ContentType.Status)
                {
                    labelState = StatusLabel.Name;
                    contentTypeState = IsStatus.Name;
                }
            }

            VisualStateManager.GoToState(this, labelState, true);
            VisualStateManager.GoToState(this, contentTypeState, true);
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
            RaiseEvent(new UserSourceRoutedEventArgs(RiverContentItem.FlipRequestedEvent, _flipBtn));
        }

        /// <summary>
        /// Backing store for TagName.
        /// </summary>
        private string _tagName;

        /// <summary>
        /// Gets or sets the name of the tag to load.
        /// </summary>
        /// <value>The name of the tag.</value>
        public string TagName
        {
            get
            {
                return _tagName;
            }

            set
            {
                _tagName = value;
                _isTagLoaded = false;
            }
        }

        #region IsFlipped

        /// <summary>
        /// Gets or sets a value indicating whether this instance is flipped.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is flipped; otherwise, <c>false</c>.
        /// </value>
        public bool IsFlipped
        {
            get { return (bool)GetValue(IsFlippedProperty); }
            set { SetValue(IsFlippedProperty, value); }
        }

        /// <summary>
        /// Backing store for IsFlipped.
        /// </summary>
        public static readonly DependencyProperty IsFlippedProperty = DependencyProperty.Register(
            "IsFlipped",
            typeof(bool),
            typeof(TagBack),
            new PropertyMetadata(false, (sender, e) => (sender as TagBack).UpdateIsFlipped()));

        /// <summary>
        /// When the item is flipped, load the MS Tag.
        /// </summary>
        private void UpdateIsFlipped()
        {
            if (!IsFlipped || _isTagLoaded == true)
            {
                return;
            }

            LoadTag();
        }

        #endregion

        #region Tag Loading

        /// <summary>
        /// Begins loading the tag.
        /// </summary>
        private void LoadTag()
        {
            _tagImageHorizontal.Source = _tagImageVertical.Source = null;
            VisualStateManager.GoToState(this, TagLoading.Name, true);

            // Create and load the tag on a background thread.
            if (_TagWorker != null)
            {
                _TagWorker.CancelAsync();
            }

            _TagWorker = new BackgroundWorker() { WorkerSupportsCancellation = true };

            _TagWorker.DoWork += (sender, e) =>
            {
                object[] args = e.Argument as object[];
                e.Result = CreateTag(args[0] as UserCredential, args[1] as string, args[2] as Uri);
            };

            _TagWorker.RunWorkerCompleted += (sender, e) =>
            {
                if (e.Result != null)
                {
                    VisualStateManager.GoToState(this, ActualWidth > ActualHeight ? TagLoadedHorizontal.Name : TagLoadedVertical.Name, true);
                    _tagImageHorizontal.Source = _tagImageVertical.Source = e.Result as BitmapSource;
                }
                else
                {
                    VisualStateManager.GoToState(this, TagFailed.Name, true);
                }
            };

            _TagWorker.RunWorkerAsync(new object[] { _tagCredential, _tagCategory, new Uri(_tagName) });
        }

        /// <summary>
        /// Creates an MS tag for a given URL.
        /// </summary>
        /// <param name="credential">The API credential.</param>
        /// <param name="category">The tag category.</param>
        /// <param name="link">The link to encode in the tag.</param>
        /// <returns>The tag image.</returns>
        private BitmapSource CreateTag(UserCredential credential, string category, Uri link)
        {
            // Hash the URL for use in the title, because if the URL is too long, the tag service gets angry.
            string hash = GetMD5Hash(link.OriginalString);

            // Define the range for which the tag is valid.
            DateTime tagStartDate = DateTime.UtcNow.AddDays(-1);
            DateTime tagEndDate = DateTime.UtcNow.AddDays(90);

            MIBPContractClient client = new MIBPContractClient();
            Tag tag = null;

            try
            {
                // See if this tag already exists.
                tag = client.GetTagByTagName(credential, category, hash);

                if (tag.UTCEndDate > tagEndDate)
                {
                    // If the tag is expired, change the end date so that it will work again.
                    tag.UTCStartDate = tagStartDate;
                    tag.UTCEndDate = tagEndDate;

                    try
                    {
                        client.UpdateTag(credential, category, hash, tag);
                    }
                    catch
                    {
                        return null;
                    }
                }
            }
            catch
            {
                // The tag wasn't found, so create a new one.
                tag = new URITag
                {
                    MedFiUrl = link.OriginalString,
                    Title = hash,
                    UTCStartDate = tagStartDate,
                    UTCEndDate = tagEndDate,
                };

                try
                {
                    client.CreateTag(credential, category, tag);
                }
                catch
                {
                    return null;
                }
            }

            try
            {
                byte[] barcode = client.GetBarcode(credential, category, hash, ImageTypes.png, .8f, DecorationType.HCCBRP_DECORATION_NONE, false);
                BitmapSource bmp = new PngBitmapDecoder(new MemoryStream(barcode), BitmapCreateOptions.None, BitmapCacheOption.Default).Frames[0];
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the MD5 hash of a string.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>The MD5 hash of a string</returns>
        private static string GetMD5Hash(string input)
        {
            using (MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(input);
                bytes = md5.ComputeHash(bytes);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    stringBuilder.Append(b.ToString("x2", CultureInfo.InvariantCulture).ToUpperInvariant());
                }

                return stringBuilder.ToString();
            }
        }

        #endregion
    }
}
