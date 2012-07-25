using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SocialStream.ConfigTool.Command;
using SocialStream.ConfigTool.Helpers;

namespace SocialStream.ConfigTool.Controls
{
    /// <summary>
    /// Interaction logic for ConfigInterface.xaml
    /// </summary>
    public partial class ConfigInterface : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigInterface"/> class.
        /// </summary>
        public ConfigInterface()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Click event of the defaults button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Defaults_Click(object sender, RoutedEventArgs e)
        {
            LoadDefaultsCommand.Execute();
        }

        /// <summary>
        /// Handles the Click event of the Cancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            CancelCommand.Execute();
        }

        /// <summary>
        /// Handles the Click event of the Save control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            int errors = 0;
            foreach (TabItem tab in _Editors.Items)
            {
                errors += (tab.Content as UserControl)
                    .GetVisualOfBaseType<FrameworkElement>()
                    .Where(element => Validation.GetHasError(element))
                    .Count();
            }

            if (errors != 0)
            {
                MessageBox.Show(SocialStream.ConfigTool.Properties.Resources.SaveError);
                return;
            }

            SaveCommand.Execute();
        }

        /// <summary>
        /// Handles the SelectionChanged event of the TabControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Controls.SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.OriginalSource is TabControl))
            {
                // Comboboxes and items in the editors can trigger this.
                return;
            }

            if (e.RemovedItems.Count == 0)
            {
                return;
            }

            TabItem item = e.RemovedItems[0] as TabItem;

            int errors = (item.Content as UserControl)
                .GetVisualOfBaseType<FrameworkElement>()
                .Where(element => Validation.GetHasError(element))
                .Count();

            SetEditorIsValid(item, errors == 0);
        }

        #region EditorIsValid

        /// <summary>
        /// Gets the value of the EditorIsValid attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the EditorIsValid attached property.</returns>
        public static bool GetEditorIsValid(DependencyObject obj)
        {
            return (bool)obj.GetValue(EditorIsValidProperty);
        }

        /// <summary>
        /// Sets the value of the EditorIsValid attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the EditorIsValid attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetEditorIsValid(DependencyObject obj, bool value)
        {
            obj.SetValue(EditorIsValidProperty, value);
        }

        /// <summary>
        /// Identifies the EditorIsValid attached property.
        /// </summary>
        public static readonly DependencyProperty EditorIsValidProperty = DependencyProperty.RegisterAttached("EditorIsValid", typeof(bool), typeof(ConfigInterface), new UIPropertyMetadata(true));

        #endregion
    }
}
