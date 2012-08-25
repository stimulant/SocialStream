using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to remove a query from the FacebookQueries list
    /// </summary>
    internal static class RemoveFacebookQueryCommand
    {
        /// <summary>
        /// Removes the specified item from FacebookQueries
        /// </summary>
        /// <param name="query">The query.</param>
        internal static void Execute(BindableStringVO query)
        {
            AppState.Instance.FacebookQueries.Remove(query);
        }
    }
}
