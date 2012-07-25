using System;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Surface.Presentation.Controls;

namespace SocialStream.Controls
{
    /// <summary>
    /// A class which maintains the state of an item in the river.
    /// </summary>
    internal class RiverItemState
    {
        /// <summary>
        /// Gets or sets the column at which this item is being shown.
        /// </summary>
        /// <value>The column at which this item is being shown.</value>
        internal int Column { get; set; }

        /// <summary>
        /// Gets or sets the row at which this item is being shown.
        /// </summary>
        /// <value>The row at which this item is being shown.</value>
        internal int Row { get; set; }

        /// <summary>
        /// Gets or sets the number of columns this item spans.
        /// </summary>
        /// <value>The number of columns this item spans.</value>
        internal int ColumnSpan { get; set; }

        /// <summary>
        /// Gets or sets the number of rows this item spans.
        /// </summary>
        /// <value>The number of columns this item spans.</value>
        internal int RowSpan { get; set; }

        /// <summary>
        /// Gets or sets the item style. This should be applied to elements within the template supplied to the GridLayout property.
        /// The specified style will be used to render that item as a proxy in the river.
        /// </summary>
        /// <value>The item style.</value>
        internal Style ProxyStyle { get; set; }

        /// <summary>
        /// Gets or sets the item style. This should be applied to elements within the template supplied to the GridLayout property.
        /// The specified style will be used to render that item as a ScatterViewItem in the river.
        /// </summary>
        /// <value>The item style.</value>
        internal Style ItemStyle { get; set; }

        /// <summary>
        /// Gets or sets the orientation at which to end the return-to-river animation.
        /// </summary>
        /// <value>The orientation at which to end the return-to-river animation.</value>
        internal double OriginalOrientation { get; set; }

        /// <summary>
        /// Gets or sets the HorizontalOffset that the River was at when an item was pulled from the river.
        /// </summary>
        /// <value>The HorizontalOffset that the River was at when an item was pulled from the river.</value>
        internal double OriginalHorizontalOffset { get; set; }

        /// <summary>
        /// Gets or sets the HorizontalOffset that the River was at when an item began to be returned to the river.
        /// </summary>
        /// <value>The HorizontalOffset that the River was at when an item began to be returned to the river.</value>
        internal double ReturnHorizontalOffset { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is animating back to the river.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is animating back to the river; otherwise, <c>false</c>.
        /// </value>
        internal bool IsAnimatingBackToRiver { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has animated back to the river and is waiting to be removed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has animated back to the river and is waiting to be removed; otherwise, <c>false</c>.
        /// </value>
        internal bool IsAnimatingBackToRiverComplete { get; set; }

        /// <summary>
        /// Gets or sets the time at which the return-to-river animation began.
        /// </summary>
        /// <value>The time at which the return-to-river animation began.</value>
        internal DateTime AnimateBackBeginTime { get; set; }

        /// <summary>
        /// Gets or sets the center point at which to begin the return-to-river animation.
        /// </summary>
        /// <value>The center point at which to begin the return-to-river animation.</value>
        internal Point AnimateBackFromCenter { get; set; }

        /// <summary>
        /// Gets or sets the orientation at which to begin the return-to-river animation.
        /// </summary>
        /// <value>The orientation at which to begin the return-to-river animation.</value>
        internal double AnimateBackFromOrientation { get; set; }

        /// <summary>
        /// Gets or sets the orientation to which the item will rotate when activated.
        /// </summary>
        /// <value>The orientation to which the item will rotate when activated.</value>
        internal double ContactOrientation { get; set; }

        /// <summary>
        /// Gets or sets the size at which to begin the return-to-river animation.
        /// </summary>
        /// <value>The size at which to begin the return-to-river animation.</value>
        internal Size AnimateBackFromSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is animating to its contact orientation.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is animating to its contact orientation; otherwise, <c>false</c>.
        /// </value>
        internal bool IsAnimatingToContactOrientation { get; set; }

        /// <summary>
        /// Gets or sets the time at which the orient-to-contact animation began.
        /// </summary>
        /// <value>The time at which the orient-to-contact animation began.</value>
        internal DateTime AnimateToContactOrientationBeginTime { get; set; }

        /// <summary>
        /// Gets or sets the timer which is used to determine when an element in the river has gone idle.
        /// </summary>
        /// <value>The timer which is used to determine when an element in the river has gone idle.</value>
        internal DispatcherTimer IdleTimer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is removed from the river.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is removed from the river; otherwise, <c>false</c>.
        /// </value>
        internal bool IsRemovedFromRiver { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the river attempted to add an item to the stream, but it rejected it by returning false from
        /// RiverItemBase.GetData. If so, the river won't attempt to use that item again until it's looped around.
        /// </summary>
        /// <value>
        /// <c>true</c> if the item rejected the attempt to add to the river; otherwise, <c>false</c>.
        /// </value>
        internal bool DidAttemptToAddToRiver { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is animating to its removed size.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is animating to its removed size; otherwise, <c>false</c>.
        /// </value>
        internal bool IsAnimatingToRemovedSize { get; set; }

        /// <summary>
        /// Gets or sets the size to which the item should animate to when it's been removed from the river.
        /// </summary>
        /// <value>The size to which the item should animate to when it's been removed from the river.</value>
        internal Size RemovedSize { get; set; }

        /// <summary>
        /// Gets or sets the minimum size of the item.
        /// </summary>
        /// <value>The minimum size of the item.</value>
        internal Size MinSize { get; set; }

        /// <summary>
        /// Gets or sets the time at which the animation of the size of an item after it's been removed from the river began.
        /// </summary>
        /// <value>The time at which the animation of the size of an item after it's been removed from the river began.</value>
        internal DateTime AnimateToRemovedSizeBeginTime { get; set; }

        /// <summary>
        /// Gets or sets the ScatterViewItem.
        /// </summary>
        /// <value>The ScatterViewItem.</value>
        internal ScatterViewItem Svi { get; set; }
    }
}
