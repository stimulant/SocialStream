using System.Windows;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called when the cancel button is clicked.
    /// </summary>
    internal static class CancelCommand
    {
        /// <summary>
        /// Exits the app.
        /// </summary>
        internal static void Execute()
        {
            Application.Current.Shutdown();
        }
    }
}
