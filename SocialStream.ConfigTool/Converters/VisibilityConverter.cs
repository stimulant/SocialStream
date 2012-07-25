// -------------------------------------------------------------
// <copyright file="VisibilityConverter.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation 2011. All rights reserved.
// </copyright>
// -------------------------------------------------------------

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SocialStream.ConfigTool.Converters
{
    /// <summary>
    /// Convert non-empty strings, true bools, and numbers above zero to visible. Everything else to collapsed. Pass any parameter to reverse the behavior.
    /// </summary>
    public class VisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Modifies the source data before passing it to the target for display in the UI.
        /// </summary>
        /// <param name="value">The source data being passed to the target.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the target dependency property.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the target dependency property.
        /// </returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily", Justification = "Suppressed to maintain legibility.")]
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool converted =
                (value is string && !string.IsNullOrEmpty((string)value)) ||
                (value is int && (int)value > 0) ||
                (value is uint && (uint)value > 0) ||
                (value is double && (double)value > 0) ||
                (value is bool && (bool)value == true) ||
                (value is ICollection && (value as ICollection).Count > 0) ||
                (!(value is string) && !(value is int) && !(value is uint) && !(value is double) && !(value is bool) && !(value is ICollection) && value != null);

            if (parameter != null)
            {
                converted = !converted;
            }

            return converted ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Modifies the target data before passing it to the source object.  This method is called only in <see cref="F:System.Windows.Data.BindingMode.TwoWay"/> bindings.
        /// </summary>
        /// <param name="value">The target data being passed to the source.</param>
        /// <param name="targetType">The <see cref="T:System.Type"/> of data expected by the source object.</param>
        /// <param name="parameter">An optional parameter to be used in the converter logic.</param>
        /// <param name="culture">The culture of the conversion.</param>
        /// <returns>
        /// The value to be passed to the source object.
        /// </returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
