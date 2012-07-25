using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Input.Manipulations;
using System.Windows.Interactivity;
using Microsoft.Surface.Presentation.Controls;

namespace SocialStream.Helpers
{
    /// <summary>
    /// A behavior to be applied to a SurfaceScrollViewer which is nested inside of a ScatterViewItem to override
    /// the SurfaceScrollViewer behavior of capturing all gestures, even those which might have been intended to
    /// translate, scale, or rotate the parent ScatterViewItem. With this behavior attached, the SurfaceScrollViewer
    /// will still scroll as usual, but gestures which are clearly not scrolls will be passed to the parent
    /// ScatterViewItem.
    /// </summary>
    public class ScrollViewerInScatterViewItemBehavior : Behavior<SurfaceScrollViewer>
    {
        /// <summary>
        /// A ManipulationProcessor to track the contacts on the SurfaceScrollViewer.
        /// </summary>
        private ManipulationProcessor2D _manipulationProcessor;

        /// <summary>
        /// The parent ScatterViewItem.
        /// </summary>
        private ScatterViewItem _scatterViewItem;

        /// <summary>
        /// Called after the behavior is attached to an AssociatedObject.
        /// </summary>
        /// <remarks>Override this to hook up functionality to the AssociatedObject.</remarks>
        protected override void OnAttached()
        {
            _manipulationProcessor = new ManipulationProcessor2D(Manipulations2D.Rotate | Manipulations2D.Scale | Manipulations2D.TranslateX | Manipulations2D.TranslateY);
            _manipulationProcessor.Delta += ManipulationProcessor_Delta;

            AssociatedObject.PreviewTouchDown += ScrollViewerTouchHandler;
            AssociatedObject.PreviewTouchMove += ScrollViewerTouchHandler;
            AssociatedObject.PreviewTouchUp += ScrollViewerTouchHandler;
        }

        /// <summary>
        /// Handles contact events on the ScrollViewer. Keeps the ManipulationProcessor tracking any contacts on the ScrollViewer.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="System.Windows.Input.TouchEventArgs"/> instance containing the event data.</param>
        private void ScrollViewerTouchHandler(object sender, TouchEventArgs e)
        {
            List<Manipulator2D> manipulators = new List<Manipulator2D>();
            foreach (TouchDevice touch in AssociatedObject.TouchesOver)
            {
                Point position = touch.GetTouchPoint(AssociatedObject).Position;
                manipulators.Add(new Manipulator2D(touch.Id, (float)position.X, (float)position.Y));
            }

            _manipulationProcessor.ProcessManipulators(DateTime.UtcNow.Ticks, manipulators);
        }

        /// <summary>
        /// Handles the Affine2DManipulationDelta event of the ManipulationProcessor. Attempts to distinguish scroll gestures
        /// from those intended to manipulate the parent ScatterViewItem.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.Manipulation2DDeltaEventArgs"/> instance containing the event data.</param>
        private void ManipulationProcessor_Delta(object sender, Manipulation2DDeltaEventArgs e)
        {
            if (_scatterViewItem == null)
            {
                // Hook up an event to the parent SVI. When it loses a contact, the ScrollViewer should capture them again.
                _scatterViewItem = AssociatedObject.FindVisualParent<ScatterViewItem>();
                _scatterViewItem.PreviewTouchUp += ScatterViewItem_PreviewContactUp;
            }

            if (_scatterViewItem == null)
            {
                // There's no SVI, so don't do anything.
                return;
            }

            if (e.Delta.ExpansionX != 0 || e.Delta.ExpansionY != 0 || e.Delta.Rotation != 0)
            {
                // If any scale or rotation is detected, pass that to the SVI.
                GiveControlToScatterViewItem();
                return;
            }

            Vector translation = new Vector((double)e.Cumulative.TranslationX, (double)e.Cumulative.TranslationY);

            if (Math.Abs(translation.Length) < 10)
            {
                // There's no translation, so don't do anything.
                return;
            }

            if (AssociatedObject.ComputedHorizontalScrollBarVisibility == Visibility.Collapsed && AssociatedObject.ComputedVerticalScrollBarVisibility == Visibility.Collapsed)
            {
                // There are no scrollbars, so pass all translations to the SVI.
                GiveControlToScatterViewItem();
                return;
            }

            if (translation.Length >= SensitivityDistance)
            {
                // The scroll has gone for some distance, so assume the gesture is intended as a scroll and never do a drag.
                _manipulationProcessor.CompleteManipulation(DateTime.UtcNow.Ticks);
                return;
            }

            double translationAngle = Math.Abs(Math.Atan2(e.Cumulative.TranslationX, e.Cumulative.TranslationY) * (180 / Math.PI));
            if (AssociatedObject.ComputedVerticalScrollBarVisibility == Visibility.Visible && AssociatedObject.ComputedHorizontalScrollBarVisibility == Visibility.Collapsed)
            {
                // The ScrollViewer is scrolling vertically.
                if (translationAngle >= 90 - SensitivityAngle && translationAngle <= 90 + SensitivityAngle)
                {
                    // The gesture scrolled horizontally, so pass that to the SVI.
                    GiveControlToScatterViewItem();
                }
            }
            else if (AssociatedObject.ComputedVerticalScrollBarVisibility == Visibility.Collapsed && AssociatedObject.ComputedHorizontalScrollBarVisibility == Visibility.Visible)
            {
                // The ScrollViewer is scrolling horizontally.
                if (e.Cumulative.TranslationX < 0)
                {
                    // The gesture is scrolling to the left.
                    if (180 - translationAngle < SensitivityAngle)
                    {
                        // But it's close enough to vertical that it should go to the SVI.
                        GiveControlToScatterViewItem();
                    }
                }
                else if (e.Cumulative.TranslationX > 0)
                {
                    // The gesture is scrolling to the right.
                    if (translationAngle <= SensitivityAngle)
                    {
                        // But it's close enough to vertical that it should go to the SVI.
                        GiveControlToScatterViewItem();
                    }
                }
            }
        }

