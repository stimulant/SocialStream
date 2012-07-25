using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Input;

namespace SocialStream.Controls
{
    /// <summary>
    /// Interaction logic for BackgroundImage.xaml
    /// </summary>
    public partial class BackgroundImage : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundImage"/> class.
        /// </summary>
        public BackgroundImage()
        {
            InitializeComponent();

            InteractiveSurface.PrimarySurfaceDevice.TiltChanged += (sender, e) => UpdateBackground();
            UpdateBackground();
        }

        /// <summary>
        /// Updates the background image according to the tilt of the surface device.
        /// </summary>
        private void UpdateBackground()
        {
            _LayoutRoot.Background = Resources[InteractiveSurface.PrimarySurfaceDevice.Tilt == Tilt.Vertical ? "VerticalBackground" : "HorizontalBackground"] as ImageBrush;
        }
    }
}
