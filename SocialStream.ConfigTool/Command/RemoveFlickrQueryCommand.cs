using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to remove a query from the FlickrQueries list
    /// </summary>
    internal static class RemoveFlickrQueryCommand
    {
        /// <summary>
        /// Removes the specified item from FlickrQueries
        /// </summary>
        /// <param name="query">The query.</param>
        internal static void Execute(BindableStringVO query)
        {
            AppState.Instance.FlickrQueries.Remove(query);
        }
    }
}
