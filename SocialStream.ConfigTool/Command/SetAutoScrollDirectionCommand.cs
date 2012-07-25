using SocialStream.ConfigTool.Model;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to set the AutoScrollDirection
    /// </summary>
    internal static class SetAutoScrollDirectionCommand
    {
        /// <summary>
        /// Sets the SetAutoScrollDirectionCommand property.
        /// </summary>
        /// <param name="Direction">The Direction.</param>
        internal static void Execute(int Direction)
        {
            AppState.Instance.AutoScrollDirection = Direction;
        }
    }
}
