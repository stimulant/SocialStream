using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Media.Animation;
using Blake.NUI.WPF.Touch;
using Microsoft.Surface;
using SocialStream.Helpers;
using SocialStream.Properties;
using System.Windows;

namespace SocialStream
{
    /// <summary>
    /// The main application window.
    /// </summary>
    public partial class SurfaceWindow : Microsoft.Surface.Presentation.Controls.SurfaceWindow
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SurfaceWindow"/> class.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Not an issue.")]
        public SurfaceWindow()
        {
            InitializeComponent();

            // Add handlers for Application activation events
            ApplicationServices.WindowInteractive += OnWindowInteractive;
            ApplicationServices.WindowNoninteractive += OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable += OnWindowUnavailable;

#if DEBUG
            if (System.Windows.SystemParameters.WorkArea.Width > 1920 || System.Windows.SystemParameters.WorkArea.Height > 1080)
            {
                // Shrink down when running on giant monitors
                Width = Math.Min(System.Windows.SystemParameters.WorkArea.Width, 1920);
                Height = Math.Min(System.Windows.SystemParameters.WorkArea.Height, 1080);
                WindowState = WindowState.Normal;
            }
#endif

            if (!SurfaceEnvironment.IsSurfaceEnvironmentAvailable)
            {
                // running in simulator.
                Startup();
            }

            // Add the configuable color resources.
            Resources["NewsThemeColor"] = Settings.Default.NewsThemeColor;
            Resources["NewsForegroundColor"] = Settings.Default.NewsForegroundColor;
            Resources["SocialThemeColor"] = Settings.Default.SocialThemeColor;
            Resources["SocialForegroundColor"] = Settings.Default.SocialForegroundColor;
            Resources["NewItemBorderColor"] = Settings.Default.NewItemBorderColor;

            // Don't put all the binding errors in the output window
            PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Critical;

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _layoutRoot.Opacity = 0;
            }

            // Promote mouse clicks to touch events.
            MouseTouchDevice.RegisterEvents(this);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Window.Closed"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            ApplicationServices.WindowInteractive -= OnWindowInteractive;
            ApplicationServices.WindowNoninteractive -= OnWindowNoninteractive;
            ApplicationServices.WindowUnavailable -= OnWindowUnavailable;

            base.OnClosed(e);
        }

        /// <summary>
        /// Set up the app state.
        /// </summary>
        private void Startup()
        {
            if (AppState.Instance.IsInitialized)
            {
                return;
            }

            Audio.Initialize();
            AppState.Instance.Initialize(_river);
            DataContext = AppState.Instance;

            // Fade the app in so that the loading message appears on the correct side before the user sees it.
            (Resources["ShowActivated"] as Storyboard).Begin(this);
        }

        /// <summary>
        /// Occurs when users can interact with the application.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnWindowInteractive(object sender, EventArgs e)
        {
            if (!AppState.Instance.IsInitialized)
            {
                Startup();
            }

            AppState.Instance.IsPaused = false;
        }

        /// <summary>
        /// Occurs when the application window is visible to users but users cannot interact with the application.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnWindowNoninteractive(object sender, EventArgs e)
        {
            if (SurfaceState.IsInSingleAppMode)
            {
                return;
            }

            AppState.Instance.IsPaused = true;
        }

        /// <summary>
        /// Occurs when the application is no longer visible to users in any way and should enter a low-CPU usage state, pause, and so on.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void OnWindowUnavailable(object sender, EventArgs e)
        {
            if (SurfaceState.IsInSingleAppMode)
            {
                return;
            }

            AppState.Instance.IsPaused = true;
        }
    }
}