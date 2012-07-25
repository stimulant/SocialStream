
namespace SocialStream.Controls
{
    /// <summary>
    /// Indicates whether an item should be visible, and whether or not it's in the looping part of the river. The looping part is the
    /// portion of the screen which is past the last column of the GridLayout.
    /// </summary>
    internal struct RiverItemVisibilityInfo
    {
        /// <summary>
        /// Gets or sets a value indicating whether this instance is visible.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is visible; otherwise, <c>false</c>.
        /// </value>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is looping.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is looping; otherwise, <c>false</c>.
        /// </value>
        public bool IsLooping { get; set; }
    }
}
