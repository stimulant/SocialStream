using System;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using FeedProcessor;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;
using Microsoft.Surface.Presentation.Controls;
using SocialStream.Controls.RiverContent;
using SocialStream.Helpers;
using SocialStream.Properties;

namespace SocialStream.Controls
{
    /// <summary>
    /// Represents a content item in the river. Manages the display of content within itself and transitions between large and small content.
    /// </summary>
    internal class RiverContentItem : RiverItemBase
    {
        #region Private fields

        /// <summary>
        /// The content to display when the first half of the flash animation completes.
        /// </summary>
        private UserControl _nextState;

        /// <summary>
        /// The current item being rendered.
        /// </summary>
        private FeedItem _feedItem;

        /// <summary>
        /// The renderer for ImageFeedItems when in the river.
        /// </summary>
        private SmallImage _smallImage;

        /// <summary>
        /// The renderer for NewsFeedItems when in the river.
        /// </summary>
        private SmallNews _smallNews;

        /// <summary>
        /// The renderer for StatusFeedItems when in the river.
        /// </summary>
        private SmallTweet _smallTweet;

        /// <summary>
        /// The button to ban a user.
        /// </summary>
        private SurfaceButton _banBtn;

        /// <summary>
        /// The button to delete a specific item.
        /// </summary>
        private SurfaceButton _deleteBtn;

        /// <summary>
        /// An animation shown for especially new items.
        /// </summary>
        private Storyboard _breaking;

        /// <summary>
        /// The ItemProxy that this item is a part of.
        /// </summary>
        private ItemProxy _parentProxy;

        #endregion

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            _smallImage = GetTemplateChild("PART_SmallImage") as SmallImage;
            _smallNews = GetTemplateChild("PART_SmallNews") as SmallNews;
            _smallTweet = GetTemplateChild("PART_SmallTweet") as SmallTweet;
            _banBtn = GetTemplateChild("PART_BanBtn") as SurfaceButton;
            _deleteBtn = GetTemplateChild("PART_DeleteBtn") as SurfaceButton;

            _banBtn.Click += AdminBtn_Click;
            _deleteBtn.Click += AdminBtn_Click;

            _breaking = (Template.Resources["Breaking"] as Storyboard).Clone();

            _parentProxy = this.FindVisualParent<ItemProxy>();

            if (_parentProxy != null)
            {
                _smallImage.Image.ImageLoaded += (sender, e) => _parentProxy.IsEnabled = true;
            }

            base.OnApplyTemplate();
        }

        #region RiverItemBase

        /// <summary>
        /// Called by the river when the item should retrieve data from its data source.
        /// </summary>
        /// <param name="state">A definition which describes the size and position of this item in the river.</param>
        /// <param name="maintainUnblockedData">if set to <c>true</c> [maintain unblocked data].</param>
        /// <returns>
        /// The data that this item will render. If null, the item won't be shown.
        /// </returns>
        internal override object GetData(RiverItemState state, bool maintainUnblockedData)
        {
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                return new object();
            }
            else if (AppState.Instance.FeedProcessor != null)
            {
                if (maintainUnblockedData && _feedItem != null && _feedItem.BlockReason == BlockReason.None)
                {
                    return _feedItem;
                }

                FeedItem feedItem = null;

                if (state.RowSpan == state.ColumnSpan)
                {
                    // If it's square, be an image.
                    feedItem = AppState.Instance.FeedProcessor.GetNextItem(ContentType.Image);
                }
                else
                {
                    // If not, be twitter or news.
                    feedItem = AppState.Instance.FeedProcessor.GetNextItem(ContentType.Status | ContentType.News);
                }

                return feedItem;
            }

