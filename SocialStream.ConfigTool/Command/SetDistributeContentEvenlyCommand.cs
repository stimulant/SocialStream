using SocialStream.ConfigTool.Model;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to set whether or not to distribute content evenly.
    /// </summary>
    internal static class SetDistributeContentEvenlyCommand
    {
        /// <summary>
        /// Sets the DistributeContentEvenly property.
        /// </summary>
        /// <param name="distributeContentEvenly">if set to <c>true</c> [distribute content evenly].</param>
        internal static void Execute(bool distributeContentEvenly)
        {
            AppState.Instance.DistributeContentEvenly = distributeContentEvenly;
        }
    }
}
