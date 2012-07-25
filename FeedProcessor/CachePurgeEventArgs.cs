using System;
using System.Collections.ObjectModel;

namespace FeedProcessor
{
    /// <summary>
    /// The EventArgs object passed along with the Processor.CachePurged event.
    /// </summary>
    public class CachePurgeEventArgs : EventArgs
    {
        /// <summary>
        /// Backing store for ValidData
        /// </summary>
        private readonly ReadOnlyCollection<object> _validData;

        /// <summary>
        /// Gets the data remaining in the processor's cache.
        /// </summary>
        public ReadOnlyCollection<object> ValidData
        {
            get { return _validData; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CachePurgeEventArgs"/> class.
        /// </summary>
        /// <param name="validData">The data remaining in the processor's cache.</param>
        public CachePurgeEventArgs(ReadOnlyCollection<object> validData)
            : base()
        {
            _validData = validData;
        }
    }
}
