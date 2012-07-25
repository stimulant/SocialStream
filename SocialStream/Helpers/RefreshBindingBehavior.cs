using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interactivity;
using System.Windows.Threading;

namespace SocialStream.Helpers
{
    /// <summary>
    /// An attached behavior to update the target of a binding every second.
    /// </summary>
    public class RefreshBindingBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// The timer which is used to update all the binding.
        /// </summary>
        private DispatcherTimer _watchTimer;

        /// <summary>
        /// The binding expression to update on the interval.
        /// </summary>
        private BindingExpression _bindingExpression;

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            _watchTimer = new DispatcherTimer(DispatcherPriority.DataBind) { Interval = RefreshInterval };
            UpdatePropertyToRefresh();
            base.OnAttached();
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected override void OnDetaching()
        {
            DependencyProperty dp = GetDependencyProperty();
            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(dp, AssociatedObject.GetType());
            dpd.RemoveValueChanged(AssociatedObject, ValueChanged);
            _watchTimer.Stop();
            _watchTimer.Tick -= WatchTimer_Tick;

            base.OnDetaching();
        }

        #region PropertyToRefresh

        /// <summary>
        /// Gets or sets the property to refresh. This should be the name of the property to update on an interval, such as "Text" for a TextBlock.
        /// </summary>
        /// <value>The property to refresh.</value>
        public string PropertyToRefresh
        {
            get { return (string)GetValue(PropertyToRefreshProperty); }
            set { SetValue(PropertyToRefreshProperty, value); }
        }

        /// <summary>
        /// Identifies the PropertyToRefresh dependency property.
        /// </summary>
        public static readonly DependencyProperty PropertyToRefreshProperty = DependencyProperty.Register("PropertyToRefresh", typeof(string), typeof(RefreshBindingBehavior), new PropertyMetadata(string.Empty, PropertyToRefreshPropertyChanged));

        /// <summary>
        /// Fires when the PropertyToRefresh property changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void PropertyToRefreshPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as RefreshBindingBehavior).UpdatePropertyToRefresh();
        }

        /// <summary>
        /// Fires when the PropertyToRefresh property is set.
        /// </summary>
        private void UpdatePropertyToRefresh()
        {
            if (AssociatedObject == null || DesignerProperties.GetIsInDesignMode(AssociatedObject))
            {
                return;
            }

            DependencyProperty dp = GetDependencyProperty();
            if (dp == null)
            {
                // Didn't find a property by the specified name.
                return;
            }

            _bindingExpression = AssociatedObject.GetBindingExpression(dp);

            if (_bindingExpression == null)
            {
                // There's no binding set on that property, so add a listener to catch when there is.
                DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(dp, AssociatedObject.GetType());
                dpd.AddValueChanged(AssociatedObject, ValueChanged);
                return;
            }

            // Found a binding, so start the timer.
            StartTimer();
        }

        #endregion

        #region RefreshInterval

        /// <summary>
        /// Gets or sets the interval at which the binding should be refreshed.
        /// </summary>
        public TimeSpan RefreshInterval
        {
            get { return (TimeSpan)GetValue(RefreshIntervalProperty); }
            set { SetValue(RefreshIntervalProperty, value); }
        }

        /// <summary>
        /// Identifies the RefreshInterval dependency property.
        /// </summary>
        public static readonly DependencyProperty RefreshIntervalProperty = DependencyProperty.Register("RefreshInterval", typeof(TimeSpan), typeof(RefreshBindingBehavior), new PropertyMetadata(TimeSpan.FromSeconds(1), RefreshIntervalPropertyChanged));

        /// <summary>
        /// Fires when RefreshInterval is changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void RefreshIntervalPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as RefreshBindingBehavior).UpdateRefreshInterval();
        }

        /// <summary>
        /// Fires when RefreshInterval is changed.
        /// </summary>
        private void UpdateRefreshInterval()
        {
            if (_watchTimer != null)
            {
                _watchTimer.Interval = RefreshInterval;
            }
        }

        #endregion

        /// <summary>
        /// Starts the timer.
        /// </summary>
        private void StartTimer()
        {
            if (_watchTimer.IsEnabled)
            {
                return;
            }

            _watchTimer.Tick -= WatchTimer_Tick;
            _watchTimer.Tick += WatchTimer_Tick;
            _watchTimer.Start();
        }

        /// <summary>
        /// Gets the dependency property from an element, based on the name specified in PropertyToRefresh.
        /// </summary>
        /// <returns>The DependencyProperty.</returns>
        private DependencyProperty GetDependencyProperty()
        {
            Type type = AssociatedObject.GetType();
            FieldInfo field = type.GetField(PropertyToRefresh + "Property");
            if (field == null)
            {
                return null;
            }

            return field.GetValue(AssociatedObject) as DependencyProperty;
        }

        /// <summary>
        /// Fires when the DependencyProperty's value goes from null to a binding expression.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void ValueChanged(object sender, EventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            DependencyProperty dp = GetDependencyProperty();
            _bindingExpression = element.GetBindingExpression(dp);
            if (_bindingExpression == null)
            {
                // Still no expression, keep waiting...
                return;
            }

            // Unhook the changed handler.
            DependencyPropertyDescriptor dpd = DependencyPropertyDescriptor.FromProperty(dp, element.GetType());
            dpd.RemoveValueChanged(element, ValueChanged);

            StartTimer();
        }

        /// <summary>
        /// Every time the timer ticks, update all the bindings.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void WatchTimer_Tick(object sender, EventArgs e)
        {
            _bindingExpression.UpdateTarget();
        }
    }
}
