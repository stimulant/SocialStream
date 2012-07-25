using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace SocialStream.Controls
{
    /// <summary>
    /// Interaction logic for AdminIndicatorLight.xaml
    /// </summary>
    public partial class AdminIndicatorLight : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdminIndicatorLight"/> class.
        /// </summary>
        public AdminIndicatorLight()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            UpdateIsOn();
        }

        #region IsOn

        /// <summary>
        /// Gets or sets a value indicating whether this instance is on.
        /// </summary>
        /// <value><c>true</c> if this instance is on; otherwise, <c>false</c>.</value>
        public bool IsOn
        {
            get { return (bool)GetValue(IsOnProperty); }
            set { SetValue(IsOnProperty, value); }
        }

        /// <summary>
        /// Identifies the IsOn dependency property.
        /// </summary>
        public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register("IsOn", typeof(bool), typeof(AdminIndicatorLight), new PropertyMetadata(false, (sender, e) => (sender as AdminIndicatorLight).UpdateIsOn()));

        /// <summary>
        /// Updates the visual state when IsOn changes.
        /// </summary>
        private void UpdateIsOn()
        {
            VisualStateManager.GoToState(this, IsOn ? OnState.Name : OffState.Name, true);
        }

        #endregion
    }
}
