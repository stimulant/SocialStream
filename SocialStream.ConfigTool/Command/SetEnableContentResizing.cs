using SocialStream.ConfigTool.Model;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to set whether to enable content resizing
    /// </summary>
    internal static class SetEnableContentResizingCommand
    {
        /// <summary>
        /// Sets the EnableContentResizing property.
        /// </summary>
        /// <param name="enableContentResizing">if set to <c>true</c> [enable content resizing].</param>
        internal static void Execute(bool enableContentResizing)
        {
            AppState.Instance.EnableContentResizing = enableContentResizing;
        }
    }
}
