using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to add an item to the FacebookBans list
    /// </summary>
    internal static class AddFacebookBanCommand
    {
        /// <summary>
        /// Adds a new blank item to FacebookBans
        /// </summary>
        internal static void Execute()
        {
            AppState.Instance.FacebookBans.Add(new BindableStringVO());
        }
    }
}
