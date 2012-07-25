using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Blake.NUI.WPF.Touch;
using Microsoft.Surface.Presentation;
using Microsoft.Surface.Presentation.Controls;
using Microsoft.Surface.Presentation.Input;
using SocialStream.Helpers;

namespace SocialStream.Controls
{
    /// <summary>
    /// Presents a scrollable list of items in a "river" of ItemProxys that scrolls across the screen.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Sorry, this class is huge.")]
    public partial class River : UserControl
    {
        #region Private fields

        /// <summary>
        /// The number of rows in the grid.
        /// </summary>
        private int _totalRows;

        /// <summary>
        /// The number of columns in the grid.
        /// </summary>
        private int _totalColumns;

        /// <summary>
        /// The number of columns on screen now.
        /// </summary>
        private double _screenColumns;

        /// <summary>
        /// A random generator for various operations.
        /// </summary>
        private Random _random = new Random();

        /// <summary>
        /// The time at which the last frame was rendered, used to update the scroll position over time.
        /// </summary>
        private DateTime _lastFrameTime;

        /// <summary>
        /// If the scroll position is flicked in a direction opposite of the auto scroll speed, it will slow to a stop, then speed back up
        /// to the scroll speed. During this time, _isReversing is true.
        /// </summary>
        private bool _isReversing;

        /// <summary>
        /// The time at which reversing began.
        /// </summary>
        private DateTime _reversingStartTime;

        /// <summary>
        /// How long it should take to speed up to the scroll speed.
        /// </summary>
        private static double _reversingDuration = .5;

        /// <summary>
        /// A list of all the river item states.
        /// </summary>
        private List<RiverItemState> _states;

        /// <summary>
        /// A mapping between item definitions and ItemProxys, to facilitate quicker lookups.
        /// </summary>
        private Dictionary<RiverItemState, ItemProxy> _stateToProxyMap = new Dictionary<RiverItemState, ItemProxy>();

        /// <summary>
        /// A mapping between item definitions and ItemProxys, to facilitate quicker lookups.
        /// </summary>
        private Dictionary<ItemProxy, RiverItemState> _sviToProxyMap = new Dictionary<ItemProxy, RiverItemState>();

        /// <summary>
        /// A pool of ItemProxys of each style which are recycled when new items are needed in the river.
        /// </summary>
        private Dictionary<Style, Queue<ItemProxy>> _styledRiverItemPool = new Dictionary<Style, Queue<ItemProxy>>();

        /// <summary>
        /// Another pool of ItemProxys, used for items which use the default SVI style.
        /// </summary>
        private Queue<ItemProxy> _defaultRiverItemPool = new Queue<ItemProxy>();

        /// <summary>
        /// The amount of change in scroll since scrolling began.
        /// </summary>
        private double _cumulativeScrollChange;

        /// <summary>
        /// Whether or not the stream has been scrolled back by the user or if it's advanced on its own.
        /// </summary>
        private bool _isScrollingBack;

        /// <summary>
        /// The amount by which the river has been scrolled backward by the user.
        /// </summary>
        private double _scrollBack;

        /// <summary>
        /// Objects which represent the state of river items that aren't onscreen anymore.
        /// </summary>
        private List<RiverItemHistoryInfo> _history = new List<RiverItemHistoryInfo>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="River"/> class.
        /// </summary>
        public River()
        {
            InitializeComponent();

            SizeChanged += (sender, e) => UpdateGridCellSize();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return;
            }

            _lastFrameTime = DateTime.Now;

            IsManipulationEnabled = true;
            ManipulationDelta += River_ManipulationDelta;
            ManipulationInertiaStarting += River_ManipulationInertiaStarting;
        }

        #region Properties

        #region GridLayout

        /// <summary>
        /// Gets or sets the grid layout of the river. The ControlTemplate is expected to be a Grid full of elements, whose Row, Column,
        /// RowSpan, and ColumnSpan values are used to position them in the river. These items should use the River.ProxyStyle attached
        /// property to specify a ItemProxy style with which the item should be rendered.
        /// </summary>
        /// <value>The grid layout.</value>
        public ControlTemplate GridLayout
        {
            get { return (ControlTemplate)GetValue(GridLayoutProperty); }
            set { SetValue(GridLayoutProperty, value); }
        }

        /// <summary>
        /// Backing store for GridLayout.
        /// </summary>
        public static readonly DependencyProperty GridLayoutProperty = DependencyProperty.Register(
            "GridLayout",
            typeof(ControlTemplate),
            typeof(River),
            new PropertyMetadata(null, (sender, e) => (sender as River).UpdateGridLayout()));

        /// <summary>
        /// Updates the grid layout of the river.
        /// </summary>
        private void UpdateGridLayout()
        {
            CompositionTarget.Rendering -= CompositionTarget_Rendering;

            if (GridLayout == null)
            {
                return;
            }

            Grid layout = GridLayout.LoadContent() as Grid;

            // Copy the chilren in the grid to another list for easy access.
            _states = new List<RiverItemState>();
            _stateToProxyMap = new Dictionary<RiverItemState, ItemProxy>();
            _sviToProxyMap = new Dictionary<ItemProxy, RiverItemState>();

            foreach (UIElement child in layout.Children)
            {
                RiverItemState state = new RiverItemState
                {
                    Row = Grid.GetRow(child),
                    Column = Grid.GetColumn(child),
                    RowSpan = Grid.GetRowSpan(child),
                    ColumnSpan = Grid.GetColumnSpan(child),
                    ProxyStyle = GetProxyStyle(child),
                    ItemStyle = GetItemStyle(child),
                    RemovedSize = Size.Empty
                };

                _states.Add(state);
                _stateToProxyMap[state] = null;

                // Compute how many rows and columns have been specified.
                _totalRows = Math.Max(_totalRows, state.Row + state.RowSpan - 1);
                _totalColumns = Math.Max(_totalColumns, state.Column + state.ColumnSpan - 1);
            }

            _totalColumns++;
            _totalRows++;

            _scatterView.Items.Clear();
            _styledRiverItemPool.Clear();

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                Update();
                return;
            }

            // Begin rendering.
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        #endregion

        #region HorizontalOffset

        /// <summary>
        /// Gets or sets the amount by which the river has scrolled, in pixels.
        /// </summary>
        /// <value>The horizontal offset.</value>
        public double HorizontalOffset
        {
            get { return (double)GetValue(HorizontalOffsetProperty); }
            set { SetValue(HorizontalOffsetProperty, value); }
        }

        /// <summary>
        /// Backing store for HorizontalOffset.
        /// </summary>
        public static readonly DependencyProperty HorizontalOffsetProperty = DependencyProperty.Register(
            "HorizontalOffset",
            typeof(double),
            typeof(River),
            new FrameworkPropertyMetadata(0.0, null, CoerceHorizontalOffsetProperty));

        /// <summary>
        /// Coerces the horizontal offset property.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="baseValue">The base value.</param>
        /// <returns>A horizontal offset within the valid range.</returns>
        private static object CoerceHorizontalOffsetProperty(DependencyObject sender, object baseValue)
        {
            return (sender as River).CoerceHorizontalOffset((double)baseValue);
        }

        /// <summary>
        /// Coerces the horizontal offset.
        /// </summary>
        /// <param name="baseValue">The base value.</param>
        /// <returns>A horizontal offset within the valid range.</returns>
        private object CoerceHorizontalOffset(double baseValue)
        {
            double change = baseValue - HorizontalOffset;

            _cumulativeScrollChange += change;

            double max = _totalColumns * GridCellSize.Width;

            while (baseValue < max)
            {
                baseValue += max;
            }

