using System.Windows;
using System.Windows.Controls;

namespace SocialStream.Controls
{
    /// <summary>
    /// Base class from which items in the river can inherit in order to communicate with the river.
    /// </summary>
    internal abstract class RiverItemBase : UserControl
    {
        /// <summary>
        /// Called by the river when the item should retrieve data from its data source.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="maintainUnblockedData">if set to <c>true</c> [maintain unblocked data].</param>
        /// <returns>
        /// The data that this item will render. If null, the item won't be shown.
        /// </returns>
        internal abstract object GetData(RiverItemState state, bool maintainUnblockedData);

        /// <summary>
        /// Called by the river when the item should render some data.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="data">The data that the river is requesting to be rendered. The item can override this and return different data if needed.</param>
        /// <returns>
        /// The data that this item will render. If null, the item won't be shown.
        /// </returns>
        internal abstract object RenderData(RiverItemState state, object data);

        /// <summary>
        /// Called by the river when this item is being hidden because it scrolled out of view.
        /// </summary>
        internal abstract void Cleanup();

        /// <summary>
        /// Called when the item is removed from the river by the user.
        /// </summary>
        /// <returns>Sizing restrictions for the item once its in the river.</returns>
        internal abstract RiverSize Removed();

        /// <summary>
        /// Called when the item is added back to the river due to a timeout.
        /// </summary>
        internal abstract void Added();

        #region CloseRequested

        /// <summary>
        /// Occurs when the close button is tapped.
        /// </summary>
        public static readonly RoutedEvent CloseRequestedEvent = EventManager.RegisterRoutedEvent("CloseRequested", RoutingStrategy.Bubble, typeof(SourceRoutedEventHandler), typeof(RiverItemBase));

        /// <summary>
        /// Occurs when the close button is tapped.
        /// </summary>
        public event RoutedEventHandler CloseRequested
        {
            add { AddHandler(CloseRequestedEvent, value); }
            remove { RemoveHandler(CloseRequestedEvent, value); }
        }

        #endregion

        #region FlipRequested

        /// <summary>
        /// Occurs when flip button is tapped.
        /// </summary>
        public static readonly RoutedEvent FlipRequestedEvent = EventManager.RegisterRoutedEvent("FlipRequested", RoutingStrategy.Bubble, typeof(SourceRoutedEventHandler), typeof(RiverItemBase));

        /// <summary>
        /// Occurs when flip button is tapped.
        /// </summary>
        public event SourceRoutedEventHandler FlipRequested
        {
            add { AddHandler(FlipRequestedEvent, value); }
            remove { RemoveHandler(FlipRequestedEvent, value); }
        }

        #endregion

        #region RefreshRequested

        /// <summary>
        /// Occurs when the item wants new content.
        /// </summary>
        public static readonly RoutedEvent RefreshRequestedEvent = EventManager.RegisterRoutedEvent("RefreshRequested", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RiverItemBase));

        /// <summary>
        /// Occurs when the item wants new content.
        /// </summary>
        public event RoutedEventHandler RefreshRequested
        {
            add { AddHandler(RefreshRequestedEvent, value); }
            remove { RemoveHandler(RefreshRequestedEvent, value); }
        }

        #endregion

        /// <summary>
        /// Represents the method that is called when the FlipRequested and CloseRequested events fire.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The UserSourceRoutedEventArgs object.</param>
        public delegate void SourceRoutedEventHandler(object sender, UserSourceRoutedEventArgs e);

        /// <summary>
        /// Called when the item has been removed from the river and its growth animation has completed.
        /// </summary>
        internal abstract void RemoveFinished();
    }

    /// <summary>
    /// Describes the various size restrictions applied to an item in the river.
    /// </summary>
    internal struct RiverSize
    {
        /// <summary>
        /// Gets or sets the minimum size of the ScatterViewItem.
        /// </summary>
        /// <value>The size of the minimum size of the ScatterViewItem.</value>
        public Size MinSize { get; set; }

        /// <summary>
        /// Gets or sets the size that the ScatterViewItem will animated to when it's removed.
        /// </summary>
        /// <value>The size that the ScatterViewItem will animated to when it's removed.</value>
        public Size RemovedSize { get; set; }

        /// <summary>
        /// Gets or sets the maximum size of the ScatterViewItem.
        /// </summary>
        /// <value>The size of the maximum size of the ScatterViewItem.</value>
        public Size MaxSize { get; set; }
    }
}
