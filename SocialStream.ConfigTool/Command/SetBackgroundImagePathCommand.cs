using Microsoft.Surface.Presentation;
using SocialStream.ConfigTool.Model;

namespace SocialStream.ConfigTool.Command
{
    /// <summary>
    /// Called to set the path of one of the background images.
    /// </summary>
    internal static class SetBackgroundImagePathCommand
    {
        /// <summary>
        /// Sets the path of one of the background images.
        /// </summary>
        /// <param name="orientation">The orientation.</param>
        /// <param name="path">The path to the image file.</param>
        internal static void Execute(Tilt orientation, string path)
        {
            if (orientation == Tilt.Vertical)
            {
                AppState.Instance.VerticalBackgroundPath = path;
            }
            else if (orientation == Tilt.Horizontal)
            {
                AppState.Instance.HorizontalBackgroundPath = path;
            }
        }
    }
}