            bool isForward = Math.Sign(change) == Math.Sign(AutoScrollSpeed);

            if (!isForward || (_isScrollingBack && isForward))
            {
                // The river is being scrolled backward, or it was scrolled backward and is still moving forward to where it was.
                _isScrollingBack = true;
                _scrollBack += change;

                bool isDoneScrollingBack = Math.Sign(_scrollBack) == Math.Sign(AutoScrollSpeed);
                if (isDoneScrollingBack)
                {
                    _scrollBack = 0;
                    _isScrollingBack = false;
                }
            }

            return baseValue % max;
        }

        #endregion

        #region GridCellSize

        /// <summary>
        /// Gets or sets the size of each grid cell.
        /// </summary>
        /// <value>The size of the grid cell.</value>
        public Size GridCellSize
        {
            get { return (Size)GetValue(GridCellSizeProperty); }
            set { SetValue(GridCellSizeProperty, value); }
        }

        /// <summary>
        /// Backing store for GridCellSize.
        /// </summary>
        public static readonly DependencyProperty GridCellSizeProperty = DependencyProperty.Register(
            "GridCellSize",
            typeof(Size),
            typeof(River),
            new PropertyMetadata(new Size(84, 84), (sender, e) => (sender as River).UpdateGridCellSize()));

        /// <summary>
        /// Updates the size of the grid cell.
        /// </summary>
        private void UpdateGridCellSize()
        {
            _screenColumns = (int)Math.Ceiling(ActualWidth / GridCellSize.Width);
        }

        #endregion

        #region ReturnToRiverSpeed

        /// <summary>
        /// Gets or sets the speed at which an item should return to the river, in pixels per second.
        /// </summary>
        /// <value>The the speed at which an item should return to the river, in pixels per second.</value>
        public double ReturnToRiverSpeed
        {
            get { return (double)GetValue(ReturnToRiverSpeedProperty); }
            set { SetValue(ReturnToRiverSpeedProperty, value); }
        }

        /// <summary>
        /// Backing store for ReturnToRiverSpeed.
        /// </summary>
        public static readonly DependencyProperty ReturnToRiverSpeedProperty = DependencyProperty.Register(
            "ReturnToRiverSpeed",
            typeof(double),
            typeof(River),
            new PropertyMetadata(1500.0));

        #endregion

        #region OrientSpeed

        /// <summary>
        /// Gets or sets the speed at which an item should orient to the contact that touched it, in degrees per second.
        /// </summary>
        /// <value>The the speed at which an item should orient to the contact that touched it, in degrees per second.</value>
        public double OrientSpeed
        {
            get { return (double)GetValue(OrientSpeedProperty); }
            set { SetValue(OrientSpeedProperty, value); }
        }

        /// <summary>
        /// Backing store for OrientSpeed.
        /// </summary>
        public static readonly DependencyProperty OrientSpeedProperty = DependencyProperty.Register(
            "OrientSpeed",
            typeof(double),
            typeof(River),
            new PropertyMetadata(720.0));

        #endregion

        #region MinimumTransitionDuration

        /// <summary>
        /// Gets or sets the minimum duration of the animations to contacts and the river.
        /// </summary>
        /// <value>The minimum duration of the animations to contacts and the river.</value>
        public TimeSpan MinimumTransitionDuration
        {
            get { return (TimeSpan)GetValue(MinimumTransitionDurationProperty); }
            set { SetValue(MinimumTransitionDurationProperty, value); }
        }

        /// <summary>
        /// Backing store for MinimumTransitionDuration.
        /// </summary>
        public static readonly DependencyProperty MinimumTransitionDurationProperty = DependencyProperty.Register(
            "MinimumTransitionDuration",
            typeof(TimeSpan),
            typeof(River),
            new PropertyMetadata(TimeSpan.FromMilliseconds(250)));

        #endregion

        #region IdleTimeoutFront

        /// <summary>
        /// Gets or sets the idle timeout. If an item in the river is not manipulated for this long, it will be returned back to the river.
        /// </summary>
        /// <value>The idle timeout.</value>
        public TimeSpan IdleTimeoutFront
        {
            get { return (TimeSpan)GetValue(IdleTimeoutFrontProperty); }
            set { SetValue(IdleTimeoutFrontProperty, value); }
        }

        /// <summary>
        /// Backing store for IdleTimeout.
        /// </summary>
        public static readonly DependencyProperty IdleTimeoutFrontProperty = DependencyProperty.Register(
            "IdleTimeoutFront",
            typeof(TimeSpan),
            typeof(River),
            new PropertyMetadata(TimeSpan.FromSeconds(3)));

        #endregion

        #region IdleTimeoutBack

        /// <summary>
        /// Gets or sets the idle timeout. If an item in the river is not manipulated for this long, it will be returned back to the river.
        /// </summary>
        /// <value>The idle timeout.</value>
        public TimeSpan IdleTimeoutBack
        {
            get { return (TimeSpan)GetValue(IdleTimeoutBackProperty); }
            set { SetValue(IdleTimeoutBackProperty, value); }
        }

        /// <summary>
        /// Backing store for IdleTimeout.
        /// </summary>
        public static readonly DependencyProperty IdleTimeoutBackProperty = DependencyProperty.Register(
            "IdleTimeoutBack",
            typeof(TimeSpan),
            typeof(River),
            new PropertyMetadata(TimeSpan.FromSeconds(3)));

        #endregion

        #region AutoScrollSpeed

        /// <summary>
        /// Gets or sets the default speed of the river, in pixels per second.
        /// </summary>
        public double AutoScrollSpeed
        {
            get { return (double)GetValue(AutoScrollSpeedProperty); }
            set { SetValue(AutoScrollSpeedProperty, value); }
        }

        /// <summary>
        /// Backing store for Speed.
        /// </summary>
        public static readonly DependencyProperty AutoScrollSpeedProperty = DependencyProperty.Register(
            "AutoScrollSpeed",
            typeof(double),
            typeof(River),
            new PropertyMetadata(84.0, (sender, e) => (sender as River).UpdateAutoScrollSpeed()));

        /// <summary>
        /// When the scrolling speed is changed, update the position of the loading indicator.
        /// </summary>
        private void UpdateAutoScrollSpeed()
        {
            VisualStateManager.GoToState(this, AutoScrollSpeed > 0 ? MovingRightState.Name : MovingLeftState.Name, true);
        }

        #endregion

        #region IsContentLoaded

        /// <summary>
        /// Gets or sets a value indicating whether the river should display its loaded state.
        /// </summary>
        /// <value>
        /// <c>true</c> if the river should display its loaded state; otherwise, <c>false</c>.
        /// </value>
        public bool IsContentLoaded
        {
            get { return (bool)GetValue(IsContentLoadedProperty); }
            set { SetValue(IsContentLoadedProperty, value); }
        }

        /// <summary>
        /// Backing store for IsContentLoaded.
        /// </summary>
        public static readonly DependencyProperty IsContentLoadedProperty = DependencyProperty.Register("IsContentLoaded", typeof(bool), typeof(River), new PropertyMetadata(false, (sender, e) => (sender as River).UpdateIsContentLoaded()));

        /// <summary>
        /// Fires when IsContentLoaded changes. Shows or hides the loaded state.
        /// </summary>
        private void UpdateIsContentLoaded()
        {
            VisualStateManager.GoToState(this, IsContentLoaded ? NotLoadingState.Name : IsLoadingState.Name, true);

            if (IsContentLoaded && AutoScrollSpeed == 0)
            {
                // If in manual mode, auto-scroll to the first screen of content.
                HorizontalOffset += ActualWidth;
            }
        }