            return null;
        }

        /// <summary>
        /// Called by the river when the item should render some data.
        /// </summary>
        /// <param name="state">A definition which describes the size and position of this item in the river.</param>
        /// <param name="data">The data that the river is requesting to be rendered. The item can override this and return different data if needed.</param>
        /// <returns>
        /// The data that this item will render. If null, the item won't be shown.
        /// </returns>
        internal override object RenderData(RiverItemState state, object data)
        {
            UserControl visualState = null;
            object finalData = data;

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                if (state.RowSpan == state.ColumnSpan)
                {
                    visualState = _smallImage;
                }
                else if (App.Random.NextDouble() > .5)
                {
                    visualState = _smallNews;
                }
                else
                {
                    visualState = _smallTweet;
                }
            }
            else if (AppState.Instance.IsInitialized)
            {
                _feedItem = data as FeedItem;

                if (_feedItem != null && _feedItem.BlockReason != BlockReason.None)
                {
                    // This object is being rendered from history, but it got blocked since it was last displayed. Reject this content and get new content.
                    finalData = _feedItem = GetData(state, false) as FeedItem;
                }

                if (_feedItem is ImageFeedItem)
                {
                    // visualState = removed ? _largeImage as UserControl : _smallImage as UserControl;
                    visualState = _smallImage as UserControl;
                }
                else if (_feedItem is NewsFeedItem)
                {
                    // visualState = removed ? _largeNews as UserControl : _smallNews as UserControl;
                    visualState = _smallNews as UserControl;
                }
                else if (_feedItem is StatusFeedItem)
                {
                    // visualState = removed ? _largeTweet as UserControl : _smallTweet as UserControl;
                    visualState = _smallTweet as UserControl;
                }

                DataContext = _feedItem;

                if (_feedItem is ImageFeedItem && _parentProxy != null)
                {
                    // Disable the item while it's loading.
                    _parentProxy.IsEnabled = _smallImage.Image.BitmapImage != null;
                }

                _banBtn.Visibility = _feedItem != null && !string.IsNullOrEmpty(_feedItem.Author) ? Visibility.Visible : Visibility.Hidden;
            }

            GoToState(visualState);

            if (_feedItem != null && DateTime.Now - _feedItem.Date <= Settings.Default.NewItemAlert)
            {
                // Show the "breaking" animation for new items.
                _breaking.Begin(this, Template, true);
            }

            return finalData;
        }

        /// <summary>
        /// Called by the river when this item is being hidden because it scrolled out of view.
        /// </summary>
        internal override void Cleanup()
        {
            _feedItem = null;
            _breaking.Stop(this);
            DataContext = null;
        }

        /// <summary>
        /// Called when the item is removed from the river by the user.
        /// </summary>
        /// <returns>Sizing restrictions for the item once its in the river.</returns>
        internal override RiverSize Removed()
        {
            RiverSize riverSize = new RiverSize { MaxSize = DesiredSize, RemovedSize = DesiredSize, MinSize = DesiredSize };
            return riverSize;
        }

        /// <summary>
        /// Called when the item has been removed from the river and its growth animation has completed.
        /// </summary>
        internal override void RemoveFinished()
        {
        }

        /// <summary>
        /// Called when the item is added back to the river due to a timeout.
        /// </summary>
        internal override void Added()
        {
            _nextState = null;
            if (_feedItem is ImageFeedItem)
            {
                _nextState = _smallImage;
            }
            else if (_feedItem is NewsFeedItem)
            {
                _nextState = _smallNews;
            }
            else if (_feedItem is StatusFeedItem)
            {
                _nextState = _smallTweet;
            }

            if (_feedItem != null && DateTime.Now - _feedItem.Date <= Settings.Default.NewItemAlert)
            {
                // Show the "breaking" animation for new items.
                _breaking.Begin(this, Template, true);
            }
        }

        #endregion

        /// <summary>
        /// When the "flash" animation completes, populate the new content and play the second half of the animation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Flash_Completed(object sender, EventArgs e)
        {
            GoToState(_nextState);
            _nextState = null;
        }

        /// <summary>
        /// Toggle the visibility of the content renderers.
        /// This should probably be done with VSM, but I always have trouble with it in WPF custom controls...
        /// </summary>
        /// <param name="state">The content renderer to show.</param>
        private void GoToState(UserControl state)
        {
            _smallImage.Visibility = state == _smallImage ? Visibility.Visible : Visibility.Hidden;
            _smallNews.Visibility = state == _smallNews ? Visibility.Visible : Visibility.Hidden;
            _smallTweet.Visibility = state == _smallTweet ? Visibility.Visible : Visibility.Hidden;

            if (state != null)
            {
                state.DataContext = _feedItem;
            }
        }

        #region IsAdminTagPresent

        /// <summary>
        /// Gets or sets a value indicating whether the admin tag is present.
        /// </summary>
        /// <value>
        /// <c>true</c> if the admin tag is present; otherwise, <c>false</c>.
        /// </value>
        public bool IsAdminTagPresent
        {
            get { return (bool)GetValue(IsAdminTagPresentProperty); }
            set { SetValue(IsAdminTagPresentProperty, value); }
        }

        /// <summary>
        /// Identifies the IsAdminTagPresent dependency property.
        /// </summary>
        public static readonly DependencyProperty IsAdminTagPresentProperty = DependencyProperty.Register("IsAdminTagPresent", typeof(bool), typeof(RiverContentItem), new PropertyMetadata(false));

        #endregion

        /// <summary>
        /// Handles the Click event of the admin buttons. Adds a ban to the FeedProcessor queries.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.RoutedEventArgs"/> instance containing the event data.</param>
        private void AdminBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_feedItem == null || !AppState.Instance.IsInitialized)
            {
                return;
            }

            if (sender == _deleteBtn)
            {
                AppState.Instance.AddBan(_feedItem.SourceType, string.Format(CultureInfo.InvariantCulture, "{0}", _feedItem.Uri.AbsoluteUri));
            }
            else if (sender == _banBtn)
            {
                AppState.Instance.AddBan(_feedItem.SourceType, string.Format(CultureInfo.InvariantCulture, "{0}{1}", Processor.AuthorQueryMarker, _feedItem.Author));
            }

            RaiseEvent(new RoutedEventArgs(RiverItemBase.RefreshRequestedEvent));
        }
    }
}
