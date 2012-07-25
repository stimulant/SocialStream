using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SocialStream.Controls
{
    /// <summary>
    /// An object which is shown in the river, but replaced with a ScatterViewItem when removed.
    /// </summary>
    public partial class ItemProxy : Control
    {
        /// <summary>
        /// Private reference to a rotate transform used to orient this item.
        /// </summary>
        private RotateTransform _rotation;

        /// <summary>
        /// Private reference to a translate transform to position this item.
        /// </summary>
        private TranslateTransform _translation;

        /// <summary>
        /// Initializes a new instance of the <see cref="ItemProxy"/> class.
        /// </summary>
        public ItemProxy()
        {
            RenderTransformOrigin = new Point(.5, .5);

            _rotation = new RotateTransform();
            _translation = new TranslateTransform();
            RenderTransform = new TransformGroup
            {
                Children = new TransformCollection { _rotation, _translation }
            };

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                SizeChanged += (sender, e) =>
                {
                    UpdateCenter();
                    UpdateOrientation();
                };
            }
        }

        #region CanMove

        /// <summary>
        /// Gets or sets a value indicating whether this instance can move.
        /// </summary>
        /// <value><c>true</c> if this instance can move; otherwise, <c>false</c>.</value>
        public bool CanMove
        {
            get { return (bool)GetValue(CanMoveProperty); }
            set { SetValue(CanMoveProperty, value); }
        }

        /// <summary>
        /// The identifier for the CanMove dependency property.
        /// </summary>
        public static readonly DependencyProperty CanMoveProperty = DependencyProperty.Register("CanMove", typeof(bool), typeof(ItemProxy), new PropertyMetadata(false));

        #endregion

        #region Center

        /// <summary>
        /// Gets or sets the center.
        /// </summary>
        /// <value>The center.</value>
        public Point Center
        {
            get { return (Point)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        /// <summary>
        /// Identifier for the Center dependency property.
        /// </summary>
        public static readonly DependencyProperty CenterProperty = DependencyProperty.Register("Center", typeof(Point), typeof(ItemProxy), new PropertyMetadata(new Point(), (sender, e) => (sender as ItemProxy).UpdateCenter()));

        /// <summary>
        /// Updates the center.
        /// </summary>
        private void UpdateCenter()
        {
            _translation.X = Center.X - (ActualWidth / 2);
            _translation.Y = Center.Y - (ActualHeight / 2);
        }

        #endregion

        #region Orientation

        /// <summary>
        /// Gets or sets the orientation.
        /// </summary>
        /// <value>The orientation.</value>
        public double Orientation
        {
            get { return (double)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        /// <summary>
        /// Identifier for the Orientation dependency property.
        /// </summary>
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register("Orientation", typeof(double), typeof(ItemProxy), new PropertyMetadata(0.0, (sender, e) => (sender as ItemProxy).UpdateOrientation()));

        /// <summary>
        /// Updates the orientation.
        /// </summary>
        private void UpdateOrientation()
        {
            _rotation.Angle = Orientation;
        }

        #endregion
    }
}
