using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace SocialStream.ConfigTool.Validators
{
    /// <summary>
    /// A validation rule for category namees.
    /// </summary>
    public class IsValidQueryStringRule : ValidationRule
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
                return new ValidationResult(false, SocialStream.ConfigTool.Properties.Resources.QueryStringEmpty);
            }

            if (val.Contains(','))
            {
                return new ValidationResult(false, SocialStream.ConfigTool.Properties.Resources.QueryStringComma);
            }

            return ValidationResult.ValidResult;
        }
    }
}