        #endregion

        #endregion

        #region Attached Properties

        #region ProxyStyle

        /// <summary>
        /// Gets the value of the ProxyStyle attached property. This style is applied to the associated SVI.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the ProxyStyle attached property.</returns>
        internal static Style GetProxyStyle(DependencyObject obj)
        {
            if (obj == null)
            {
                return null;
            }

            return (Style)obj.GetValue(ProxyStyleProperty);
        }

        /// <summary>
        /// Sets the value of the ProxyStyle attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the ProxyStyle attached property.</param>
        /// <param name="value">The property value to set.</param>
        internal static void SetProxyStyle(DependencyObject obj, Style value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(ProxyStyleProperty, value);
        }

        /// <summary>
        /// Identifies the ProxyStyle attached property.
        /// </summary>
        public static readonly DependencyProperty ProxyStyleProperty = DependencyProperty.RegisterAttached("ProxyStyle", typeof(Style), typeof(River), new UIPropertyMetadata(null));

        #endregion

        #region ItemStyle

        /// <summary>
        /// Gets the value of the ItemStyle attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the ItemStyle attached property.</returns>
        public static Style GetItemStyle(DependencyObject obj)
        {
            return (Style)obj.GetValue(ItemStyleProperty);
        }

        /// <summary>
        /// Sets the value of the ItemStyle attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the ItemStyle attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetItemStyle(DependencyObject obj, Style value)
        {
            obj.SetValue(ItemStyleProperty, value);
        }

        /// <summary>
        /// Identifies the ItemStyle attached property.
        /// </summary>
        public static readonly DependencyProperty ItemStyleProperty = DependencyProperty.RegisterAttached("ItemStyle", typeof(Style), typeof(River), new UIPropertyMetadata(null));

        #endregion

        #region TimeoutDelay

        /// <summary>
        /// Gets the value of the TimeoutDelay attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the TimeoutDelay attached property.</returns>
        public static TimeSpan GetTimeoutDelay(DependencyObject obj)
        {
            if (obj == null)
            {
                return TimeSpan.Zero;
            }

            return (TimeSpan)obj.GetValue(TimeoutDelayProperty);
        }

