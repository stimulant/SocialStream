using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to remove a query from the FacebookBans list
    /// </summary>
    internal static class RemoveFacebookBanCommand
    {
        /// <summary>
        /// Removes the specified item from FacebookBans
        /// </summary>
        /// <param name="query">The query.</param>
        internal static void Execute(BindableStringVO query)
        {
            AppState.Instance.FacebookBans.Remove(query);
        }
    }
}
