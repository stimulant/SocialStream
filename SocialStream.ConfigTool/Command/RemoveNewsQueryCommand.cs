using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to remove a query from the NewsQueries list
    /// </summary>
    internal static class RemoveNewsQueryCommand
    {
        /// <summary>
        /// Removes the specified item from NewsQueries
        /// </summary>
        /// <param name="query">The query.</param>
        internal static void Execute(BindableStringVO query)
        {
            AppState.Instance.NewsQueries.Remove(query);
        }
    }
}
