using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using FeedProcessor;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;
using SocialStream.Controls.RiverContent;
using SocialStream.Helpers;
using SocialStream.Properties;

namespace SocialStream.Controls
{
    /// <summary>
    /// The item contained within a ScatterViewItem when removed from the river.
    /// </summary>
    internal class RemovedContentItem : RiverItemBase
    {
        #region Private fields

        /// <summary>
        /// The first half of the flash animation.
        /// </summary>
        private Storyboard _flashOn;

        /// <summary>
        /// The second half of the flash animation.
        /// </summary>
        private Storyboard _flashOff;

        /// <summary>
        /// The content to display when the first half of the flash animation completes.
        /// </summary>
        private UserControl _nextState;

        /// <summary>
        /// The current item being rendered.
        /// </summary>
        private FeedItem _feedItem;

        /// <summary>
        /// The renderer for ImageFeedItems when removed from the river.
        /// </summary>
        private LargeImage _largeImage;

        /// <summary>
        /// The renderer for NewsFeedItems when removed from the river.
        /// </summary>
        private LargeNews _largeNews;

        /// <summary>
        /// The renderer for StatusFeedItems when removed from the river.
        /// </summary>
        private LargeTweet _largeTweet;

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

        #endregion

        /// <summary>
        /// When overridden in a derived class, is invoked whenever application code or internal processes call <see cref="M:System.Windows.FrameworkElement.ApplyTemplate"/>.
        /// </summary>
        public override void OnApplyTemplate()
        {
            _flashOn = (Template.Resources["FlashOn"] as Storyboard).Clone();
            _flashOn.Completed += Flash_Completed;
            _flashOff = (Template.Resources["FlashOff"] as Storyboard).Clone();
            _largeImage = GetTemplateChild("PART_LargeImage") as LargeImage;
            _largeNews = GetTemplateChild("PART_LargeNews") as LargeNews;
            _largeTweet = GetTemplateChild("PART_LargeTweet") as LargeTweet;
            _smallImage = GetTemplateChild("PART_SmallImage") as SmallImage;
            _smallNews = GetTemplateChild("PART_SmallNews") as SmallNews;
            _smallTweet = GetTemplateChild("PART_SmallTweet") as SmallTweet;

            base.OnApplyTemplate();
        }

        #region Backside

        /// <summary>
        /// Gets or sets the element that's on the back of this river item.
        /// </summary>
        /// <value>The element that's on the back of this river item.</value>
        public FrameworkElement Backside
        {
            get { return (FrameworkElement)GetValue(BacksideProperty); }
            set { SetValue(BacksideProperty, value); }
        }

        /// <summary>
        /// The identifier for the Backside dependency property.
        /// </summary>
        public static readonly DependencyProperty BacksideProperty = DependencyProperty.Register("Backside", typeof(FrameworkElement), typeof(RemovedContentItem), new PropertyMetadata(null));

        #endregion

        #region RiverItemBase

        /// <summary>
        /// Called by the river when the item should retrieve data from its data source.
        /// </summary>
        /// <param name="state">The state.</param>
        /// <param name="maintainUnblockedData">if set to <c>true</c> [maintain unblocked data].</param>
        /// <returns>
        /// The data that this item will render. If null, the item won't be shown.
        /// </returns>
        internal override object GetData(RiverItemState state, bool maintainUnblockedData)
        {
            // Always returns null. Only RenderData is called for removed items.
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

                if (Backside != null)
                {
                    TagBack tag = Backside.FindVisualChild<TagBack>();
                    if (tag != null)
                    {
                        tag.DataContext = DataContext;
                        if (_feedItem == null)
                        {
                            tag.TagName = string.Empty;
                        }
                        else
                        {
                            // Force mobile.twitter.com, because twitter doesn't redirect to the mobile site properly on Windows Mobile 6 devices
                            tag.TagName = _feedItem.Uri.OriginalString.Replace("twitter.com", "mobile.twitter.com");
                        }
                    }
                }
            }

            GoToState(visualState);

            return finalData;
        }

        /// <summary>
        /// Called by the river when this item is being hidden because it scrolled out of view.
        /// </summary>
        internal override void Cleanup()
        {
            _feedItem = null;
            DataContext = null;
        }

