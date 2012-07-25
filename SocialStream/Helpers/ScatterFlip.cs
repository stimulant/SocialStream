using System.Windows;

namespace SocialStream.Helpers
{
    /// <summary>
    /// Provides a "Flipped" attached property, used on flipping ScatterViewItems.
    /// </summary>
    public static class ScatterFlip
    {
        #region IsFlipped

        /// <summary>
        /// Gets the value of the IsFlipped attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the IsFlipped attached property.</returns>
        public static bool GetIsFlipped(DependencyObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            return (bool)obj.GetValue(IsFlippedProperty);
        }

        /// <summary>
        /// Sets the value of the IsFlipped attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the IsFlipped attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetIsFlipped(DependencyObject obj, bool value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(IsFlippedProperty, value);
        }

        /// <summary>
        /// Identifies the IsFlipped attached property.
        /// </summary>
        public static readonly DependencyProperty IsFlippedProperty = DependencyProperty.RegisterAttached("IsFlipped", typeof(bool), typeof(ScatterFlip), new UIPropertyMetadata(false));

        #endregion
    }
}
