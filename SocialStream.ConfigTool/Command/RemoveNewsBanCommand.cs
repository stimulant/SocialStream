using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to remove a query from the NewsBans list
    /// </summary>
    internal static class RemoveNewsBanCommand
    {
        /// <summary>
        /// Removes the specified item from NewsBans
        /// </summary>
        /// <param name="query">The query.</param>
        internal static void Execute(BindableStringVO query)
        {
            AppState.Instance.NewsBans.Remove(query);
        }
    }
}
