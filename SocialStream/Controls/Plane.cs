using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SocialStream.Controls
{
    /// <summary>
    /// A control which supports rotation along the 3D X/Y/Z axes. It's based on both Planerator
    /// and ContentControl3D.
    /// http://blogs.msdn.com/greg_schechter/archive/2007/10/26/enter-the-planerator-dead-simple-3d-in-wpf-with-a-stupid-name.aspx
    /// http://joshsmithonwpf.wordpress.com/2009/02/23/introducing-contentcontrol3d/
    /// Like ContentControl3D, it supports having different content applied to the "back" of the plane
    /// using the BackContent property. Like Planerator, it respects the sizing of the content.
    /// Unlike either, it will show the content without using the 3D engine whenever it's rotated such
    /// that it's parallel to the display. This means text always renders nicely.
    /// </summary>
    public class Plane : ContentControl
    {
        #region private fields

        /// <summary>
        /// The 3D scene used to render the control.
        /// </summary>
        private Viewport3D _viewport;

        /// <summary>
        /// The camera, which determines the view of the scene.
        /// </summary>
        private PerspectiveCamera _camera;

        /// <summary>
        /// The ContentPresenter used to display content on the front.
        /// </summary>
        private Border _frontContent;

        /// <summary>
        /// The model used to display content on the front.
        /// </summary>
        private Viewport2DVisual3D _frontModel;

        /// <summary>
        /// The ContentPresenter used to display content on the back.
        /// </summary>
        private Border _backContent;

        /// <summary>
        /// The model used to display content on the back.
        /// </summary>
        private Viewport2DVisual3D _backModel;

        /// <summary>
        /// The 3D scale transform, used to keep the content at the right size.
        /// </summary>
        private ScaleTransform3D _scale;

        /// <summary>
        /// The 3D rotation transform, used to rotate the content in 3D.
        /// </summary>
        private RotateTransform3D _rotate;

        /// <summary>
        /// The quaternion component of the 3D rotation.
        /// </summary>
        private QuaternionRotation3D _quaternion;

        /// <summary>
        /// A container for the currently displayed content visual when the plane is rotated parallel to the display.
        /// </summary>
        private Border _fixedContainer;

        /// <summary>
        /// A container for the currently hidden visual when the plane is rotated parallel to the display.
        /// </summary>
        private Border _fixedHiddenContainer;

        /// <summary>
        /// A 2D rotation for the content visuals when they are shown in 2D, so that they match the 3D rotation.
        /// </summary>
        private RotateTransform _fixedTransform;

        /// <summary>
        /// A set of lights which add shading when the object is rotated.
        /// </summary>
        private ModelVisual3D _directionalLights;

        /// <summary>
        /// A set of lights which don't shade the objects.
        /// </summary>
        private ModelVisual3D _ambientLights;

        /// <summary>
        /// Whether the last rotation change had the visual parallel to the display.
        /// </summary>
        private bool _lastOnAxis;

        /// <summary>
        /// Whether the last rotation change had the visual showing its back side.
        /// </summary>
        private bool _lastIsBackShowing;

        /// <summary>
        /// Whether the last rotation change had padding clipping enabled.
        /// </summary>
        private bool _lastIsPaddingClippingEnabled;

        /// <summary>
        /// The content bounds.
        /// </summary>
        private Rect _bounds;

        #endregion

        #region Static lookups

        /// <summary>
        /// Optimization, only create the axis once.
        /// </summary>
        static private readonly Vector3D _axisX = new Vector3D(1, 0, 0);

        /// <summary>
        /// Optimization, only create the axis once.
        /// </summary>
        static private readonly Vector3D _axisY = new Vector3D(0, 1, 0);

        /// <summary>
        /// Optimization, only create the axis once.
        /// </summary>
        static private readonly Vector3D _axisZ = new Vector3D(0, 0, 1);

        #endregion

        #region Setup

        /// <summary>
        /// Initializes static members of the <see cref="Plane"/> class.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Necessary to set DefaultStyleKey."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Sorry, it's big.")]
        static Plane()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Plane), new FrameworkPropertyMetadata(typeof(Plane)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plane"/> class.
        /// </summary>
        public Plane()
        {
            SizeChanged += (sender, e) => UpdateRotation();
            Loaded += (sender, e) => UpdateRotation();
        }

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            _viewport = GetTemplateChild("PART_Viewport") as Viewport3D;
            _camera = GetTemplateChild("PART_Camera") as PerspectiveCamera;

            _frontContent = GetTemplateChild("PART_FrontContent") as Border;
            _frontModel = GetTemplateChild("PART_FrontModel") as Viewport2DVisual3D;
            _backContent = GetTemplateChild("PART_BackContent") as Border;
            _backModel = GetTemplateChild("PART_BackModel") as Viewport2DVisual3D;

            _scale = GetTemplateChild("PART_Scale") as ScaleTransform3D;
            _rotate = GetTemplateChild("PART_Rotate") as RotateTransform3D;
            _quaternion = GetTemplateChild("PART_Quaternion") as QuaternionRotation3D;

            _fixedContainer = GetTemplateChild("PART_FixedContainer") as Border;
            _fixedHiddenContainer = GetTemplateChild("PART_FixedHiddenContainer") as Border;
            _fixedTransform = GetTemplateChild("PART_FixedTransform") as RotateTransform;

            _directionalLights = GetTemplateChild("PART_DirectionalLights") as ModelVisual3D;
            _ambientLights = GetTemplateChild("PART_AmbientLights") as ModelVisual3D;

            SizeChanged += (sender, e) => UpdateBounds();
            _frontContent.SizeChanged += (sender, e) => UpdateBounds();
            _backContent.SizeChanged += (sender, e) => UpdateBounds();

            UpdateBounds();
            UpdateRotation();
            UpdateLights();
            UpdateCacheInvalidationThresholdMaximum();
            UpdateCacheInvalidationThresholdMinimum();

            base.OnApplyTemplate();
        }

        #endregion

        #region Properties

        #region BackContent

        /// <summary>
        /// Gets or sets the content to display on the back.
        /// </summary>
        /// <value>The content to display on the back.</value>
        public object BackContent
        {
            get { return (object)GetValue(BackContentProperty); }
            set { SetValue(BackContentProperty, value); }
        }

        /// <summary>
        /// Backing store for BackContent.
        /// </summary>
        public static readonly DependencyProperty BackContentProperty = DependencyProperty.Register("BackContent", typeof(object), typeof(Plane), new PropertyMetadata(null));

        #endregion

        #region BackContentTemplate

        /// <summary>
        /// Gets or sets the back content template.
        /// </summary>
        /// <value>The back content template.</value>
        public DataTemplate BackContentTemplate
        {
            get { return (DataTemplate)GetValue(BackContentTemplateProperty); }
            set { SetValue(BackContentTemplateProperty, value); }
        }

        /// <summary>
        /// Backing store for BackContentTemplate.
        /// </summary>
        public static readonly DependencyProperty BackContentTemplateProperty = DependencyProperty.Register("BackContentTemplate", typeof(DataTemplate), typeof(Plane), new PropertyMetadata(null));

        #endregion

        #region RotationX

        /// <summary>
        /// Gets or sets the rotation on the X axis.
        /// </summary>
        /// <value>The rotation on the X axis.</value>
        public double RotationX
        {
            get { return (double)GetValue(RotationXProperty); }
            set { SetValue(RotationXProperty, value); }
        }

        /// <summary>
        /// Backing store for RotationX.
        /// </summary>
        public static readonly DependencyProperty RotationXProperty = DependencyProperty.Register(
            "RotationX",
            typeof(double),
            typeof(Plane),
            new FrameworkPropertyMetadata(0.0, (sender, e) => ((Plane)sender).UpdateRotation(), (sender, baseValue) => (double)baseValue % 360));

        #endregion

        #region RotationY

        /// <summary>
        /// Gets or sets the rotation on the Y aYis.
        /// </summary>
        /// <value>The rotation on the Y aYis.</value>
        public double RotationY
        {
            get { return (double)GetValue(RotationYProperty); }
            set { SetValue(RotationYProperty, value); }
        }

        /// <summary>
        /// Backing store for RotationY.
        /// </summary>
        public static readonly DependencyProperty RotationYProperty = DependencyProperty.Register(
            "RotationY",
            typeof(double),
            typeof(Plane),
            new FrameworkPropertyMetadata(0.0, (sender, e) => ((Plane)sender).UpdateRotation(), (sender, baseValue) => (double)baseValue % 360));

        #endregion

        #region RotationZ

        /// <summary>
        /// Gets or sets the rotation on the Z aZis.
        /// </summary>
        /// <value>The rotation on the Z aZis.</value>
        public double RotationZ
        {
            get { return (double)GetValue(RotationZProperty); }
            set { SetValue(RotationZProperty, value); }
        }

        /// <summary>
        /// Backing store for RotationZ.
        /// </summary>
        public static readonly DependencyProperty RotationZProperty = DependencyProperty.Register(
            "RotationZ",
            typeof(double),
            typeof(Plane),
            new FrameworkPropertyMetadata(0.0, (sender, e) => ((Plane)sender).UpdateRotation(), (sender, baseValue) => (double)baseValue % 360));

        #endregion

        #region RotationCenterX

        /// <summary>
        /// Gets or sets the center of the rotation on the X axis.
        /// </summary>
        /// <value>The center of the rotation on the X axis.</value>
        public double RotationCenterX
        {
            get { return (double)GetValue(RotationCenterXProperty); }
            set { SetValue(RotationCenterXProperty, value); }
        }

        /// <summary>
        /// Backing store for RotationCenterX.
        /// </summary>
        public static readonly DependencyProperty RotationCenterXProperty = DependencyProperty.Register(
            "RotationCenterX",
            typeof(double),
            typeof(Plane),
            new PropertyMetadata(0.0, (sender, e) => (sender as Plane).UpdateRotationCenter()));

        #endregion

        #region RotationCenterY

        /// <summary>
        /// Gets or sets the center of the rotation on the X axis.
        /// </summary>
        /// <value>The center of the rotation on the X axis.</value>
        public double RotationCenterY
        {
            get { return (double)GetValue(RotationCenterYProperty); }
            set { SetValue(RotationCenterYProperty, value); }
        }

        /// <summary>
        /// Backing store for RotationCenterY.
        /// </summary>
        public static readonly DependencyProperty RotationCenterYProperty = DependencyProperty.Register(
            "RotationCenterY",
            typeof(double),
            typeof(Plane),
            new PropertyMetadata(0.0, (sender, e) => (sender as Plane).UpdateRotationCenter()));

        #endregion

        #region RotationCenterZ

        /// <summary>
        /// Gets or sets the center of the rotation on the Z aZis.
        /// </summary>
        /// <value>The center of the rotation on the Z aZis.</value>
        public double RotationCenterZ
        {
            get { return (double)GetValue(RotationCenterZProperty); }
            set { SetValue(RotationCenterZProperty, value); }
        }

        /// <summary>
        /// Backing store for RotationCenterZ.
        /// </summary>
        public static readonly DependencyProperty RotationCenterZProperty = DependencyProperty.Register(
            "RotationCenterZ",
            typeof(double),
            typeof(Plane),
            new PropertyMetadata(0.0, (sender, e) => (sender as Plane).UpdateRotationCenter()));

        #endregion

        #region FieldOfView

        /// <summary>
        /// Gets or sets the camera's field of view.
        /// </summary>
        /// <value>The camera's field of view.</value>
        public double FieldOfView
        {
            get { return (double)GetValue(FieldOfViewProperty); }
            set { SetValue(FieldOfViewProperty, value); }
        }

        /// <summary>
        /// Backing store for FieldOfView.
        /// </summary>
        public static readonly DependencyProperty FieldOfViewProperty = DependencyProperty.Register(
            "FieldOfView",
            typeof(double),
            typeof(Plane),
            new FrameworkPropertyMetadata(45.0, (sender, e) => ((Plane)sender).UpdateCamera(), (sender, baseValue) => Math.Min(Math.Max((double)baseValue, 0.5), 179.9)));

        #endregion

        #region UseLights

        /// <summary>
        /// Gets or sets a value indicating whether to use directional lighting.
        /// </summary>
        /// <value><c>true</c> if using directional lighting; otherwise, <c>false</c>.</value>
        public bool UseLights
        {
            get { return (bool)GetValue(UseLightsProperty); }
            set { SetValue(UseLightsProperty, value); }
        }

        /// <summary>
        /// Backing store for UseLights.
        /// </summary>
        public static readonly DependencyProperty UseLightsProperty = DependencyProperty.Register(
            "UseLights",
            typeof(bool),
            typeof(Plane),
            new PropertyMetadata(true, (sender, e) => (sender as Plane).UpdateLights()));

        #endregion

        #region CacheInvalidationThresholdMaximum

        /// <summary>
        /// Gets or sets the cache invalidation threshold maximum.
        /// The CacheInvalidationThresholdMinimum and CacheInvalidationThresholdMaximum property values are relative size values that determine when the TileBrush object should be regenerated due to changes in scale. For example, by setting the CacheInvalidationThresholdMaximum property to 2.0, the cache for the TileBrush only needs to be regenerated when its size exceeds twice the size of the current cache.
        /// http://msdn.microsoft.com/en-us/library/bb613591.aspx
        /// </summary>
        /// <value>The cache invalidation threshold maximum.</value>
        public double CacheInvalidationThresholdMaximum
        {
            get { return (double)GetValue(CacheInvalidationThresholdMaximumProperty); }
            set { SetValue(CacheInvalidationThresholdMaximumProperty, value); }
        }

        /// <summary>
        /// Backing store for CacheInvalidationThresholdMaximum.
        /// </summary>
        public static readonly DependencyProperty CacheInvalidationThresholdMaximumProperty = DependencyProperty.Register(
            "CacheInvalidationThresholdMaximum",
            typeof(double),
            typeof(Plane),
            new PropertyMetadata(2.0, (sender, e) => (sender as Plane).UpdateCacheInvalidationThresholdMaximum()));

        /// <summary>
        /// Updates the cache invalidation threshold maximum.
        /// </summary>
        private void UpdateCacheInvalidationThresholdMaximum()
        {
            if (_frontModel == null || _backModel == null)
            {
                return;
            }

            RenderOptions.SetCacheInvalidationThresholdMaximum(_frontModel, CacheInvalidationThresholdMaximum);
            RenderOptions.SetCacheInvalidationThresholdMaximum(_backModel, CacheInvalidationThresholdMaximum);
        }

        #endregion

        #region CacheInvalidationThresholdMinimum

        /// <summary>
        /// Gets or sets the cache invalidation threshold Minimum.
        /// The CacheInvalidationThresholdMinimum and CacheInvalidationThresholdMinimum property values are relative size values that determine when the TileBrush object should be regenerated due to changes in scale. For example, by setting the CacheInvalidationThresholdMinimum property to 2.0, the cache for the TileBrush only needs to be regenerated when its size exceeds twice the size of the current cache.
        /// http://msdn.microsoft.com/en-us/library/bb613591.aspx
        /// </summary>
        /// <value>The cache invalidation threshold Minimum.</value>
        public double CacheInvalidationThresholdMinimum
        {
            get { return (double)GetValue(CacheInvalidationThresholdMinimumProperty); }
            set { SetValue(CacheInvalidationThresholdMinimumProperty, value); }
        }

        /// <summary>
        /// Backing store for CacheInvalidationThresholdMinimum.
        /// </summary>
        public static readonly DependencyProperty CacheInvalidationThresholdMinimumProperty = DependencyProperty.Register(
            "CacheInvalidationThresholdMinimum",
            typeof(double),
            typeof(Plane),
            new PropertyMetadata(2.0, (sender, e) => (sender as Plane).UpdateCacheInvalidationThresholdMinimum()));

        /// <summary>
        /// Updates the cache invalidation threshold Minimum.
        /// </summary>
        private void UpdateCacheInvalidationThresholdMinimum()
        {
            if (_frontModel == null || _backModel == null)
            {
                return;
            }

            RenderOptions.SetCacheInvalidationThresholdMinimum(_frontModel, CacheInvalidationThresholdMinimum);
            RenderOptions.SetCacheInvalidationThresholdMinimum(_backModel, CacheInvalidationThresholdMinimum);
        }

        #endregion

        #region FrontPadding

        /// <summary>
        /// Gets or sets the thickness applied to the front content. Setting a value here will make the front side appear smaller than the back side.
        /// </summary>
        /// <value>The thickness applied to the front content. Setting a value here will make the front side appear smaller than the back side.</value>
        public Thickness FrontPadding
        {
            get { return (Thickness)GetValue(FrontPaddingProperty); }
            set { SetValue(FrontPaddingProperty, value); }
        }

        /// <summary>
        /// The identifier for the FrontPadding dependency property.
        /// </summary>
        public static readonly DependencyProperty FrontPaddingProperty = DependencyProperty.Register("FrontPadding", typeof(Thickness), typeof(Plane), new PropertyMetadata(new Thickness()));

        #endregion

        #region BackPadding

        /// <summary>
        /// Gets or sets the thickness applied to the back content. Setting a value here will make the back side appear smaller than the front side.
        /// </summary>
        /// <value>The thickness applied to the back content. Setting a value here will make the back side appear smaller than the front side.</value>
        public Thickness BackPadding
        {
            get { return (Thickness)GetValue(BackPaddingProperty); }
            set { SetValue(BackPaddingProperty, value); }
        }

        /// <summary>
        /// The identifier for the BackPadding dependency property.
        /// </summary>
        public static readonly DependencyProperty BackPaddingProperty = DependencyProperty.Register("BackPadding", typeof(Thickness), typeof(Plane), new PropertyMetadata(new Thickness()));

        #endregion

        #region IsPaddingClippingEnabled

        /// <summary>
        /// Gets or sets a value indicating whether the visual should be clipped according to the Front/Back padding properties.
        /// </summary>
        /// <value>Whether the visual should be clipped according to the Front/Back padding properties.</value>
        public bool IsPaddingClippingEnabled
        {
            get { return (bool)GetValue(IsPaddingClippingEnabledProperty); }
            set { SetValue(IsPaddingClippingEnabledProperty, value); }
        }

        /// <summary>
        /// The identifier for the IsPaddingClippingEnabled dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPaddingClippingEnabledProperty = DependencyProperty.Register(
            "IsPaddingClippingEnabled",
            typeof(bool),
            typeof(Plane),
            new PropertyMetadata(true, (sender, e) => (sender as Plane).UpdateRotation()));

        #endregion

        #endregion

        #region Attached Properties

        #region PlaneBackContent

        /// <summary>
        /// Gets the value of the PlaneBackContent attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneBackContent attached property.</returns>
        public static FrameworkElement GetPlaneBackContent(DependencyObject obj)
        {
            if (obj == null)
            {
                return null;
            }

            return (FrameworkElement)obj.GetValue(PlaneBackContentProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneBackContent attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneBackContent attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneBackContent(DependencyObject obj, FrameworkElement value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneBackContentProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneBackContent attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneBackContentProperty = DependencyProperty.RegisterAttached("PlaneBackContent", typeof(FrameworkElement), typeof(Plane), new UIPropertyMetadata(null));

        #endregion

        #region PlaneBackContentTemplate

        /// <summary>
        /// Gets the value of the PlaneBackContentTemplate attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneBackContentTemplate attached property.</returns>
        public static DataTemplate GetPlaneBackContentTemplate(DependencyObject obj)
        {
            if (obj == null)
            {
                return null;
            }

            return (DataTemplate)obj.GetValue(PlaneBackContentTemplateProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneBackContentTemplate attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneBackContentTemplate attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneBackContentTemplate(DependencyObject obj, DataTemplate value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneBackContentTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneBackContentTemplate attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneBackContentTemplateProperty = DependencyProperty.RegisterAttached("PlaneBackContentTemplate", typeof(DataTemplate), typeof(Plane), new UIPropertyMetadata(null));

        #endregion

        #region PlaneRotationX

        /// <summary>
        /// Gets the value of the PlaneRotationX attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneRotationX attached property.</returns>
        public static double GetPlaneRotationX(DependencyObject obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (double)obj.GetValue(PlaneRotationXProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneRotationX attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneRotationX attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneRotationX(DependencyObject obj, double value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneRotationXProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneRotationX attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneRotationXProperty = DependencyProperty.RegisterAttached("PlaneRotationX", typeof(double), typeof(Plane), new UIPropertyMetadata(0.0));

        #endregion

        #region PlaneRotationY

        /// <summary>
        /// Gets the value of the PlaneRotationY attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneRotationY attached property.</returns>
        public static double GetPlaneRotationY(DependencyObject obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (double)obj.GetValue(PlaneRotationYProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneRotationY attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneRotationY attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneRotationY(DependencyObject obj, double value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneRotationYProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneRotationY attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneRotationYProperty = DependencyProperty.RegisterAttached("PlaneRotationY", typeof(double), typeof(Plane), new UIPropertyMetadata(0.0));

        #endregion

        #region PlaneRotationZ

        /// <summary>
        /// Gets the value of the PlaneRotationZ attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneRotationZ attached property.</returns>
        public static double GetPlaneRotationZ(DependencyObject obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (double)obj.GetValue(PlaneRotationZProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneRotationZ attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneRotationZ attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneRotationZ(DependencyObject obj, double value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneRotationZProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneRotationZ attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneRotationZProperty = DependencyProperty.RegisterAttached("PlaneRotationZ", typeof(double), typeof(Plane), new UIPropertyMetadata(0.0));

        #endregion

        #region PlaneRotationCenterX

        /// <summary>
        /// Gets the value of the PlaneRotationCenterX attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneRotationCenterX attached property.</returns>
        public static double GetPlaneRotationCenterX(DependencyObject obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (double)obj.GetValue(PlaneRotationCenterXProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneRotationCenterX attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneRotationCenterX attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneRotationCenterX(DependencyObject obj, double value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneRotationCenterXProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneRotationCenterX attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneRotationCenterXProperty = DependencyProperty.RegisterAttached("PlaneRotationCenterX", typeof(double), typeof(Plane), new UIPropertyMetadata(0.0));

        #endregion

        #region PlaneRotationCenterY

        /// <summary>
        /// Gets the value of the PlaneRotationCenterY attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneRotationCenterY attached property.</returns>
        public static double GetPlaneRotationCenterY(DependencyObject obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (double)obj.GetValue(PlaneRotationCenterYProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneRotationCenterY attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneRotationCenterY attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneRotationCenterY(DependencyObject obj, double value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneRotationCenterYProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneRotationCenterY attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneRotationCenterYProperty = DependencyProperty.RegisterAttached("PlaneRotationCenterY", typeof(double), typeof(Plane), new UIPropertyMetadata(0.0));

        #endregion

        #region PlaneRotationCenterZ

        /// <summary>
        /// Gets the value of the PlaneRotationCenterZ attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneRotationCenterZ attached property.</returns>
        public static double GetPlaneRotationCenterZ(DependencyObject obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (double)obj.GetValue(PlaneRotationCenterZProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneRotationCenterZ attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneRotationCenterZ attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneRotationCenterZ(DependencyObject obj, double value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneRotationCenterZProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneRotationCenterZ attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneRotationCenterZProperty = DependencyProperty.RegisterAttached("PlaneRotationCenterZ", typeof(double), typeof(Plane), new UIPropertyMetadata(0.0));

        #endregion

        #region PlaneFieldOfView

        /// <summary>
        /// Gets the value of the PlaneFieldOfView attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneFieldOfView attached property.</returns>
        public static double GetPlaneFieldOfView(DependencyObject obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (double)obj.GetValue(PlaneFieldOfViewProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneFieldOfView attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneFieldOfView attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneFieldOfView(DependencyObject obj, double value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneFieldOfViewProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneFieldOfView attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneFieldOfViewProperty = DependencyProperty.RegisterAttached("PlaneFieldOfView", typeof(double), typeof(Plane), new UIPropertyMetadata(45.0));

        #endregion

        #region PlaneUseLights

        /// <summary>
        /// Gets the value of the PlaneUseLights attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneUseLights attached property.</returns>
        public static bool GetPlaneUseLights(DependencyObject obj)
        {
            if (obj == null)
            {
                return false;
            }

            return (bool)obj.GetValue(PlaneUseLightsProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneUseLights attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneUseLights attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneUseLights(DependencyObject obj, bool value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneUseLightsProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneUseLights attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneUseLightsProperty = DependencyProperty.RegisterAttached("PlaneUseLights", typeof(bool), typeof(Plane), new UIPropertyMetadata(UseLightsProperty.DefaultMetadata.DefaultValue));

        #endregion

        #region PlaneCacheInvalidationThresholdMaximum

        /// <summary>
        /// Gets the value of the PlaneCacheInvalidationThresholdMaximum attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneCacheInvalidationThresholdMaximum attached property.</returns>
        public static double GetPlaneCacheInvalidationThresholdMaximum(DependencyObject obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (double)obj.GetValue(PlaneCacheInvalidationThresholdMaximumProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneCacheInvalidationThresholdMaximum attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneCacheInvalidationThresholdMaximum attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneCacheInvalidationThresholdMaximum(DependencyObject obj, double value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneCacheInvalidationThresholdMaximumProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneCacheInvalidationThresholdMaximum attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneCacheInvalidationThresholdMaximumProperty = DependencyProperty.RegisterAttached("PlaneCacheInvalidationThresholdMaximum", typeof(double), typeof(Plane), new UIPropertyMetadata(.5));

        #endregion

        #region PlaneCacheInvalidationThresholdMinimum

        /// <summary>
        /// Gets the value of the PlaneCacheInvalidationThresholdMinimum attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneCacheInvalidationThresholdMinimum attached property.</returns>
        public static double GetPlaneCacheInvalidationThresholdMinimum(DependencyObject obj)
        {
            if (obj == null)
            {
                return 0;
            }

            return (double)obj.GetValue(PlaneCacheInvalidationThresholdMinimumProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneCacheInvalidationThresholdMinimum attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneCacheInvalidationThresholdMinimum attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneCacheInvalidationThresholdMinimum(DependencyObject obj, double value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneCacheInvalidationThresholdMinimumProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneCacheInvalidationThresholdMinimum attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneCacheInvalidationThresholdMinimumProperty = DependencyProperty.RegisterAttached("PlaneCacheInvalidationThresholdMinimum", typeof(double), typeof(Plane), new UIPropertyMetadata(2.0));

        #endregion

        #region PlaneFrontPadding

        /// <summary>
        /// Gets the value of the PlaneFrontPadding attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneFrontPadding attached property.</returns>
        public static Thickness GetPlaneFrontPadding(DependencyObject obj)
        {
            if (obj == null)
            {
                return new Thickness();
            }

            return (Thickness)obj.GetValue(PlaneFrontPaddingProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneFrontPadding attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneFrontPadding attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneFrontPadding(DependencyObject obj, Thickness value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneFrontPaddingProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneFrontPadding attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneFrontPaddingProperty = DependencyProperty.RegisterAttached("PlaneFrontPadding", typeof(Thickness), typeof(Plane), new UIPropertyMetadata(new Thickness()));

        #endregion

        #region PlaneBackPadding

        /// <summary>
        /// Gets the value of the PlaneBackPadding attached property.
        /// </summary>
        /// <param name="obj">The element from which to read the property value.</param>
        /// <returns>The value of the PlaneBackPadding attached property.</returns>
        public static Thickness GetPlaneBackPadding(DependencyObject obj)
        {
            if (obj == null)
            {
                return new Thickness();
            }

            return (Thickness)obj.GetValue(PlaneBackPaddingProperty);
        }

        /// <summary>
        /// Sets the value of the PlaneBackPadding attached property.
        /// </summary>
        /// <param name="obj">The element on which to set the PlaneBackPadding attached property.</param>
        /// <param name="value">The property value to set.</param>
        public static void SetPlaneBackPadding(DependencyObject obj, Thickness value)
        {
            if (obj == null)
            {
                return;
            }

            obj.SetValue(PlaneBackPaddingProperty, value);
        }

        /// <summary>
        /// Identifies the PlaneBackPadding attached property.
        /// </summary>
        public static readonly DependencyProperty PlaneBackPaddingProperty = DependencyProperty.RegisterAttached("PlaneBackPadding", typeof(Thickness), typeof(Plane), new UIPropertyMetadata(new Thickness()));

        #endregion

        #endregion

        #region Rendering

        /// <summary>
        /// Updates the bounds.
        /// </summary>
        private void UpdateBounds()
        {
            _bounds = VisualTreeHelper.GetDescendantBounds(_frontContent);
            UpdateClip();
            UpdateRotation();
            UpdateCamera();
            UpdateRotationCenter();
        }

        /// <summary>
        /// Updates the camera.
        /// </summary>
        private void UpdateCamera()
        {
            if (_camera == null)
            {
                return;
            }

            // Math from Planerator. http://blogs.msdn.com/greg_schechter/archive/2007/04/03/camera-construction-in-parallaxui.aspx
            double fovInRadians = FieldOfView * (Math.PI / 180);
            double z = _bounds.Width / Math.Tan(fovInRadians / 2) / 2;
            _camera.Position = new Point3D(_bounds.Width / 2, _bounds.Height / 2, z);
            _camera.FieldOfView = FieldOfView;
            _scale.ScaleX = _bounds.Width;
            _scale.ScaleY = _bounds.Height;
        }

        /// <summary>
        /// Updates the rotation.
        /// </summary>
        private void UpdateRotation()
        {
            if (_frontModel == null || _bounds == Rect.Empty)
            {
                return;
            }

            // Determine if the rotation is parallel to the display.
            bool isOnAxis = IsOnAxis;

            // Determine if we're showing the backside.
            bool isBackShowing = IsBackShowingRotation(RotationX, RotationY, RotationZ);

            if (isOnAxis != _lastOnAxis || IsPaddingClippingEnabled != _lastIsPaddingClippingEnabled)
            {
                UpdateClip();
            }

            if (isOnAxis)
            {
                // Update the fixed transform so it's rotated like the 3D transform.
                if (!_fixedTransform.IsFrozen)
                {
                    _fixedTransform.Angle = isBackShowing ? RotationZ + 180 : -RotationZ;
                    if (Math.Abs(RotationX) == 180)
                    {
                        _fixedTransform.Angle += 180;
                    }
                }
            }
            else
            {
                // Update the rotation of the 3D model.
                UpdateQuaternion();
            }

            if (isBackShowing != _lastIsBackShowing || isOnAxis != _lastOnAxis)
            {
                // Wipe out the current visuals.
                _frontModel.Visual = null;
                _backModel.Visual = null;
                _fixedContainer.Child = null;
                _fixedHiddenContainer.Child = null;

                if (isOnAxis)
                {
                    // The rotation is parallel to the display, so show the fixed version of the content.
                    _fixedContainer.Child = isBackShowing ? _backContent : _frontContent;
                    _fixedHiddenContainer.Child = isBackShowing ? _frontContent : _backContent;
                }
                else
                {
                    // Decide which visual to be showing.
                    if (isBackShowing)
                    {
                        _backModel.Visual = _backContent;
                    }
                    else
                    {
                        _frontModel.Visual = _frontContent;
                    }
                }

                if (isOnAxis != _lastOnAxis)
                {
                    UpdateRotationCenter();
                }
            }

            _lastOnAxis = isOnAxis;
            _lastIsBackShowing = isBackShowing;
            _lastIsPaddingClippingEnabled = IsPaddingClippingEnabled;
        }

        /// <summary>
        /// If on axis, clip the visual so that it's not hit testable outside of any padding applied to the sides.
        /// </summary>
        private void UpdateClip()
        {
            // Determine if the rotation is parallel to the display.
            bool isOnAxis = IsOnAxis;

            // Determine if we're showing the backside.
            bool isBackShowing = IsBackShowingRotation(RotationX, RotationY, RotationZ);

            if (isOnAxis && IsPaddingClippingEnabled)
            {
                Rect bounds = isBackShowing ? VisualTreeHelper.GetDescendantBounds(_backContent) : _bounds;
                FrameworkElement content = isBackShowing ? _backContent : _frontContent;
                if (bounds != Rect.Empty)
                {
                    Thickness padding = isBackShowing ? BackPadding : FrontPadding;

                    // bounds = new Rect(bounds.X + padding.Left, bounds.Y + padding.Top, bounds.Width - padding.Left - padding.Right, bounds.Height - padding.Bottom - padding.Top);
                    // There's some glitch here. The above line should work, but bounds.Width/Height is occasionally wrong. Intermittent bug.
                    bounds = new Rect(bounds.X + padding.Left, bounds.Y + padding.Top, content.ActualWidth - padding.Left - padding.Right, content.ActualHeight - padding.Bottom - padding.Top);
                    Clip = new RectangleGeometry { Rect = bounds };
                }
            }
            else
            {
                Clip = null;
            }
        }

        /// <summary>
        /// Updates the quaternion.
        /// </summary>
        private void UpdateQuaternion()
        {
            if (_quaternion == null)
            {
                return;
            }

            Quaternion qx = new Quaternion(_axisX, RotationX);
            Quaternion qy = new Quaternion(_axisY, RotationY);
            Quaternion qz = new Quaternion(_axisZ, RotationZ);
            _quaternion.Quaternion = qx * qy * qz;
        }

        /// <summary>
        /// Updates the rotation center.
        /// </summary>
        private void UpdateRotationCenter()
        {
            if (_fixedTransform == null || _rotate == null)
            {
                return;
            }

            // Determine if the rotation is parallel to the display.
            bool isOnAxis = IsOnAxis;

            if (isOnAxis)
            {
                _fixedTransform.CenterX = (_bounds.Width / 2) + RotationCenterX;
                _fixedTransform.CenterY = (_bounds.Height / 2) - RotationCenterY;
            }
            else
            {
                _rotate.CenterX = (_bounds.Width / 2) + RotationCenterX;
                _rotate.CenterY = (_bounds.Height / 2) - RotationCenterY;
                _rotate.CenterZ = RotationCenterZ;
            }
        }

        /// <summary>
        /// Updates the lights.
        /// </summary>
        private void UpdateLights()
        {
            if (_viewport == null)
            {
                return;
            }

            _viewport.Children.Remove(_ambientLights);
            _viewport.Children.Remove(_directionalLights);
            _viewport.Children.Insert(0, UseLights ? _directionalLights : _ambientLights);
        }

        /// <summary>
        /// Gets a value indicating whether this instance is rotated to be parallel to the display.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is on axis; otherwise, <c>false</c>.
        /// </value>
        private bool IsOnAxis
        {
            get
            {
                return RotationX % 180 == 0 && RotationY % 180 == 0 && RotationZ % 180 == 0;
            }
        }

        /// <summary>
        /// Determines whether the back of the plane is showing, given a rotation.
        /// Credit to Joel Pryde.
        /// </summary>
        /// <param name="x">The x rotation.</param>
        /// <param name="y">The y rotation.</param>
        /// <param name="z">The z rotation.</param>
        /// <returns>
        /// <c>true</c> if the back of the plane is showing, given a rotation; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsBackShowingRotation(double x, double y, double z)
        {
            Matrix3D rotMatrix = new Matrix3D();
            rotMatrix.Rotate(new Quaternion(new Vector3D(1, 0, 0), x));
            rotMatrix.Rotate(new Quaternion(new Vector3D(0, 1, 0) * rotMatrix, y));
            rotMatrix.Rotate(new Quaternion(new Vector3D(0, 0, 1) * rotMatrix, z));

            Vector3D transformZ = rotMatrix.Transform(new Vector3D(0, 0, 1));
            return Vector3D.DotProduct(new Vector3D(0, 0, 1), transformZ) < 0;
        }

        #endregion
    }
}
