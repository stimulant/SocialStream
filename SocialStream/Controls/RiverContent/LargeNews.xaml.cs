using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FeedProcessor.FeedItems;
using HTMLConverter;
using SocialStream.Helpers;
using SocialStream.Properties;

namespace SocialStream.Controls.RiverContent
{
    /// <summary>
    /// A river item which shows the large version of news.
    /// </summary>
    public partial class LargeNews : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LargeNews"/> class.
        /// </summary>
        public LargeNews()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            // Resize images when the RTB size changes
            _richTextBox.SizeChanged += (sender, e) => SizeImages();

            /*
            Loaded += (sender, e) =>
            {
                // Resize images when the parent Plane rotates
                DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(Plane.RotationXProperty, typeof(Plane));
                dpd.AddValueChanged(this.FindVisualParent<Plane>(), (a, b) => SizeImages());
            };
             */
        }

        /// <summary>
        /// When populated with data, convert the HTML to XAML and display it in the RichTextBox.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Intentionally catching all exceptions from this external library.")]
        internal void RenderContent()
        {
            NewsFeedItem item = DataContext as NewsFeedItem;
            if (item == null)
            {
                return;
            }

            _scroller.ScrollToTop();
            VisualStateManager.GoToState(this, Success.Name, true);

            try
            {
                // Build XAML using HtmlToXamlConverter, from here: http://windowsclient.net/downloads/folders/controlgallery/entry2313.aspx
                // Note that this has been modified quite a bit to add image support and disable some features. Search for "DISABLE" in
                // HtmlToXamlConverter.cs to see what's been turned off.
                string xaml = HtmlToXamlConverter.ConvertHtmlToXaml(item.Body, true);

                // Remove dumb images
                xaml = Regex.Replace(xaml, "<(Inline|Block)UIContainer><Image Source=\"[^\"]*(feeds\\.wordpress|feedburner|doubleclick).*?\" /></(Inline|Block)UIContainer>", string.Empty);

                // Try to repair images that use relative paths beginning with /.
                if (item.Uri != null)
                {
                    xaml = Regex.Replace(xaml, "Source=\"(/.*?)\"", "Source=\"http://" + item.Uri.Host + "$1\"");
                }

                // Remove empty paragraphs.
                xaml = Regex.Replace(xaml, "<Paragraph />", string.Empty);
                xaml = Regex.Replace(xaml, "<Paragraph>\\s*?</Paragraph>", string.Empty);
                xaml = Regex.Replace(xaml, "<Paragraph><LineBreak />", "<Paragraph>");
                xaml = Regex.Replace(xaml, "<LineBreak /></Paragraph>", "</Paragraph>");
                xaml = Regex.Replace(xaml, "<Paragraph>(<(Run|LineBreak) />\\s)*?</Paragraph>", string.Empty);
                xaml = Regex.Replace(xaml, "<Paragraph><Run> </Run></Paragraph>", string.Empty);

                // Build FlowDocument.
                byte[] byteArray = Encoding.UTF8.GetBytes(xaml);
                using (MemoryStream stream = new MemoryStream(byteArray))
                {
                    FlowDocument document = XamlReader.Load(stream) as FlowDocument;
                    _richTextBox.Document = document;
                    _richTextBox.UpdateLayout();
                    SizeImages();
                }
            }
            catch
            {
                VisualStateManager.GoToState(this, Fail.Name, true);
            }
        }

        /// <summary>
        /// Handles the Click event of the Close button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.InputEventArgs"/> instance containing the event data.</param>
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
        /// Handles the SizeChanged event of the Image control. Update image sizes once they've loaded.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.SizeChangedEventArgs"/> instance containing the event data.</param>
        private void Image_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SizeImages();
        }

        /// <summary>
        /// Look for images within the RichTextBox and set them to scale only as large as the RTB.
        /// </summary>
        private void SizeImages()
        {
            foreach (Image image in _richTextBox.GetVisualOfType<Image>())
            {
                image.SizeChanged -= Image_SizeChanged;
                BitmapSource source = image.Source as BitmapSource;
                if (source == null)
                {
                    continue;
                }

                image.Width = Math.Min(source.PixelWidth, _richTextBox.ViewportWidth);
                image.Height = double.NaN;
                image.MaxWidth = image.MaxHeight = double.PositiveInfinity;
                image.Stretch = Stretch.UniformToFill;

                if (source.PixelWidth <= 1)
                {
                    image.SizeChanged += Image_SizeChanged;
                }
            }
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
            _scroller.Loaded += Scroller_Loaded;
        }

        /// <summary>
        /// When resizing is enabled, force an update to the shadows when the scroller loads.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Scroller_Loaded(object sender, RoutedEventArgs e)
        {
            _shadowScroll.UpdateShadows();
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
            _scroller.Loaded -= Scroller_Loaded;
        }
    }
}
