using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to add a query to the TwitterQueries list
    /// </summary>
    internal static class AddTwitterQueryCommand
    {
        /// <summary>
        /// Adds a blank query to TwitterQueries
        /// </summary>
        internal static void Execute()
        {
            AppState.Instance.TwitterQueries.Add(new BindableStringVO());
        }
    }
}
