using System.Windows;
using SocialStream.ConfigTool.Model;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to save settings to disk.
    /// </summary>
    internal static class SaveCommand
    {
        /// <summary>
        /// Save settings to disk.
        /// </summary>
        internal static void Execute()
        {
            AppState.Instance.Save();
            Application.Current.Shutdown();
        }
    }
}
