using System;
using System.ComponentModel;
using System.Windows;

namespace SocialStream.Helpers
{
    /// <summary>
    /// The Surface SDK included changed events for some dependency properties, such as "IsAnyContactCapturedWithinChanged". These
    /// were not ported to WPF4, so this class reinstates them.
    /// </summary>
    public static class TouchChangedEvents
    {
        #region AreAnyTouchesCapturedWithinChanged

        /// <summary>
        /// Descriptor for AreAnyTouchesCapturedWithin.
        /// </summary>
        private static DependencyPropertyDescriptor _areAnyTouchesCapturedWithinProperty = DependencyPropertyDescriptor.FromProperty(UIElement.AreAnyTouchesCapturedWithinProperty, typeof(UIElement));

        /// <summary>
        /// Attached event description for AreAnyTouchesCapturedWithinChanged.
        /// </summary>
        public static readonly RoutedEvent AreAnyTouchesCapturedWithinChangedEvent = EventManager.RegisterRoutedEvent("AreAnyTouchesCapturedWithinChanged", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TouchChangedEvents));

        /// <summary>
        /// Adds a handler for the AreAnyTouchesCapturedWithinChanged event.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="handler">The handler.</param>
        public static void AddAreAnyTouchesCapturedWithinChangedHandler(DependencyObject target, RoutedEventHandler handler)
        {
            UIElement element = target as UIElement;
            if (element != null)
            {
                element.AddHandler(AreAnyTouchesCapturedWithinChangedEvent, handler);
                _areAnyTouchesCapturedWithinProperty.AddValueChanged(target, AreAnyTouchesCapturedWithinChangedHandler);
            }
        }

        /// <summary>
        /// Removes a handler for the AreAnyTouchesCapturedWithinChanged event.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="handler">The handler.</param>
        public static void RemoveAreAnyTouchesCapturedWithinChangedHandler(DependencyObject target, RoutedEventHandler handler)
        {
            UIElement element = target as UIElement;
            if (element != null)
            {
                element.RemoveHandler(AreAnyTouchesCapturedWithinChangedEvent, handler);
                _areAnyTouchesCapturedWithinProperty.RemoveValueChanged(target, AreAnyTouchesCapturedWithinChangedHandler);
            }
        }

        /// <summary>
        /// Internal handler for the dependency property's changed event.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private static void AreAnyTouchesCapturedWithinChangedHandler(object sender, EventArgs e)
        {
            UIElement element = sender as UIElement;
            if (element != null)
            {
                element.RaiseEvent(new RoutedEventArgs(AreAnyTouchesCapturedWithinChangedEvent));
            }
        }

        #endregion
    }
}
