
namespace SocialStream.Controls
{
    /// <summary>
    /// Describes the state of a river item in the past, allowing that state to be restored when the user scrolls the river.
    /// </summary>
    internal class RiverItemHistoryInfo
    {
        /// <summary>
        /// Gets or sets the river item definition, which defines the size and position of the element in the river.
        /// </summary>
        /// <value>The river item definition, which defines the size and position of the element in the river.</value>
        public RiverItemState State { get; set; }

        /// <summary>
        /// Gets or sets a value indicating how many loops that the river's grid had made at this point in the history.
        /// </summary>
        /// <value>The value indicating how many loops that the river's grid had made at this point in the history.</value>
        public int Grid { get; set; }

        /// <summary>
        /// Gets or sets the data that the river item was rendering on this page.
        /// </summary>
        /// <value>The data that the river item was rendering on this page.</value>
        public object Data { get; set; }

        /// <summary>
        /// Gets or sets the orientation that the river item was rotated to on this page.
        /// </summary>
        /// <value>The orientation that the river item was rotated to on this page.</value>
        public double Orientation { get; set; }
    }
}
