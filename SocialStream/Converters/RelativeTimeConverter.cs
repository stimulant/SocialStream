using System;
using System.Globalization;
using System.Windows.Data;
using SocialStream.Properties;

namespace SocialStream.Converters
{
    /// <summary>
    /// Given a DateTime, return a string representing its relative value, like "10 minutes ago".
    /// </summary>
    public class RelativeTimeConverter : IValueConverter
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
            if (!(value is DateTime))
            {
                return string.Empty;
            }

            DateTime date = (DateTime)value;
            TimeSpan diff = DateTime.Now - date;
            string suffix = string.Empty;
            int numeral = 0;

            if (diff.TotalDays >= 365)
            {
                numeral = (int)Math.Floor(diff.TotalDays / 365);
                suffix = numeral == 1 ? Resources.YearAgo : Resources.YearsAgo;
            }
            else if (diff.TotalDays >= 31)
            {
                numeral = (int)Math.Floor(diff.TotalDays / 31);
                suffix = numeral == 1 ? Resources.MonthAgo : Resources.MonthsAgo;
            }
            else if (diff.TotalDays >= 7)
            {
                numeral = (int)Math.Floor(diff.TotalDays / 7);
                suffix = numeral == 1 ? Resources.WeekAgo : Resources.WeeksAgo;
            }
            else if (diff.TotalDays >= 1)
            {
                numeral = (int)Math.Floor(diff.TotalDays);
                suffix = numeral == 1 ? Resources.DayAgo : Resources.DaysAgo;
            }
            else if (diff.TotalHours >= 1)
            {
                numeral = (int)Math.Floor(diff.TotalHours);
                suffix = numeral == 1 ? Resources.HourAgo : Resources.HoursAgo;
            }
            else if (diff.TotalMinutes >= 1)
            {
                numeral = (int)Math.Floor(diff.TotalMinutes);
                suffix = numeral == 1 ? Resources.MinuteAgo : Resources.MinutesAgo;
            }
            else if (diff.TotalSeconds >= 1)
            {
                numeral = (int)Math.Floor(diff.TotalSeconds);
                suffix = numeral == 1 ? Resources.SecondAgo : Resources.SecondsAgo;
            }
            else
            {
                suffix = Resources.JustNow;
            }

            string output = numeral == 0 ? suffix : string.Format(CultureInfo.InvariantCulture, "{0} {1}", numeral, suffix);
            return output;
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
