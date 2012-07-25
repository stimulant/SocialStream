using SocialStream.ConfigTool.Model;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to set whether the profanity filter is enabled
    /// </summary>
    internal static class SetProfanityFilterCommand
    {
        /// <summary>
        /// Sets the IsSafeSearchEnabled property.
        /// </summary>
        /// <param name="isProfanityFilterEnabled">if set to <c>true</c> [is profanity filter enabled].</param>
        internal static void Execute(bool isProfanityFilterEnabled)
        {
            AppState.Instance.IsProfanityFilterEnabled = isProfanityFilterEnabled;
        }
    }
}
