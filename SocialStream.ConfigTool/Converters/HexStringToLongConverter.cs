using System;
using System.Globalization;
using System.Windows.Data;

namespace SocialStream.ConfigTool.Converters
{
    /// <summary>
    /// Convert between a hex string and a long.
    /// </summary>
    public class HexStringToLongConverter : IValueConverter
    {
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
            try
            {
                return ((long)value).ToString("X", CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new NotSupportedException(Properties.Resources.HexError);
            }
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
            if (value == null)
            {
                throw new NotSupportedException(Properties.Resources.HexError);
            }

            try
            {
                return long.Parse(value.ToString(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }
            catch
            {
                throw new NotSupportedException(Properties.Resources.HexError);
            }
        }
    }
}
