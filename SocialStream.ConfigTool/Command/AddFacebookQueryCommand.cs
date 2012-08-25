using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to add a query to the FacebookQueries list
    /// </summary>
    internal static class AddFacebookQueryCommand
    {
        /// <summary>
        /// Adds a blank query to FacebookQueries
        /// </summary>
        internal static void Execute()
        {
            AppState.Instance.FacebookQueries.Add(new BindableStringVO());
        }
    }
}
