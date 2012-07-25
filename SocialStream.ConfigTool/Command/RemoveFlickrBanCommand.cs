using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to remove a query from the FlickrBans list
    /// </summary>
    internal static class RemoveFlickrBanCommand
    {
        /// <summary>
        /// Removes the specified item from FlickrBans
        /// </summary>
        /// <param name="query">The query.</param>
        internal static void Execute(BindableStringVO query)
        {
            AppState.Instance.FlickrBans.Remove(query);
        }
    }
}