        /// <summary>
        /// Gives the control to ScatterViewItem.
        /// </summary>
        private void GiveControlToScatterViewItem()
        {
            AssociatedObject.TouchesCaptured.ToList().ForEach(c =>
            {
                _scatterViewItem.CaptureTouch(c);
            });

            _manipulationProcessor.CompleteManipulation(DateTime.UtcNow.Ticks);
        }

        /// <summary>
        /// Handles the PreviewContactUp event of the ScatterViewItem control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.TouchEventArgs"/> instance containing the event data.</param>
        private void ScatterViewItem_PreviewContactUp(object sender, TouchEventArgs e)
        {
            AssociatedObject.TouchesOver.ToList().ForEach(c => AssociatedObject.CaptureTouch(c));
        }

        /// <summary>
        /// Called when the behavior is being detached from its AssociatedObject, but before it has actually occurred.
        /// </summary>
        /// <remarks>Override this to unhook functionality from the AssociatedObject.</remarks>
        protected override void OnDetaching()
        {
            AssociatedObject.PreviewTouchDown -= ScrollViewerTouchHandler;
            AssociatedObject.PreviewTouchUp -= ScrollViewerTouchHandler;

            if (_scatterViewItem != null)
            {
                _scatterViewItem.PreviewTouchUp -= ScatterViewItem_PreviewContactUp;
                _scatterViewItem = null;
            }

            _manipulationProcessor.Delta -= ManipulationProcessor_Delta;
            _manipulationProcessor = null;

            base.OnDetaching();
        }

        #region SensitivityAngle

        /// <summary>
        /// Gets or sets the SensitivityAngle of the behavior in degrees. A lower value means the behavior will pass contacts to the ScatterViewItem more often.
        /// </summary>
        /// <value>The SensitivityAngle of the behavior in degrees. A lower value means the behavior will pass contacts to the ScatterViewItem more often.</value>
        public double SensitivityAngle
        {
            get { return (double)GetValue(SensitivityAngleProperty); }
            set { SetValue(SensitivityAngleProperty, value); }
        }

        /// <summary>
        /// The identifier for the SensitivityAngle dependency property.
        /// </summary>
        public static readonly DependencyProperty SensitivityAngleProperty = DependencyProperty.Register("SensitivityAngle", typeof(double), typeof(ScrollViewerInScatterViewItemBehavior), new PropertyMetadata(30.0));

        #endregion

        #region SensitivityDistance

        /// <summary>
        /// Gets or sets the distance after which a manipulation will not be converted to a drag.
        /// </summary>
        /// <value>The distance after which a manipulation will not be converted to a drag.</value>
        public double SensitivityDistance
        {
            get { return (double)GetValue(SensitivityDistanceProperty); }
            set { SetValue(SensitivityDistanceProperty, value); }
        }

        /// <summary>
        /// The identifier for the SensitivityDistance dependency property.
        /// </summary>
        public static readonly DependencyProperty SensitivityDistanceProperty = DependencyProperty.Register("SensitivityDistance", typeof(double), typeof(ScrollViewerInScatterViewItemBehavior), new PropertyMetadata(30.0));

        #endregion
    }
}
