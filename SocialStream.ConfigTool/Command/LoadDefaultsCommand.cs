using System.Windows;
using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.Properties;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to load default config values.
    /// </summary>
    internal static class LoadDefaultsCommand
    {
        /// <summary>
        /// Loads the default config values.
        /// </summary>
        internal static void Execute()
        {
            if (MessageBox.Show(Resources.DefaultsConfirmation, string.Empty, MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                AppState.Instance.LoadDefaults();
            }
        }
    }
}
