using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to add an item to the FlickrBans list
    /// </summary>
    internal static class AddFlickrBanCommand
    {
        /// <summary>
        /// Adds a new blank item to FlickrBans
        /// </summary>
        internal static void Execute()
        {
            AppState.Instance.FlickrBans.Add(new BindableStringVO());
        }
    }
}