        /// <summary>
        /// Called when the item is removed from the river by the user.
        /// </summary>
        /// <returns>Sizing restrictions for the item once its in the river.</returns>
        internal override RiverSize Removed()
        {
            _flashOn.Begin(this, Template);
            _nextState = null;

            RiverSize riverSize = new RiverSize { MaxSize = DesiredSize, RemovedSize = DesiredSize, MinSize = DesiredSize };

            if (_feedItem is ImageFeedItem)
            {
                double longestWidth = Settings.Default.MinImageSize.Width + ((Settings.Default.MaxImageSize.Width - Settings.Default.MinImageSize.Width) / 2);
                double longestHeight = Settings.Default.MinImageSize.Height + ((Settings.Default.MaxImageSize.Height - Settings.Default.MinImageSize.Height) / 2);
                double longestSide = Math.Min(longestWidth, longestHeight) + (Math.Abs(longestWidth - longestHeight) / 2);

                // Grow to a size that's the same aspect ratio as the image, but clamped to longestSide.
                riverSize.RemovedSize = (_feedItem as ImageFeedItem).ThumbnailSize;

                if (riverSize.RemovedSize.Width == 0 && riverSize.RemovedSize.Height == 0)
                {
                    // Workaround for the thumbnail size sometimes not being set. This is a bug.
                    riverSize.RemovedSize = new Size(1, 1);
                }

                if (riverSize.RemovedSize.Width > riverSize.RemovedSize.Height)
                {
                    riverSize.RemovedSize = new Size(longestSide, riverSize.RemovedSize.Height / riverSize.RemovedSize.Width * longestSide);
                }
                else
                {
                    riverSize.RemovedSize = new Size(riverSize.RemovedSize.Width / riverSize.RemovedSize.Height * longestSide, longestSide);
                }

                riverSize.MinSize = Settings.Default.MinImageSize;
                riverSize.MaxSize = Settings.Default.MaxImageSize;

                _nextState = _largeImage;
            }
            else if (_feedItem is NewsFeedItem)
            {
                _nextState = _largeNews;
                riverSize.MinSize = Settings.Default.MinNewsSize;
                riverSize.MaxSize = Settings.Default.MaxNewsSize;
                riverSize.RemovedSize = riverSize.MinSize;
            }
            else if (_feedItem is StatusFeedItem)
            {
                _nextState = _largeTweet;
                riverSize.MinSize = Settings.Default.MinStatusSize;
                riverSize.MaxSize = Settings.Default.MaxStatusSize;
                riverSize.RemovedSize = riverSize.MinSize;
            }

            return riverSize;
        }

        /// <summary>
        /// Called when the item has been removed from the river and its growth animation has completed.
        /// </summary>
        internal override void RemoveFinished()
        {
            if (_largeNews.Visibility == Visibility.Visible)
            {
                _largeNews.SetUpResizing();
            }

            if (_largeTweet.Visibility == Visibility.Visible)
            {
                _largeTweet.SetUpResizing();
            }
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
                _largeNews.TearDownResizing();
            }
            else if (_feedItem is StatusFeedItem)
            {
                _nextState = _smallTweet;
                _largeTweet.TearDownResizing();
            }

            _flashOn.Begin(this, Template);
        }

        #endregion

        /// <summary>
        /// When the "flash" animation completes, populate the new content and play the second half of the animation.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void Flash_Completed(object sender, EventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            GoToState(_nextState);
            _flashOff.Begin(this, Template);
            _nextState = null;
        }

        /// <summary>
        /// Toggle the visibility of the content renderers.
        /// This should probably be done with VSM, but I always have trouble with it in WPF custom controls...
        /// </summary>
        /// <param name="state">The content renderer to show.</param>
        private void GoToState(UserControl state)
        {
            _largeImage.Visibility = state == _largeImage ? Visibility.Visible : Visibility.Hidden;
            _largeNews.Visibility = state == _largeNews ? Visibility.Visible : Visibility.Hidden;
            _largeTweet.Visibility = state == _largeTweet ? Visibility.Visible : Visibility.Hidden;
            _smallImage.Visibility = state == _smallImage ? Visibility.Visible : Visibility.Hidden;
            _smallNews.Visibility = state == _smallNews ? Visibility.Visible : Visibility.Hidden;
            _smallTweet.Visibility = state == _smallTweet ? Visibility.Visible : Visibility.Hidden;

            if (state != null)
            {
                state.DataContext = _feedItem;
            }

            _largeImage.RenderContent();
            _largeNews.RenderContent();
        }
    }
}
