using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Blake.NUI.WPF.Touch
{
    /// <summary>
    /// From Blake.NUI.
    /// </summary>
    public class MouseTouchDevice : TouchDevice
    {
        #region Class Members

        private static MouseTouchDevice device;

        public Point Position { get; set; }

        #endregion

        #region Public Static Methods

        public static void RegisterEvents(UIElement root)
        {
            if (root == null)
            {
                return;
            }

            root.PreviewMouseDown += MouseDown;
            root.PreviewMouseMove += MouseMove;
            root.PreviewMouseUp += MouseUp;
        }

        #endregion

        #region Private Static Methods

        private static void MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (device != null &&
                device.IsActive)
            {
                device.ReportUp();
                device.Deactivate();
                device = null;
            }
            device = new MouseTouchDevice(e.MouseDevice.GetHashCode());
            device.SetActiveSource(e.MouseDevice.ActiveSource);
            device.Position = e.GetPosition(null);
            device.Activate();
            device.ReportDown();
            e.Handled = true;
        }

        private static void MouseMove(object sender, MouseEventArgs e)
        {
            if (device != null &&
                device.IsActive)
            {
                device.Position = e.GetPosition(null);
                device.ReportMove();
                e.Handled = true;
            }
        }

        private static void MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (device != null &&
                device.IsActive)
            {
                device.Position = e.GetPosition(null);
                device.ReportUp();
                device.Deactivate();
                device = null;
                e.Handled = true;
            }
        }

        #endregion

        #region Constructors

        public MouseTouchDevice(int deviceId) :
            base(deviceId)
        {
            Position = new Point();
        }

        #endregion

        #region Overridden methods

        public override TouchPointCollection GetIntermediateTouchPoints(IInputElement relativeTo)
        {
            return new TouchPointCollection();
        }

        public override TouchPoint GetTouchPoint(IInputElement relativeTo)
        {
            Point point = Position;
            if (relativeTo != null)
            {
                try
                {
                    point = this.ActiveSource.RootVisual.TransformToDescendant((Visual)relativeTo).Transform(Position);
                }
                catch
                {
                }
            }

            Rect rect = new Rect(point, new Size(1, 1));

            return new TouchPoint(this, point, rect, TouchAction.Move);
        }

        #endregion

    }
}
