using System.Globalization;
using Microsoft.Win32;

namespace SocialStream.Helpers
{
    /// <summary>
    /// Helper properties for getting info about the current Surface environment.
    /// </summary>
    internal static class SurfaceState
    {
        /// <summary>
        /// The key which stores whether or not the system is running in single application mode.
        /// </summary>
        private const string ShellRegKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Surface\v1.0\Shell";

        /// <summary>
        /// Gets a value indicating whether the app is running in Single Application Mode.
        /// </summary>
        /// <value>
        /// <c>true</c> if the app is running in Single Application Mode; otherwise, <c>false</c>.
        /// </value>
        public static bool IsInSingleAppMode
        {
            get
            {
                bool isInSingleAppMode = false;
                object singleAppMode = Registry.GetValue(ShellRegKey, "SingleAppMode", null);
                if (singleAppMode != null)
                {
                    isInSingleAppMode = int.Parse(singleAppMode.ToString(), CultureInfo.InvariantCulture) == 1;
                }

                return isInSingleAppMode;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the Surface unit is in User Mode.
        /// </summary>
        /// <value>
        /// <c>true</c> if the Surface unit is in User Mode; otherwise, <c>false</c>.
        /// </value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Useful as an example.")]
        public static bool IsInUserMode
        {
            get
            {
                bool isInUserMode = false;
                object userMode = Registry.GetValue(ShellRegKey, "TableMode", null);
                if (userMode != null)
                {
                    isInUserMode = int.Parse(userMode.ToString(), CultureInfo.InvariantCulture) == 2;
                }

                return isInUserMode;
            }
        }
    }
}
