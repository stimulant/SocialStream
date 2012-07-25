using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to add an item to the TwitterBans list
    /// </summary>
    internal static class AddTwitterBanCommand
    {
        /// <summary>
        /// Adds a new blank item to TwitterBans
        /// </summary>
        internal static void Execute()
        {
            AppState.Instance.TwitterBans.Add(new BindableStringVO());
        }
    }
}
