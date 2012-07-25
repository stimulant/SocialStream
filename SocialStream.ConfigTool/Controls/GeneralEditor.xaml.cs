using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SocialStream.ConfigTool.Command;

namespace SocialStream.ConfigTool.Controls
{
    /// <summary>
    /// Interaction logic for GeneralEditor.xaml
    /// </summary>
    public partial class GeneralEditor : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralEditor"/> class.
        /// </summary>
        public GeneralEditor()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the Checked event of the Radio control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void Radio_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton radio = sender as RadioButton;
            if (!radio.IsLoaded)
            {
                return;
            }

            SetEnableContentResizingCommand.Execute(bool.Parse(radio.Tag.ToString()));
        }
    }
}
