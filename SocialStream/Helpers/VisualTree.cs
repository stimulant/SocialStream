using System.Windows;
using System.Windows.Media;

namespace SocialStream.Helpers
{
    /// <summary>
    /// Helper methods to find a visual child or parent of a given type.
    /// </summary>
    public static class VisualTree
    {
        /// <summary>
        /// Finds a visual child of a given type.
        /// http://msdn.microsoft.com/en-us/library/bb613579.aspx
        /// </summary>
        /// <typeparam name="T">The type to search for.</typeparam>
        /// <param name="obj">The object at the root of the tree to search.</param>
        /// <returns>The visual child.</returns>
        public static T FindVisualChild<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                {
                    return (T)child;
                }
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a visual parent of a given type.
        /// http://msdn.microsoft.com/en-us/library/bb613579.aspx
        /// </summary>
        /// <typeparam name="T">The type to search for.</typeparam>
        /// <param name="obj">The object at the root of the tree to search.</param>
        /// <returns>The visual parent.</returns>
        public static T FindVisualParent<T>(this DependencyObject obj)
            where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(obj);
            while (parent != null)
            {
                T typed = parent as T;
                if (typed != null)
                {
                    return typed;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
    }
}
