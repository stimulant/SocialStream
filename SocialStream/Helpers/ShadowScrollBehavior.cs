using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace SocialStream.Helpers
{
    /// <summary>
    /// A behavior to show "shadows" on sides of a ScrollViewer to indicate that there is additional content in a given
    /// direction. Best used with a style that only shows the scrollbar during scrolling user interaction.
    /// </summary>
    public class ShadowScrollBehavior : Behavior<ScrollViewer>
    {
        /// <summary>
        /// A reference to the last content placed inside the ScrollViewer.
        /// </summary>
        private FrameworkElement _content;

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            AssociatedObject.ScrollChanged += ScrollViewer_ScrollChanged;
            AssociatedObject.SizeChanged += ScrollViewer_SizeChanged;

            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(ScrollViewer.ContentProperty, typeof(ScrollViewer));
            descriptor.AddValueChanged(AssociatedObject, ContentChanged);

            UpdateContent();
            UpdateShadows();

            base.OnAttached();
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected override void OnDetaching()
        {
            AssociatedObject.ScrollChanged -= ScrollViewer_ScrollChanged;
            AssociatedObject.SizeChanged -= ScrollViewer_SizeChanged;

            DependencyPropertyDescriptor descriptor = DependencyPropertyDescriptor.FromProperty(ScrollViewer.ContentProperty, typeof(ScrollViewer));
            descriptor.RemoveValueChanged(AssociatedObject, ContentChanged);

            VisualStateManager.GoToState(AssociatedObject, "HideTopShadow", true);
            VisualStateManager.GoToState(AssociatedObject, "HideBottomShadow", true);
            VisualStateManager.GoToState(AssociatedObject, "HideLeftShadow", true);
            VisualStateManager.GoToState(AssociatedObject, "HideRightShadow", true);

            if (_content != null)
            {
                _content.SizeChanged -= Content_SizeChanged;
            }

            _content = null;

            base.OnDetaching();
        }

        /// <summary>
        /// Handles the SizeChanged event of the ScrollViewer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.SizeChangedEventArgs"/> instance containing the event data.</param>
        private void ScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShadows();
        }

        /// <summary>
        /// When the content of the ScrollViewer changes, hook up a listener to it.
        /// </summary>
        /// <param name="sender">The ScrollViewer.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ContentChanged(object sender, EventArgs e)
        {
            UpdateContent();
        }

        /// <summary>
        /// When the content of the ScrollViewer changes, stop listening for size changes on the old content, and look at the new content instead.
        /// </summary>
        private void UpdateContent()
        {
            if (_content != null)
            {
                _content.SizeChanged -= Content_SizeChanged;
            }

            _content = AssociatedObject.Content as FrameworkElement;

            if (_content != null)
            {
                _content.SizeChanged += Content_SizeChanged;
            }
        }

        /// <summary>
        /// When the size of the content changes, update the scroll bounds.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.SizeChangedEventArgs"/> instance containing the event data.</param>
        private void Content_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateShadows();
        }

        /// <summary>
        /// Handles the ScrollChanged event of the ScrollViewer control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.ScrollChangedEventArgs"/> instance containing the event data.</param>
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateShadows();
        }

        /// <summary>
        /// Show or hide the shadows based on the current scroll position.
        /// </summary>
        public void UpdateShadows()
        {
            if (_content == null)
            {
                return;
            }

            FrameworkElement viewport = AssociatedObject.Template.FindName("PART_ConsistentViewport", AssociatedObject) as FrameworkElement;

            if (viewport == null)
            {
                return;
            }

            Size scrollBounds = new Size(Math.Max(0, _content.ActualWidth - viewport.ActualWidth), Math.Max(0, _content.ActualHeight - viewport.ActualHeight));
            VisualStateManager.GoToState(AssociatedObject, AssociatedObject.VerticalOffset > 0 ? "ShowTopShadow" : "HideTopShadow", true);
            VisualStateManager.GoToState(AssociatedObject, AssociatedObject.VerticalOffset < scrollBounds.Height ? "ShowBottomShadow" : "HideBottomShadow", true);
            VisualStateManager.GoToState(AssociatedObject, AssociatedObject.HorizontalOffset > 0 ? "ShowLeftShadow" : "HideLeftShadow", true);
            VisualStateManager.GoToState(AssociatedObject, AssociatedObject.HorizontalOffset < scrollBounds.Width ? "ShowRightShadow" : "HideRightShadow", true);
        }
    }
}
