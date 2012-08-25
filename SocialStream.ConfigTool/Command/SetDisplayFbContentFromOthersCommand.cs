using SocialStream.ConfigTool.Model;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to set whether to display content from other users on a facebook page
    /// </summary>
    internal static class SetDisplayFbContentFromOthersCommand
    {
        /// <summary>
        /// Sets the DisplayFbContentFromOthers property.
        /// </summary>
        /// <param name="displayFbContentFromOthers">if set to <c>true</c> [display fb content from others].</param>
        internal static void Execute(bool displayFbContentFromOthers)
        {
            AppState.Instance.DisplayFbContentFromOthers = displayFbContentFromOthers;
        }
    }
}
