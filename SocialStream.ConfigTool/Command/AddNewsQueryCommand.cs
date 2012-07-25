using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to add a query to the NewsQueries list
    /// </summary>
    internal static class AddNewsQueryCommand
    {
        /// <summary>
        /// Adds a blank query to NewsQueries
        /// </summary>
        internal static void Execute()
        {
            AppState.Instance.NewsQueries.Add(new BindableStringVO());
        }
    }
}
