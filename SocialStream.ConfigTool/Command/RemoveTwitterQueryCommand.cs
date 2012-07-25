using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to remove a query from the TwitterQueries list
    /// </summary>
    internal static class RemoveTwitterQueryCommand
    {
        /// <summary>
        /// Removes the specified item from TwitterQueries
        /// </summary>
        /// <param name="query">The query.</param>
        internal static void Execute(BindableStringVO query)
        {
            AppState.Instance.TwitterQueries.Remove(query);
        }
    }
}
