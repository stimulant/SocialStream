using System.Windows;
using System.Windows.Controls;
using FeedProcessor.Enums;
using Microsoft.Surface.Presentation.Controls;
using SocialStream.Helpers;

namespace SocialStream.Controls
{
    /// <summary>
    /// Contains controls for an administrator of the application. Initiated by TagVisualizer.
    /// </summary>
    public partial class AdminPanel : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdminPanel"/> class.
        /// </summary>
        public AdminPanel()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Initialize control states.
        /// </summary>
        internal void Setup()
        {
            if (AppState.Instance.RetrievalOrder == RetrievalOrder.Chronological)
            {
                _chronologicalBtn.IsChecked = true;
            }
            else
            {
                _randomBtn.IsChecked = true;
            }

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
        /// Handles the Checked event of the mode RadioButtons.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ModeBtn_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == _chronologicalBtn && AppState.Instance.RetrievalOrder != RetrievalOrder.Chronological)
            {
                AppState.Instance.RetrievalOrder = RetrievalOrder.Chronological;
                _randomBtn.IsChecked = false;
            }
            else if (sender == _randomBtn && AppState.Instance.RetrievalOrder != RetrievalOrder.Random)
            {
                AppState.Instance.RetrievalOrder = RetrievalOrder.Random;
                _chronologicalBtn.IsChecked = false;
            }
        }

        /// <summary>
        /// Handles the Click event of the RemoveBansBtn control. Removes all bans.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.InputEventArgs"/> instance containing the event data.</param>
        private void RemoveBansBtn_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.RemoveBans();
        }

        /// <summary>
        /// Hide the admin panel when the close button is tapped.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.RoutedEventArgs"/> instance containing the event data.</param>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            AppState.Instance.IsAdminTagPresent = false;
        }

        /// <summary>
        /// When the volume is adjusted, play an audio sample to test it.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void VolumeSlider_AreAnyTouchesCapturedWithinChanged(object sender, RoutedEventArgs e)
        {
            if (!(sender as SurfaceSlider).AreAnyTouchesCapturedWithin)
            {
                Audio.Instance.PlayCue("streamItem_tapDown");
            }
        }

        /// <summary>
        /// When one of the direction options is checked, set the direction.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Direction_Checked(object sender, RoutedEventArgs e)
        {
            if (sender == _DirectionLeft)
            {
                AppState.Instance.AutoScrollDirection = -1;
            }
            else if (sender == _DirectionManual)
            {
                AppState.Instance.AutoScrollDirection = 0;
            }
            else if (sender == _DirectionRight)
            {
                AppState.Instance.AutoScrollDirection = 1;
            }
        }
    }
}
