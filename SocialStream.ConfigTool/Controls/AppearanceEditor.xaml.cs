using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Surface.Presentation;
using Microsoft.Win32;
using SocialStream.ConfigTool.Command;
using SocialStream.ConfigTool.Model;

namespace SocialStream.ConfigTool.Controls
{
    /// <summary>
    /// Interaction logic for AppearanceEditor.xaml
    /// </summary>
    public partial class AppearanceEditor : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AppearanceEditor"/> class.
        /// </summary>
        public AppearanceEditor()
        {
            InitializeComponent();

            IsVisibleChanged += AppearanceEditor_IsVisibleChanged;

            Loaded += AppearanceEditor_Loaded;
        }

        /// <summary>
        /// Handles the Loaded event of the AppearanceEditor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void AppearanceEditor_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= AppearanceEditor_Loaded;

            UpdateScrollDirection();
            UpdateBackgroundPreviews();

            AppState.Instance.PropertyChanged += (a, b) =>
            {
                if (b.PropertyName == "AutoScrollDirection")
                {
                    UpdateScrollDirection();
                    UpdateBackgroundPreviews();
                }
            };
        }

        /// <summary>
        /// Updates the scroll direction.
        /// </summary>
        private void UpdateScrollDirection()
        {
            if (AppState.Instance.AutoScrollDirection < 0)
            {
                _DirectionLeft.IsChecked = true;
            }
            else if (AppState.Instance.AutoScrollDirection == 0)
            {
                _DirectionManual.IsChecked = true;
            }
            else if (AppState.Instance.AutoScrollDirection > 0)
            {
                _DirectionRight.IsChecked = true;
            }
        }

        /// <summary>
        /// Handles the IsVisibleChanged event of the AppearanceEditor control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void AppearanceEditor_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            _ScrollViewer.ScrollToTop();
        }

        /// <summary>
        /// Handles the Checked event of the Radio control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radio = sender as RadioButton;
            if (!radio.IsLoaded)
            {
                return;
            }

            SetDistributeContentEvenlyCommand.Execute(bool.Parse(radio.Tag.ToString()));
        }

        /// <summary>
        /// Handles the Checked event of the DirectionRadio control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DirectionRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == _DirectionLeft)
            {
                SetAutoScrollDirectionCommand.Execute(-1);
            }
            else if (sender == _DirectionManual)
            {
                SetAutoScrollDirectionCommand.Execute(0);
            }
            else if (sender == _DirectionRight)
            {
                SetAutoScrollDirectionCommand.Execute(1);
            }
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event of the Preview control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void Preview_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog open = new OpenFileDialog()
            {
                Filter = "PNG Images (.png)|*.png",
                InitialDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            };

            if (open.ShowDialog() != true)
            {
                return;
            }

            BitmapFrame frame = null;

            try
            {
                using (FileStream stream = File.Open(open.FileName, FileMode.Open))
                {
                    frame = new PngBitmapDecoder(stream, BitmapCreateOptions.None, BitmapCacheOption.OnDemand).Frames[0];
                }
            }
            catch
            {
            }

            if (frame == null || frame.PixelWidth != 1920 || frame.PixelHeight != 1080)
            {
                MessageBox.Show(Properties.Resources.BackgroundFormat);
                return;
            }

            SetBackgroundImagePathCommand.Execute(e.Source == _HorizontalPreview ? Tilt.Horizontal : Tilt.Vertical, open.FileName);
            UpdateBackgroundPreviews();
        }

        /// <summary>
        /// Updates the background previews by reading from the background files and creating a new image from them, 
        /// so that the original files aren't locked and can be replaced.
        /// </summary>
        private void UpdateBackgroundPreviews()
        {
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(AppState.Instance.HorizontalBackgroundPath);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            _HorizontalPreview.Source = bi;

            bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(AppState.Instance.VerticalBackgroundPath);
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.EndInit();
            _VerticalPreview.Source = bi;
        }
    }
}
