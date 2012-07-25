using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SocialStream.Converters
{
    /// <summary>
    /// Convert most values to a boolean. Pass any parameter to flip the behavior.
    /// </summary>
    public class BooleanConverter : IValueConverter
    {
        /// <summary>
        /// Use a VisibilityConverter to do the actual conversion.
        /// </summary>
        private static VisibilityConverter _visibilityConverter;

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (_visibilityConverter == null)
            {
                _visibilityConverter = new VisibilityConverter();
            }

            return (Visibility)_visibilityConverter.Convert(value, targetType, parameter, culture) == Visibility.Visible;
        }

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns null, the valid null value is used.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
