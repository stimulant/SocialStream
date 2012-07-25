using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to add a query to the FlickQueries list
    /// </summary>
    internal static class AddFlickrQueryCommand
    {
        /// <summary>
        /// Adds a blank query to FlickrQueries
        /// </summary>
        internal static void Execute()
        {
            AppState.Instance.FlickrQueries.Add(new BindableStringVO());
        }
    }
}
