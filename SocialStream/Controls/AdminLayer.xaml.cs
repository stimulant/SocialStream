using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Surface.Presentation.Input;
using SocialStream.Helpers;
using SocialStream.Properties;

namespace SocialStream.Controls
{
    /// <summary>
    /// Controls the visibility of the AdminPanel.
    /// </summary>
    public partial class AdminLayer : UserControl
    {
        /// <summary>
        /// A timer which closes the admin panel after a specified interval.
        /// </summary>
        private DispatcherTimer _idleTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminLayer"/> class.
        /// </summary>
        public AdminLayer()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            (Application.Current.MainWindow as SurfaceWindow).PreviewTouchDown += SurfaceWindow_PreviewContactDown;
            _panelContainer.ContainerManipulationDelta += (sender, e) => BeginIdleTimeout();
            TouchChangedEvents.AddAreAnyTouchesCapturedWithinChangedHandler(_panelContainer, new RoutedEventHandler(AdminPanel_AreAnyTouchesCapturedWithinChanged));
            _idleTimer = new DispatcherTimer { Interval = Settings.Default.AdminTimeoutDelay };
            _idleTimer.Tick += new EventHandler(IdleTimer_Tick);
        }

        /// <summary>
        /// Handles the PreviewContactDown event of the SurfaceWindow control. When the admin tag is placed, show the admin panel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.TouchEventArgs"/> instance containing the event data.</param>
        private void SurfaceWindow_PreviewContactDown(object sender, TouchEventArgs e)
        {
            if (e.TouchDevice.GetTagData().Series == Settings.Default.AdminByteTagSeries && e.TouchDevice.GetTagData().Value == Settings.Default.AdminByteTagValue)
            {
                if (!AppState.Instance.IsAdminTagPresent)
                {
                    _panelContainer.Orientation = Math.Round(e.TouchDevice.GetOrientation(this) / 90) * 90;

                    _panelContainer.Center = new Point(ActualWidth / 2, ActualHeight / 2);
                    _panel.Setup();
                    Audio.Instance.PlayCue("adminPanel_open");
                    AppState.Instance.IsAdminTagPresent = true;
                }

                BeginIdleTimeout();
            }
        }

        #region IsAdminTagPresent

        /// <summary>
        /// Gets or sets a value indicating whether the admin panel should be shown.
        /// </summary>
        /// <value>
        /// <c>true</c> if the admin panel should be shown; otherwise, <c>false</c>.
        /// </value>
        public bool IsAdminTagPresent
        {
            get { return (bool)GetValue(IsAdminTagPresentProperty); }
            set { SetValue(IsAdminTagPresentProperty, value); }
        }

        /// <summary>
        /// Identifies the IsAdminTagPresent dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAdminTagPresentProperty = DependencyProperty.Register("IsAdminTagPresent", typeof(bool), typeof(AdminLayer), new PropertyMetadata(false, (sender, e) => (sender as AdminLayer).UpdateIsAdminTagPresent()));

        /// <summary>
        /// Shows or hides the admin panel.
        /// </summary>
        private void UpdateIsAdminTagPresent()
        {
            (Resources[IsAdminTagPresent ? "ShowAdminPanel" : "HideAdminPanel"] as Storyboard).Begin(this);
        }

        #endregion

        /// <summary>
        /// Cancels the idle timeout.
        /// </summary>
        private void CancelIdleTimeout()
        {
            _idleTimer.Stop();
            River.SetTimeoutDelay(_panelContainer, TimeSpan.Zero);
        }

        /// <summary>
        /// Begins the idle timeout.
        /// </summary>
        private void BeginIdleTimeout()
        {
            CancelIdleTimeout();
            _idleTimer.Start();
            River.SetTimeoutDelay(_panelContainer, Settings.Default.AdminTimeoutDelay);
        }

        /// <summary>
        /// When the idle timer ticks, close the admin panel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void IdleTimer_Tick(object sender, EventArgs e)
        {
            CancelIdleTimeout();
            AppState.Instance.IsAdminTagPresent = false;
        }

        /// <summary>
        /// Cancel and restart the idle timer when child controls capture contacts.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void AdminPanel_AreAnyTouchesCapturedWithinChanged(object sender, EventArgs e)
        {
            if (_panelContainer.AreAnyTouchesCapturedWithin)
            {
                CancelIdleTimeout();
            }
            else
            {
                BeginIdleTimeout();
            }
        }
    }
}
