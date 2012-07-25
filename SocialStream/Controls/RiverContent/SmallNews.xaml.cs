using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Surface.Presentation.Controls;

namespace SocialStream.Controls.RiverContent
{
    /// <summary>
    /// A river item which shows the small version of news feed items.
    /// </summary>
    public partial class SmallNews : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SmallNews"/> class.
        /// </summary>
        public SmallNews()
        {
            InitializeComponent();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            // Bind to the parent SVI to show a pressed state.
            Binding binding = new Binding()
            {
                Path = new PropertyPath(ScatterViewItem.IsContainerActiveProperty),
                RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(ScatterViewItem), 1)
            };

            BindingOperations.SetBinding(this, IsPressedProperty, binding);
        }

        #region IsPressed

        /// <summary>
        /// Gets or sets a value indicating whether this instance is pressed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is pressed; otherwise, <c>false</c>.
        /// </value>
        public bool IsPressed
        {
            get { return (bool)GetValue(IsPressedProperty); }
            set { SetValue(IsPressedProperty, value); }
        }

        /// <summary>
        /// Backing store for IsPressed.
        /// </summary>
        public static readonly DependencyProperty IsPressedProperty = DependencyProperty.Register("IsPressed", typeof(bool), typeof(SmallNews), new PropertyMetadata(false, IsPressedPropertyChanged));

        /// <summary>
        /// Fires when IsPressed changes.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private static void IsPressedPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as SmallNews).UpdateIsPressed();
        }

        /// <summary>
        /// Shows the pressed state.
        /// </summary>
        private void UpdateIsPressed()
        {
            VisualStateManager.GoToState(this, IsPressed ? IsPressedState.Name : NotPressedState.Name, true);
        }

        #endregion
    }
}
