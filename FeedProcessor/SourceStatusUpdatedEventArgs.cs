using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FeedProcessor
{
    /// <summary>
    /// The EventArgs object passed along with the Processor.SourceStatusUpdated event.
    /// </summary>
    internal class SourceStatusUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Whether or not the source is up.
        /// </summary>
        internal readonly bool IsSourceUp;

        /// <summary>
        /// Initializes a new instance of the <see cref="SourceStatusUpdatedEventArgs"/> class.
        /// </summary>
        /// <param name="isSourceUp">if set to <c>true</c>, the source is active.</param>
        internal SourceStatusUpdatedEventArgs(bool isSourceUp)
        {
            IsSourceUp = isSourceUp;
        }
    }
}
