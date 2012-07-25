using System;
using System.Globalization;
using System.Windows.Controls;

namespace SocialStream.ConfigTool.Validators
{
    /// <summary>
    /// A validation rule for a hex number.
    /// </summary>
    public class IsValidHexColorRule : ValidationRule
    {
        /// <summary>
        /// When overridden in a derived class, performs validation checks on a value.
        /// </summary>
        /// <param name="value">The value from the binding target to check.</param>
        /// <param name="cultureInfo">The culture to use in this rule.</param>
        /// <returns>
        /// A <see cref="T:System.Windows.Controls.ValidationResult"/> object.
        /// </returns>
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string val = value as string;

            if (val == null || string.IsNullOrWhiteSpace(val))
            {
                return new ValidationResult(false, SocialStream.ConfigTool.Properties.Resources.HexColorError);
            }

            if (val.Length < 9 || !val.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(false, SocialStream.ConfigTool.Properties.Resources.HexColorError);
            }

            long parsed;
            string numpart = val.Replace("#", string.Empty);
            if (long.TryParse(numpart, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed))
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, SocialStream.ConfigTool.Properties.Resources.HexColorError);
            }
        }
    }
}