        /// <summary>
        /// Sets the value of the TimeoutDelay attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the TimeoutDelay attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetTimeoutDelay(DependencyObject obj, TimeSpan value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(TimeoutDelayProperty, value);
        }

        /// <summary>
        /// Identifies the TimeoutDelay attached property.
        /// </summary>
        public static readonly DependencyProperty TimeoutDelayProperty = DependencyProperty.RegisterAttached("TimeoutDelay", typeof(TimeSpan), typeof(River), new UIPropertyMetadata(TimeSpan.Zero));

        #endregion

        #endregion

        #region Layout Logic

        /// <summary>
        /// Update the position of the river as well as any programmatic animations each frame.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            UpdateAutoScroll();
            Update();
            _lastFrameTime = DateTime.Now;
        }

        /// <summary>
        /// Update the state of all the items.
        /// </summary>
        private void Update()
        {
            foreach (RiverItemState state in _states)
            {
                if (state == null)
                {
                    continue;
                }

                ItemProxy proxy = null;
                _stateToProxyMap.TryGetValue(state, out proxy);

                UpdateScrollPosition(proxy, state);

                if (state.Svi != null)
                {
                    if (state.IsAnimatingToContactOrientation)
                    {
                        UpdateAnimatingToContactOrientation(state);
                    }

                    if (state.IsAnimatingToRemovedSize)
                    {
                        UpdateAnimatingToRemovedSize(state);
                    }

                    if (state.IsAnimatingBackToRiver)
                    {
                        UpdateAnimatingBackToRiver(state);
                    }
                }
            }
        }

        /// <summary>
        /// Every frame, change the HorizontalOffset based on the scrolling speed.
        /// </summary>
        private void UpdateAutoScroll()
        {
            if (_totalColumns == 0 || !IsContentLoaded)
            {
                return;
            }

            if (Manipulation.IsManipulationActive(this))
            {
                // Don't scroll if the user is scrolling.
                return;
            }

            double elapsed = (DateTime.Now - _lastFrameTime).TotalSeconds;

            if (_isReversing)
            {
                // If reversing, ease the speed up from zero to the scrolling speed.
                double reverseSpeed = SineEaseIn((DateTime.Now - _reversingStartTime).TotalSeconds, 0, AutoScrollSpeed, _reversingDuration);

                if (Math.Abs(reverseSpeed) < Math.Abs(AutoScrollSpeed))
                {
                    // Update the scroll position based on the reversing speed.
                    HorizontalOffset += elapsed * reverseSpeed;
                    return;
                }

                // Once we reach scrolling speed, reversing is done.
                _isReversing = false;
            }

            // Update the scroll position based on the auto scroll speed.
            HorizontalOffset += elapsed * AutoScrollSpeed * -1;
        }

        /// <summary>
        /// Contains the main logic for creating, destroying, and positioning items. This is called whenever the HorizontalOffset changes.
        /// </summary>
        /// <param name="proxy">The proxy to update.</param>
        /// <param name="state">The state of the proxy.</param>
        private void UpdateScrollPosition(ItemProxy proxy, RiverItemState state)
        {
            if (GridCellSize == Size.Empty || ActualWidth == 0 || ActualHeight == 0 || _totalColumns == 0 || _totalRows == 0)
            {
                // Not initialized yet.
                return;
            }

            if (state.IsRemovedFromRiver)
            {
                // This item is pulled from the river, so don't do anything with it.
                return;
            }

            // Get the pool for items of this style.
            Style proxyStyle = state.ProxyStyle;
            Queue<ItemProxy> pool = null;

            if (proxyStyle == null)
            {
                // There's no style, so use the default pool.
                pool = _defaultRiverItemPool;
            }
            else
            {
                // There is a style, so create a pool if needed, and use that.
                if (!_styledRiverItemPool.ContainsKey(proxyStyle))
                {
                    _styledRiverItemPool[proxyStyle] = new Queue<ItemProxy>();
                }

                pool = _styledRiverItemPool[proxyStyle];
            }

            RiverItemVisibilityInfo visibilityInfo = GetItemVisibilityInfo(state);
            if (!visibilityInfo.IsVisible)
            {
                if (proxy != null)
                {
                    RiverItemBase riverItem = proxy.FindVisualChild<RiverItemBase>();

                    // This definition should not be visible, so hide it.
                    state.IsAnimatingBackToRiver = false;
                    state.IsAnimatingToContactOrientation = false;
                    state.IsAnimatingToRemovedSize = false;
                    state.DidAttemptToAddToRiver = false;
                    state.IsRemovedFromRiver = false;
                    _stateToProxyMap[state] = null;
                    _sviToProxyMap.Remove(proxy);
                    proxy.Visibility = Visibility.Hidden;
                    if (riverItem != null)
                    {
                        riverItem.Cleanup();
                    }

                    pool.Enqueue(proxy);
                }

                return;
            }

            // The definition should be visible, but it's not paired with any ItemProxy yet, so build or recycle one.
            if (proxy == null)
            {
                if (pool.Count > 0)
                {
                    // Use an SVI from the queue.
                    proxy = pool.Dequeue();
                    proxy.Visibility = Visibility.Visible;
                }
                else
                {
                    // Set up a new proxy.
                    proxy = AddNewItemProxy();
                    proxy.Style = proxyStyle;
                }

                // Size the ScatterView item when it's first added to the river.
                bool isPortrait = state.RowSpan > state.ColumnSpan;
                double itemWidth = state.ColumnSpan * GridCellSize.Width;
                double itemHeight = state.RowSpan * GridCellSize.Height;
                itemWidth += Plane.GetPlaneFrontPadding(proxy).Left + Plane.GetPlaneFrontPadding(proxy).Right;
                itemHeight += Plane.GetPlaneFrontPadding(proxy).Top + Plane.GetPlaneFrontPadding(proxy).Bottom;
                proxy.Width = isPortrait ? itemHeight : itemWidth;
                proxy.Height = isPortrait ? itemWidth : itemHeight;
                proxy.Visibility = Visibility.Visible;

                // More setup of added items.
                _stateToProxyMap[state] = proxy;
                _sviToProxyMap[proxy] = state;
                AttemptToAddToRiver(proxy, false, false);
            }

            if (state.DidAttemptToAddToRiver)
            {
                // We already tried to add this one and it rejected the add, so wait until it's looped around.
                return;
            }

            // Update the position of the item.
            proxy.Center = GetItemCenter(state);
        }

        /// <summary>
        /// Initialize a new ItemProxy.
        /// </summary>
        /// <returns>The new ItemProxy</returns>
        private ItemProxy AddNewItemProxy()
        {
            ItemProxy proxy = new ItemProxy();
            proxy.SnapsToDevicePixels = false;
            proxy.MinHeight = GridCellSize.Height;
            proxy.MinWidth = GridCellSize.Width;
            proxy.PreviewTouchDown += ItemProxy_PreviewTouchDown;
            proxy.Loaded += (sender, e) => AttemptToAddToRiver(sender as ItemProxy, false, false);
            proxy.AddHandler(RiverItemBase.RefreshRequestedEvent, new RoutedEventHandler(ItemProxy_RefreshRequested));
            _proxyContainer.Children.Add(proxy);
            return proxy;
        }

        /// <summary>
        /// Called when a ItemProxy should be displayed in the river. The method will attempt to get data for the item either from the
        /// river's data source or the history. If no data is available, the item will not be shown and no attempt will be made to show it
        /// until the next loop around the grid.
        /// </summary>
        /// <param name="proxy">The ItemProxy which should be added.</param>
        /// <param name="maintainOrientation">if set to <c>true</c> [maintain orientation].</param>
        /// <param name="maintainUnblockedData">if set to <c>true</c> [maintain unblocked data].</param>
        private void AttemptToAddToRiver(ItemProxy proxy, bool maintainOrientation, bool maintainUnblockedData)
        {
            RiverItemState state = GetState(proxy);
            if (state == null)
            {
                return;
            }

            if (state.DidAttemptToAddToRiver)
            {
                return;
            }

            RiverItemBase riverItem = proxy.FindVisualChild<RiverItemBase>();
            double orientation = 0;

            bool isPortrait = state.RowSpan > state.ColumnSpan;

            if (riverItem == null)
            {
                orientation = isPortrait ? 90 : 0;
            }
            else
            {
                int page = (int)Math.Ceiling(_cumulativeScrollChange / (_totalColumns * GridCellSize.Width));
                if (GetItemVisibilityInfo(state).IsLooping)
                {
                    page++;
                }

                RiverItemHistoryInfo history = _history.Where(h => h.State == state && h.Grid == page).FirstOrDefault();

                object data = null;

                if (history != null)
                {
                    // Use the historical data and orientation if it exists.
                    data = history.Data;
                    orientation = history.Orientation;
                }
                else
                {
                    data = riverItem.GetData(state, maintainUnblockedData);

                    if (InteractiveSurface.PrimarySurfaceDevice.Tilt == Tilt.Horizontal)
                    {
                        // If running on a tabletop, randomly rotate the item.
                        orientation = _random.NextDouble() > .5 ? 0 : 180;

                        if (isPortrait)
                        {
                            // Rotate vertical items.
                            orientation += 90 * (_random.NextDouble() > .5 ? 1 : -1);
                        }

                        if (state.RowSpan == state.ColumnSpan && _random.NextDouble() > .5)
                        {
                            // Maybe rotate square items.
                            orientation += 90 * (_random.NextDouble() > .5 ? 1 : -1);
                        }
                    }
                    else if (isPortrait)
                    {
                        // If running vertically, portrait items still get rotated to portrait.
                        orientation = _random.NextDouble() > .5 ? 90 : -90;
                    }
                    else
                    {
                        // If running vertically and not portrait, always render right-side-up.
                        orientation = 0;
                    }

                    history = new RiverItemHistoryInfo { State = state, Grid = page, Orientation = orientation };
                    _history.Add(history);
                }

                history.Data = riverItem.RenderData(state, data);

                if (history.Data == null)
                {
                    // The item rejected this data and didn't provide new data.
                    _history.Remove(history);
                }

                if (data == null)
                {
                    // The item rejected the attempt to add, so don't try again until it's looped around.
                    state.DidAttemptToAddToRiver = true;
                    proxy.Visibility = Visibility.Collapsed;
                }
            }

            if (maintainOrientation)
            {
                // Don't apply the orientation if this was a user-requested refresh.
                return;
            }

            proxy.Orientation = state.OriginalOrientation = orientation;
        }

        /// <summary>
        /// Determines whether the item is visible given the current river position.
        /// </summary>
        /// <param name="state">The river item.</param>
        /// <returns>The ItemVisibilityInfo.</returns>
        private RiverItemVisibilityInfo GetItemVisibilityInfo(RiverItemState state)
        {
            double leftColumn = HorizontalOffset / GridCellSize.Width;

            // Determine if the item is visible given the current river position.
            bool isVisible =
                state.Column + state.ColumnSpan >= leftColumn &&
                state.Column <= leftColumn + _screenColumns;

            // Whether or not this item is displaying past the end of the river, and looping around.
            bool isLooping = false;

            if (!isVisible && leftColumn + _screenColumns > _totalColumns)
            {
                // If this item is being displayed after the end of the river, do a different comparison to see if it's visible.
                int column = _totalColumns + state.Column;

                isVisible =
                    column + state.ColumnSpan >= leftColumn &&
                    column <= leftColumn + _screenColumns;

                if (isVisible)
                {
                    // If it's visible, set a flag to position it differently later.
                    isLooping = true;
                }
            }

            return new RiverItemVisibilityInfo { IsLooping = isLooping, IsVisible = isVisible };
        }

        /// <summary>
        /// Given an item definition, return the center of that item for the current HorizontalOffset.
        /// </summary>
        /// <param name="state">The river item.</param>
        /// <returns>The center of the river item.</returns>
        private Point GetItemCenter(RiverItemState state)
        {
            double leftColumn = HorizontalOffset / GridCellSize.Width;

            // Naive left position.
            double left = (state.Column * GridCellSize.Width) - (leftColumn * GridCellSize.Width);
            if (GetItemVisibilityInfo(state).IsLooping)
            {
                // Left position when looping.
                left += _totalColumns * GridCellSize.Width;
            }

            // Shift to center.
            left += (state.ColumnSpan * GridCellSize.Width) / 2;

            // Naive top position.
            double top = state.Row * GridCellSize.Height;

            // Shift to center.
            top += (state.RowSpan * GridCellSize.Height) / 2;

            // Shift so that the whole river is centered vertically.
            top += (ActualHeight - (_totalRows * GridCellSize.Height)) / 2;

            return new Point(left, top);
        }

        #endregion

        #region Manipulation Event Handlers

        /// <summary>
        /// Handles the PreviewContactDown event of the ItemProxy control.
        /// If a ItemProxy can't move, block the contact so that a manipulation doesn't begin. Otherwise, create an SVI from the proxy.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.TouchEventArgs"/> instance containing the event data.</param>
        private void ItemProxy_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            ItemProxy proxy = sender as ItemProxy;
            RiverItemState state = GetState(proxy);

            if (!proxy.CanMove || state.IsAnimatingToContactOrientation || state.IsAnimatingToRemovedSize || state.IsAnimatingBackToRiver)
            {
                e.Handled = true;
                return;
            }

            if ((e.OriginalSource as FrameworkElement).FindVisualParent<Button>() != null)
            {
                // Ignore if an admin button is pressed.
                return;
            }

            // Create the SVI and set its position and orientation.
            ScatterViewItem svi = new ScatterViewItem();
            _scatterView.Items.Add(svi);
            state.Svi = svi;
            svi.Style = state.ItemStyle;
            svi.MinHeight = proxy.MinHeight;
            svi.MinWidth = proxy.MinWidth;
            svi.Center = proxy.Center;
            svi.Orientation = proxy.Orientation;
            svi.CanScale = false;
            svi.CanRotate = false;

            // Set the SVI size.
            bool isPortrait = state.RowSpan > state.ColumnSpan;
            double itemWidth = state.ColumnSpan * GridCellSize.Width;
            double itemHeight = state.RowSpan * GridCellSize.Height;
            itemWidth += Plane.GetPlaneFrontPadding(svi).Left + Plane.GetPlaneFrontPadding(svi).Right;
            itemHeight += Plane.GetPlaneFrontPadding(svi).Top + Plane.GetPlaneFrontPadding(svi).Bottom;
            svi.Width = isPortrait ? itemHeight : itemWidth;
            svi.Height = isPortrait ? itemWidth : itemHeight;

            // Hook events.
            svi.ContainerActivated += ScatterViewItem_Activated;
            svi.ContainerManipulationDelta += ScatterViewItem_ScatterManipulationDelta;
            svi.ContainerDeactivated += ScatterViewItem_Deactivated;
            svi.ContainerManipulationCompleted += ScatterViewItem_ScatterManipulationCompleted;
            TouchChangedEvents.AddAreAnyTouchesCapturedWithinChangedHandler(svi, new RoutedEventHandler(ScatterViewItem_AreAnyTouchesCapturedWithinChanged));
            svi.AddHandler(RiverItemBase.FlipRequestedEvent, new RiverItemBase.SourceRoutedEventHandler(ScatterViewItem_FlipRequested));
            svi.AddHandler(RiverItemBase.CloseRequestedEvent, new RiverItemBase.SourceRoutedEventHandler(ScatterViewItem_CloseRequested));

            svi.Unloaded += (a, b) =>
            {
                // Unhook events on unload.
                svi.ContainerActivated -= ScatterViewItem_Activated;
                svi.ContainerManipulationDelta -= ScatterViewItem_ScatterManipulationDelta;
                svi.ContainerDeactivated -= ScatterViewItem_Deactivated;
                svi.ContainerManipulationCompleted -= ScatterViewItem_ScatterManipulationCompleted;
                TouchChangedEvents.RemoveAreAnyTouchesCapturedWithinChangedHandler(svi, new RoutedEventHandler(ScatterViewItem_AreAnyTouchesCapturedWithinChanged));
                svi.RemoveHandler(RiverItemBase.FlipRequestedEvent, new RiverItemBase.SourceRoutedEventHandler(ScatterViewItem_FlipRequested));
                svi.RemoveHandler(RiverItemBase.CloseRequestedEvent, new RiverItemBase.SourceRoutedEventHandler(ScatterViewItem_CloseRequested));
            };

            svi.Loaded += (a, b) =>
            {
                // Populate with data when loaded.
                RiverItemBase proxyItem = proxy.FindVisualChild<RiverItemBase>();
                RiverItemBase sviItem = svi.FindVisualChild<RiverItemBase>();
                sviItem.RenderData(state, proxyItem.DataContext);
                proxy.Visibility = Visibility.Hidden;
            };

            // Steal the touch from the proxy.
            svi.UpdateLayout();
            svi.CaptureTouch(e.TouchDevice);
        }

        /// <summary>
        /// Handles the Activated event of the ItemProxy control.
        /// When an item begins to be manipulated, begin animating its orientation to that of the contact touching it.
        /// Not using ManipulationStarted event because in some rare cases, ManipulationStarted never fires.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ScatterViewItem_Activated(object sender, RoutedEventArgs e)
        {
            ScatterViewItem svi = sender as ScatterViewItem;
            RiverItemState state = GetState(svi);
            if (state == null)
            {
                return;
            }

            TouchDevice contact = svi.TouchesCaptured.FirstOrDefault();
            if (!state.IsRemovedFromRiver && contact != null)
            {
                state.OriginalHorizontalOffset = _cumulativeScrollChange;

                if (InteractiveSurface.PrimarySurfaceDevice.IsFingerRecognitionSupported &&
                    InteractiveSurface.PrimarySurfaceDevice.IsTouchOrientationSupported &&
                    InteractiveSurface.PrimarySurfaceDevice.Tilt == Tilt.Horizontal &&
                    TouchExtensions.GetIsFingerRecognized(contact))
                {
                    // We get finger orientation and this is a finger and the surface is horizontal, so orient to the finger.
                    state.ContactOrientation = contact.GetOrientation(this) + 90;
                }
                else if (InteractiveSurface.PrimarySurfaceDevice.Tilt == Tilt.Vertical || contact is MouseTouchDevice)
                {
                    // The surface is vertical, so orient to the bottom.
                    state.ContactOrientation = 0;
                }
                else
                {
                    // We don't get finger orientation, so just stick with the original orientation.
                    state.ContactOrientation = state.OriginalOrientation;
                }

                state.AnimateToContactOrientationBeginTime = DateTime.Now;
                state.IsAnimatingToContactOrientation = true;

                Audio.Instance.PlayCue("streamItem_tapDown");
            }

            UpdateManipulation(svi);
        }

        /// <summary>
        /// Handles the ScatterManipulationDelta event of the ItemProxy control.
        /// When an item is manipulated, update the attached properties which store its current state.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.Surface.Presentation.Controls.ContainerManipulationDeltaEventArgs"/> instance containing the event data.</param>
        private void ScatterViewItem_ScatterManipulationDelta(object sender, ContainerManipulationDeltaEventArgs e)
        {
            UpdateManipulation(sender as ScatterViewItem);
        }

        /// <summary>
        /// Handles the ScatterManipulationCompleted event of the ItemProxy control.
        /// When the user stops manipulating the item, begin an idle timeout.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Microsoft.Surface.Presentation.Controls.ContainerManipulationCompletedEventArgs"/> instance containing the event data.</param>
        private void ScatterViewItem_ScatterManipulationCompleted(object sender, ContainerManipulationCompletedEventArgs e)
        {
            FinishManipulation(sender as ScatterViewItem);
        }

        /// <summary>
        /// Handles the Deactivated event of the ItemProxy control.
        /// When the user stops manipulating the item, begin an idle timeout.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ScatterViewItem_Deactivated(object sender, RoutedEventArgs e)
        {
            FinishManipulation(sender as ScatterViewItem);
        }

        /// <summary>
        /// Finishes the manipulation.
        /// </summary>
        /// <param name="svi">The ItemProxy.</param>
        private void FinishManipulation(ScatterViewItem svi)
        {
            RiverItemState state = GetState(svi);
            if (state == null)
            {
                return;
            }

            if (state.RemovedSize == Size.Empty)
            {
                RiverItemBase riverItem = svi.FindVisualChild<RiverItemBase>();
                if (riverItem != null)
                {
                    state.AnimateToRemovedSizeBeginTime = DateTime.Now;
                    state.IsAnimatingToRemovedSize = true;
                    Thickness padding = Plane.GetPlaneFrontPadding(svi);
                    RiverSize riverSize = riverItem.Removed();
                    state.RemovedSize = riverSize.RemovedSize;
                    state.MinSize = riverSize.MinSize;
                    svi.MaxWidth = riverSize.MaxSize.Width + padding.Left + padding.Right;
                    svi.MaxHeight = riverSize.MaxSize.Height + padding.Top + padding.Bottom;
                    Audio.Instance.PlayCue("streamItem_tapRelease");
                }
            }

            if (!svi.AreAnyTouchesCapturedWithin)
            {
                BeginIdleTimeout(svi);
            }
        }

        /// <summary>
        /// Handles the AreAnyTouchesCapturedWithinChanged event of the ItemProxy control. Resets the idle timeout on the item when any nested controls are interacted with.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.DependencyPropertyChangedEventArgs"/> instance containing the event data.</param>
        private void ScatterViewItem_AreAnyTouchesCapturedWithinChanged(object sender, EventArgs e)
        {
            ScatterViewItem svi = sender as ScatterViewItem;
            RiverItemState state = GetState(svi);
            if (state == null)
            {
                return;
            }

            if (svi.AreAnyTouchesCapturedWithin)
            {
                CancelIdleTimeout(svi);
            }
            else if (state.IsRemovedFromRiver && !state.IsAnimatingBackToRiver)
            {
                BeginIdleTimeout(svi);
            }
        }

        /// <summary>
        /// Updates the attached properties which store the item's state whenever it's manipulated.
        /// </summary>
        /// <param name="svi">The river item info.</param>
        private void UpdateManipulation(ScatterViewItem svi)
        {
            RiverItemState state = GetState(svi);
            if (state == null)
            {
                return;
            }

            // When this is true, UpdateGrid() won't position the item.
            state.IsRemovedFromRiver = true;

            // If the user touches during a size or back-to-river animation, stop the animation and let the user control it.
            if (state.IsAnimatingToRemovedSize)
            {
                svi.CanScale = true;
            }

            state.IsAnimatingBackToRiver = false;

            // When the item is animated back to the river, it will begin from its current center, orientation, and size.
            state.AnimateBackFromCenter = svi.Center;
            state.AnimateBackFromOrientation = svi.Orientation;
            state.AnimateBackFromSize = new Size(svi.ActualWidth, svi.ActualHeight);

            // SnapsToDevicePixels is false while it's in the river, so that scrolling looks smoother. It's true when pulled out, so text looks better.
            svi.SnapsToDevicePixels = true;

            // Every time the item is manipulated, cancel the idle timeout.
            CancelIdleTimeout(svi);
        }

        #endregion

        #region Idle Timeouts

        /// <summary>
        /// Begins the idle timeout for an item.
        /// </summary>
        /// <param name="svi">The item on which to cancel the idle timeout.</param>
        private void BeginIdleTimeout(ScatterViewItem svi)
        {
            CancelIdleTimeout(svi);
            RiverItemState state = GetState(svi);
            if (state == null)
            {
                return;
            }

            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += (sender, e) =>
            {
                ReturnItemProxyToRiver(svi);
            };
            state.IdleTimer = timer;

            timer.Interval = ScatterFlip.GetIsFlipped(svi) ? IdleTimeoutBack : IdleTimeoutFront;
            timer.Start();
            SetTimeoutDelay(svi, timer.Interval);
        }

        /// <summary>
        /// Cancels the idle timeout for an item.
        /// </summary>
        /// <param name="svi">The item on which to cancel the idle timeout.</param>
        private void CancelIdleTimeout(ScatterViewItem svi)
        {
            RiverItemState state = GetState(svi);
            if (state == null)
            {
                return;
            }

            SetTimeoutDelay(svi, TimeSpan.Zero);
            DispatcherTimer timer = state.IdleTimer;
            if (timer != null)
            {
                timer.Stop();
            }

            state.IdleTimer = null;
        }

        #endregion

        #region Programmatic Animations

        /// <summary>
        /// Update the position of items which are animating to match the orientation of their contact.
        /// </summary>
        /// <param name="state">The state of the SVI.</param>
        private void UpdateAnimatingToContactOrientation(RiverItemState state)
        {
            ScatterViewItem svi = state.Svi;
            double contactOrientation = state.ContactOrientation;
            double begin = state.AnimateBackFromOrientation % 360;
            double end = contactOrientation % 360;
            double change = end - begin;
            if (Math.Abs(change) > 180)
            {
                // Don't go the long way around.
                if (end < begin)
                {
                    end += 360;
                }
                else
                {
                    begin += 360;
                }
            }

            change = end - begin;

            double currentTime = (DateTime.Now - state.AnimateToContactOrientationBeginTime).TotalSeconds;
            double totalTime = Math.Max(MinimumTransitionDuration.TotalSeconds, Math.Abs(change / OrientSpeed));

            if (currentTime < totalTime && Math.Abs(svi.Orientation - contactOrientation) > 1)
            {
                svi.Orientation = SineEaseOut(currentTime, begin, change, totalTime);
            }
            else
            {
                svi.Orientation = contactOrientation;
                svi.CanRotate = true;
                state.IsAnimatingToContactOrientation = false;
                UpdateManipulation(svi);
                BeginIdleTimeout(svi);
            }
        }

        /// <summary>
        /// Performs programmatic animation of an item to its removed size.
        /// </summary>
        /// <param name="state">The state of the SVI.</param>
        private void UpdateAnimatingToRemovedSize(RiverItemState state)
        {
            ScatterViewItem svi = state.Svi;
            Thickness padding = Plane.GetPlaneFrontPadding(svi);
            bool isPortrait = state.RowSpan > state.ColumnSpan;
            double itemWidth = state.ColumnSpan * GridCellSize.Width;
            double itemHeight = state.RowSpan * GridCellSize.Height;
            itemWidth += padding.Left + padding.Right;
            itemHeight += padding.Top + padding.Bottom;
            double beginWidth = isPortrait ? itemHeight : itemWidth;
            double beginHeight = isPortrait ? itemWidth : itemHeight;

            double endWidth = state.RemovedSize.Width;
            double endHeight = state.RemovedSize.Height;
            endWidth += Plane.GetPlaneFrontPadding(svi).Left + Plane.GetPlaneFrontPadding(svi).Right;
            endHeight += Plane.GetPlaneFrontPadding(svi).Top + Plane.GetPlaneFrontPadding(svi).Bottom;

            double currentTime = (DateTime.Now - state.AnimateToRemovedSizeBeginTime).TotalSeconds;
            double totalTime = MinimumTransitionDuration.TotalSeconds;

            if (currentTime < totalTime)
            {
                svi.Width = SineEaseOut(currentTime, beginWidth, endWidth - beginWidth, totalTime);
                svi.Height = SineEaseOut(currentTime, beginHeight, endHeight - beginHeight, totalTime);
            }
            else
            {
                svi.Width = endWidth;
                svi.Height = endHeight;
                svi.MinWidth = Math.Min(svi.ActualWidth, state.MinSize.Width + padding.Left + padding.Right);
                svi.MinHeight = Math.Min(svi.ActualHeight, state.MinSize.Height + padding.Top + padding.Bottom);
                svi.CanScale = true;
                state.IsAnimatingToRemovedSize = false;

                RiverItemBase riverItem = svi.FindVisualChild<RiverItemBase>();
                if (riverItem != null)
                {
                    riverItem.RemoveFinished();
                }
            }

            state.AnimateBackFromSize = new Size(svi.ActualWidth, svi.ActualHeight);
        }

        /// <summary>
        /// Performs programmatic animation of a item returning back to the river.
        /// </summary>
        /// <param name="state">The state of the SVI.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Sorry.")]
        private void UpdateAnimatingBackToRiver(RiverItemState state)
        {
            ScatterViewItem svi = state.Svi;

            if (state.IsAnimatingBackToRiverComplete)
            {
                // When the animation is complete, return the item to the river.
                state.IsAnimatingBackToRiver = false;
                state.IsAnimatingBackToRiverComplete = false;
                state.IsRemovedFromRiver = false;
                state.Svi.IsHitTestVisible = true;
                state.RemovedSize = Size.Empty;
                state.Svi = null;
                _scatterView.Items.Remove(svi);
                _stateToProxyMap[state].Visibility = Visibility.Visible;
                UpdateScrollPosition(_stateToProxyMap[state], state);
                return;
            }

            Rect screen = new Rect(0, 0, Application.Current.MainWindow.ActualWidth, Application.Current.MainWindow.ActualHeight);
            double diagonal = (new Point(svi.ActualWidth, svi.ActualHeight) - new Point()).Length;
            screen.Inflate(new Size(diagonal, diagonal));

            double currentTime = (DateTime.Now - state.AnimateBackBeginTime).TotalSeconds;

            // Determine the size to animate to.
            bool isPortrait = state.RowSpan > state.ColumnSpan;
            double itemWidth = state.ColumnSpan * GridCellSize.Width;
            double itemHeight = state.RowSpan * GridCellSize.Height;
            itemWidth += Plane.GetPlaneFrontPadding(svi).Left + Plane.GetPlaneFrontPadding(svi).Right;
            itemHeight += Plane.GetPlaneFrontPadding(svi).Top + Plane.GetPlaneFrontPadding(svi).Bottom;
            double targetWidth = isPortrait ? itemHeight : itemWidth;
            double targetHeight = isPortrait ? itemWidth : itemHeight;

            // Determine the orientation to animate to.
            double begin = state.AnimateBackFromOrientation % 360;
            double end = state.OriginalOrientation % 360;
            double change = end - begin;
            if (Math.Abs(change) > 180)
            {
                // Don't go the long way around.
                if (end < begin)
                {
                    end += 360;
                }
                else
                {
                    begin += 360;
                }
            }

            change = end - begin;
            double orientationTime = Math.Abs(change / OrientSpeed);

            // Determine the center to animate to.
            Point center = GetItemCenter(state);
            double scrollChange = state.ReturnHorizontalOffset - state.OriginalHorizontalOffset;
            if (center.X > ActualWidth && scrollChange > 0)
            {
                // The position looped around, so just go off-screen left.
                center.X = -diagonal;
            }
            else if (center.X < 0 && scrollChange < 0)
            {
                // The position looped around, so just go off-screen right.
                center.X = ActualWidth + diagonal;
            }

            double centerTime = (state.AnimateBackFromCenter - center).Length / ReturnToRiverSpeed;

            // The duration of the animation will be the duration of the orientation or center animations, whichever is longer.
            double totalTime = centerTime >= MinimumTransitionDuration.TotalSeconds || centerTime > orientationTime ? centerTime : orientationTime;
            totalTime = Math.Max(totalTime, MinimumTransitionDuration.TotalSeconds);

            // Update the orientation animation.
            if (currentTime < totalTime && Math.Abs(state.OriginalOrientation - svi.Orientation) > 1)
            {
                svi.Orientation = SineEaseOut(currentTime, begin, change, totalTime);
            }
            else
            {
                svi.Orientation = state.OriginalOrientation;
            }

            // Update the center animation.
            if (currentTime < totalTime && (svi.Center - center).Length > 1)
            {
                svi.Center = new Point(
                    SineEaseOut(currentTime, state.AnimateBackFromCenter.X, center.X - state.AnimateBackFromCenter.X, totalTime),
                    SineEaseOut(currentTime, state.AnimateBackFromCenter.Y, center.Y - state.AnimateBackFromCenter.Y, totalTime));
            }
            else
            {
                svi.Center = center;
            }

            // Update the width animation.
            if (currentTime < totalTime && Math.Abs(targetWidth - svi.ActualWidth) > 1)
            {
                svi.Width = SineEaseOut(currentTime, state.AnimateBackFromSize.Width, targetWidth - state.AnimateBackFromSize.Width, totalTime);
            }
            else
            {
                svi.Width = targetWidth;
            }

            // Update the height animation.
            if (currentTime < totalTime && Math.Abs(targetHeight - svi.ActualHeight) > 1)
            {
                svi.Height = SineEaseOut(currentTime, state.AnimateBackFromSize.Height, targetHeight - state.AnimateBackFromSize.Height, totalTime);
            }
            else
            {
                svi.Height = targetHeight;
            }

            // If the item animated offscreen, just let it go.
            bool isOutsideScreen = !screen.Contains(svi.Center);
            if (isOutsideScreen)
            {
                svi.Orientation = state.OriginalOrientation;
                svi.Center = center;
                svi.Width = targetWidth;
                svi.Height = targetHeight;
            }

            state.IsAnimatingBackToRiverComplete =
                svi.Orientation == state.OriginalOrientation &&
                svi.Center == center &&
                svi.Width == targetWidth &&
                svi.Height == targetHeight;
        }

        #endregion

        #region Scrolling

        /// <summary>
        /// Occurs when a contact over an element is placed on the Microsoft Surface screen. This method is a virtual method.
        /// Begin a scroll when a finger is placed on an area of the river that doesn't contain any items.
        /// </summary>
        /// <param name="e">The <strong><see cref="T:Microsoft.Surface.Presentation.ContactEventArgs"/></strong> object that contains the event data.</param>
        protected override void OnTouchDown(TouchEventArgs e)
        {
            if (e != null && e.TouchDevice.GetIsFingerRecognized() && IsContentLoaded)
            {
                if (Manipulation.IsManipulationActive(this))
                {
                    Manipulation.CompleteManipulation(this);
                }

                Manipulation.AddManipulator(this, e.TouchDevice);
            }

            base.OnTouchDown(e);
        }

        /// <summary>
        /// Occurs when a contact over an element leaves the Microsoft Surface screen. This method is a virtual method.
        /// Stop tracking scrolling contacts.
        /// </summary>
        /// <param name="e">The <strong><see cref="T:Microsoft.Surface.Presentation.ContactEventArgs"/></strong> object that contains the event data.</param>
        protected override void OnTouchUp(TouchEventArgs e)
        {
            if (e != null && e.TouchDevice.GetIsFingerRecognized() && Manipulation.IsManipulationActive(this))
            {
                Manipulation.RemoveManipulator(this, e.TouchDevice);
            }

            base.OnTouchUp(e);
        }

        /// <summary>
        /// Handles the Affine2DManipulationDelta event of the ManipulationProcessor.
        /// Update the scroll position when the user scrolls the river.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ManipulationDeltaEventArgs"/> instance containing the event data.</param>
        private void River_ManipulationDelta(object sender, ManipulationDeltaEventArgs e)
        {
            if (!e.IsInertial)
            {
                HorizontalOffset -= e.DeltaManipulation.Translation.X;
            }
            else
            {
                if (Math.Sign(e.Velocities.LinearVelocity.X) != Math.Sign(AutoScrollSpeed) && Math.Abs(e.Velocities.LinearVelocity.X) * 1000 <= Math.Abs(AutoScrollSpeed))
                {
                    // If scrolling in the same direction as the river, stop the inertia when its velocity reaches the default speed.
                    Manipulation.CompleteManipulation(this);
                    return;
                }

                HorizontalOffset -= e.DeltaManipulation.Translation.X;
            }
        }

        /// <summary>
        /// Handles the ManipulationInertiaStarting event of the River control. Sets inertia properties.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ManipulationInertiaStartingEventArgs"/> instance containing the event data.</param>
        private void River_ManipulationInertiaStarting(object sender, ManipulationInertiaStartingEventArgs e)
        {
            e.TranslationBehavior.InitialVelocity = e.InitialVelocities.LinearVelocity;
            e.TranslationBehavior.DesiredDeceleration = (20.0 * 96.0) / 1000000.0;
        }

        /// <summary>
        /// Handles the Affine2DInertiaCompleted event of the InertiaProcessor.
        /// After an inertial scroll in the opposite direction of the river, begin reversing back up to speed.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.ManipulationCompletedEventArgs"/> instance containing the event data.</param>
        private void InertiaProcessor_Affine2DInertiaCompleted(object sender, ManipulationCompletedEventArgs e)
        {
            if (Math.Sign(e.TotalManipulation.Translation.X) != Math.Sign(AutoScrollSpeed))
            {
                return;
            }

            _isReversing = true;
            _reversingStartTime = DateTime.Now;
        }

        #endregion

        #region Easing

        /// <summary>
        /// Easing equation function for a sinusoidal (sin(t)) easing out: decelerating from zero velocity.
        /// </summary>
        /// <param name="currentTime">Current time in seconds.</param>
        /// <param name="startValue">Starting value.</param>
        /// <param name="endValue">Final value.</param>
        /// <param name="duration">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        private static double SineEaseOut(double currentTime, double startValue, double endValue, double duration)
        {
            return (endValue * Math.Sin((currentTime / duration) * (Math.PI / 2))) + startValue;
        }

        /// <summary>
        /// Easing equation function for a sinusoidal (sin(t)) easing in: 
        /// accelerating from zero velocity.
        /// </summary>
        /// <param name="currentTime">Current time in seconds.</param>
        /// <param name="startValue">Starting value.</param>
        /// <param name="endValue">Final value.</param>
        /// <param name="duration">Duration of animation.</param>
        /// <returns>The correct value.</returns>
        public static double SineEaseIn(double currentTime, double startValue, double endValue, double duration)
        {
            return (-endValue * Math.Cos(currentTime / duration * (Math.PI / 2))) + endValue + startValue;
        }

        #endregion

        #region Misc

        /// <summary>
        /// Returns an SVI to the river.
        /// </summary>
        /// <param name="svi">The ItemProxy.</param>
        private void ReturnItemProxyToRiver(ScatterViewItem svi)
        {
            RiverItemState state = GetState(svi);
            if (state == null)
            {
                return;
            }

            CancelIdleTimeout(svi);

            if (!state.IsRemovedFromRiver)
            {
                return;
            }

            Audio.Instance.PlayCue("streamItem_close");
            svi.CanScale = false;
            svi.MinHeight = GridCellSize.Height;
            svi.MinWidth = GridCellSize.Width;
            state.IsAnimatingBackToRiver = true;
            state.Svi.IsHitTestVisible = false;
            state.IsAnimatingToRemovedSize = false;
            state.IsAnimatingToContactOrientation = false;
            state.AnimateBackBeginTime = DateTime.Now;
            state.ReturnHorizontalOffset = _cumulativeScrollChange;
            ScatterFlip.SetIsFlipped(svi, false);

            RiverItemBase riverItem = svi.FindVisualChild<RiverItemBase>();
            if (riverItem != null)
            {
                riverItem.Added();
            }
        }

        /// <summary>
        /// Handles the FlipRequested event of the ItemProxy control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ScatterViewItem_FlipRequested(object sender, UserSourceRoutedEventArgs e)
        {
            ScatterViewItem svi = _scatterView.ContainerFromElement(sender as FrameworkElement) as ScatterViewItem;
            SurfaceButton button = e.UserSource as SurfaceButton;

            if (button == null && svi.AreAnyTouchesCapturedWithin)
            {
                // Don't respond if any contact is captured.
                return;
            }

            if (button != null && svi.TouchesCapturedWithin.Where(c => !button.TouchesCapturedWithin.Contains(c)).Count() > 0)
            {
                // If the only contacts captured are within the source object, it's ok to respond.
                return;
            }

            Audio.Instance.PlayCue("streamItem_flip");
            ScatterFlip.SetIsFlipped(svi, !ScatterFlip.GetIsFlipped(svi));
            BeginIdleTimeout(svi);
        }

        /// <summary>
        /// Handles the CloseRequested event of any RiverItems inside of a ItemProxy. Returns the item to the river
        /// when a close button is tapped.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ScatterViewItem_CloseRequested(object sender, UserSourceRoutedEventArgs e)
        {
            ScatterViewItem svi = _scatterView.ContainerFromElement(sender as FrameworkElement) as ScatterViewItem;
            SurfaceButton button = e.UserSource as SurfaceButton;

            if (button == null && svi.AreAnyTouchesCapturedWithin)
            {
                // Don't respond if any contact is captured.
                return;
            }

            if (button != null && svi.TouchesCapturedWithin.Where(c => !button.TouchesCapturedWithin.Contains(c)).Count() > 0)
            {
                // If the only contacts captured are within the source object, it's ok to respond.
                return;
            }

            ReturnItemProxyToRiver(svi);
        }

        /// <summary>
        /// Handles the RefreshRequested of any RiverItems inside of a ItemProxy. Refreshes the content of all items.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void ItemProxy_RefreshRequested(object sender, RoutedEventArgs e)
        {
            foreach (ItemProxy svi in _proxyContainer.Children)
            {
                RiverItemState state = GetState(svi);
                if (state == null)
                {
                    continue;
                }

                int page = (int)Math.Ceiling(_cumulativeScrollChange / (_totalColumns * GridCellSize.Width));
                if (GetItemVisibilityInfo(state).IsLooping)
                {
                    page++;
                }

                RiverItemHistoryInfo history = _history.Where(h => h.State == state && h.Grid == page).FirstOrDefault();
                if (history != null)
                {
                    _history.Remove(history);
                }

                AttemptToAddToRiver(svi, true, true);
            }
        }

        /// <summary>
        /// Remove items from the history which are no longer valid.
        /// </summary>
        /// <param name="validItems">The valid items.</param>
        internal void PurgeHistory(IList<object> validItems)
        {
            _history = (from h in _history where validItems.Contains(h.Data) select h).ToList();
        }

        /// <summary>
        /// Shortcut to get an SVI's state object.
        /// </summary>
        /// <param name="proxy">The SVI to look up.</param>
        /// <returns>The SVI's state.</returns>
        private RiverItemState GetState(ItemProxy proxy)
        {
            RiverItemState state = null;
            _sviToProxyMap.TryGetValue(proxy, out state);
            return state;
        }

        /// <summary>
        /// Given a ScatterViewItem, return the matching RiverItemState.
        /// </summary>
        /// <param name="svi">The ScatterViewItem.</param>
        /// <returns>the matching RiverItemState</returns>
        private RiverItemState GetState(ScatterViewItem svi)
        {
            return (from kvp in _sviToProxyMap
                    where kvp.Value != null &&
                        kvp.Value.Svi == svi
                    select kvp.Value).FirstOrDefault();
        }

        #endregion
    }
}
