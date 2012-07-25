using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace SocialStream.Helpers
{
    /// <summary>
    /// Helper methods for enumerating the visual children of a given root element.
    /// </summary>
    public static class VisualEnumerable
    {
        /// <summary>
        /// Gets the Visual Tree filtered by Type for a DependencyObject with that DependencyObject as the root.
        /// http://petermcg.wordpress.com/2009/03/04/linq-to-visual-tree-beta/
        /// </summary>
        /// <typeparam name="T">The type to search for.</typeparam>
        /// <param name="element">The root element of the search.</param>
        /// <returns>The search results.</returns>
        public static IEnumerable<T> GetVisualOfType<T>(this DependencyObject element)
        {
            return GetVisualTree(element).Where(t => t.GetType() == typeof(T) || t.GetType().IsSubclassOf(typeof(T))).Cast<T>();
        }

        /// <summary>
        /// Gets the Visual Tree for a DependencyObject with that DependencyObject as the root.
        /// http://petermcg.wordpress.com/2009/03/04/linq-to-visual-tree-beta/
        /// </summary>
        /// <param name="element">The root element of the search.</param>
        /// <returns>The search results.</returns>
        public static IEnumerable<DependencyObject> GetVisualTree(this DependencyObject element)
        {
            int childrenCount = VisualTreeHelper.GetChildrenCount(element);

            for (int i = 0; i < childrenCount; i++)
            {
                var visualChild = VisualTreeHelper.GetChild(element, i);

                yield return visualChild;

                foreach (var visualChildren in GetVisualTree(visualChild))
                {
                    yield return visualChildren;
                }
            }
        }
    }
}