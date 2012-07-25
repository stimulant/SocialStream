using SocialStream.ConfigTool.Model;
using SocialStream.ConfigTool.VO;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to add an item to the NewsBans list
    /// </summary>
    internal static class AddNewsBanCommand
    {
        /// <summary>
        /// Adds a new blank item to NewsBans
        /// </summary>
        internal static void Execute()
        {
            AppState.Instance.NewsBans.Add(new BindableStringVO());
        }
    }
}
