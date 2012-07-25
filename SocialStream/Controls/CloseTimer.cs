using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Animation;
using Microsoft.Surface.Presentation.Controls;

namespace SocialStream.Controls
{
    /// <summary>
    /// When bound to River.TimeoutDelay, displays a timeout animation for the last portion of that delay.
    /// </summary>
    internal class CloseTimer : Control
    {
        /// <summary>
        /// The storyboard which contains the timeout animation.
        /// </summary>
        private Storyboard _timeout;

        /// <summary>
        /// The root element of the template, which contains the timeout storyboard.
        /// </summary>
        private Panel _layoutRoot;

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            // Setting this in XAML causes Blend to freak out.
            Binding binding = new Binding()
            {
                Path = new PropertyPath(River.TimeoutDelayProperty),
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ScatterViewItem), 1)
            };

            BindingOperations.SetBinding(this, TimeoutDelayProperty, binding);

            _layoutRoot = GetTemplateChild("PART_LayoutRoot") as Panel;
            Loaded += (sender, e) => UpdateTimeoutDelay();
            UpdateTimeoutDelay();

            base.OnApplyTemplate();
        }

        #region TimeoutDelay

        /// <summary>
        /// Gets or sets the time before the parent item will get closed automatically.
        /// </summary>
        public TimeSpan TimeoutDelay
        {
            get { return (TimeSpan)GetValue(TimeoutDelayProperty); }
            set { SetValue(TimeoutDelayProperty, value); }
        }

        /// <summary>
        /// Backing store for TimeoutDelay.
        /// </summary>
        public static readonly DependencyProperty TimeoutDelayProperty = DependencyProperty.Register("TimeoutDelay", typeof(TimeSpan), typeof(CloseTimer), new PropertyMetadata(TimeSpan.Zero, TimeoutDelayPropertyChanged));

        /// <summary>
        /// Fires when TimeoutDelay changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void TimeoutDelayPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as CloseTimer).UpdateTimeoutDelay();
        }

        /// <summary>
        /// Updates the TimeoutDelay.
        /// </summary>
        private void UpdateTimeoutDelay()
        {
            if (_layoutRoot == null || !IsLoaded)
            {
                return;
            }

            if (_timeout != null)
            {
                _timeout.Stop(this);
            }

            if (TimeoutDelay == TimeSpan.Zero)
            {
                return;
            }

            if (_timeout == null)
            {
                _timeout = (_layoutRoot.Resources["Timeout"] as Storyboard).Clone();
            }

            (_timeout.Children[0] as ThicknessAnimation).BeginTime = TimeoutDelay - TimeSpan.FromSeconds(5);
            _timeout.Begin(this, Template, true);
        }

        #endregion
    }
}
