using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to remove a query from the TwitterBans list
    /// </summary>
    internal static class RemoveTwitterBanCommand
    {
        /// <summary>
        /// Removes the specified item from TwitterBans
        /// </summary>
        /// <param name="query">The query.</param>
        internal static void Execute(BindableStringVO query)
        {
            AppState.Instance.TwitterBans.Remove(query);
        }
    }
}
