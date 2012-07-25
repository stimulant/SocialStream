using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Threading;
using FeedProcessor;
using FeedProcessor.Enums;
using SocialStream.Controls;
using SocialStream.Properties;

namespace SocialStream
{
    /// <summary>
    /// Maintains the overall state of the application, and syncronizes between the app state and the feed processor.
    /// </summary>
    internal class AppState : INotifyPropertyChanged
    {
        /// <summary>
        /// How many feeds have updated for the first time.
        /// </summary>
        private int _feedsLoaded = 0;

        /// <summary>
        /// Backing store for Instance.
        /// </summary>
        private static AppState _instance;

        /// <summary>
        /// Gets the singleton instance of this class.
        /// </summary>
        /// <value>The singleton instance of this class.</value>
        internal static AppState Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new AppState();
                }

                return _instance;
            }
        }

        /// <summary>
        /// Initializes the AppState.
        /// </summary>
        /// <param name="river">The river instance which will have its cache purged when the FeedProcessor does same.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework", MessageId = "System.Collections.ObjectModel.ObservableCollection`1<System.String>.#.ctor(System.Collections.Generic.IEnumerable`1<System.String>)", Justification = "Not worried about old .NET versions."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework", MessageId = "System.Windows.Threading.Dispatcher.#Invoke(System.Delegate,System.Object[])", Justification = "Not worried about old .NET versions.")]
        internal void Initialize(River river)
        {
            AutoScrollSpeed = Math.Abs(Settings.Default.AutoScrollSpeed);
            AutoScrollDirection = Math.Sign(Settings.Default.AutoScrollDirection);
            IsProfanityFilterEnabled = Settings.Default.IsProfanityFilterEnabled;
            RetrievalOrder = Settings.Default.RetrievalOrder;

            // Set up the FeedProcessor on a background thread.
            Thread thread = new Thread(() =>
            {
                ObservableCollection<string> flickrQuery = new ObservableCollection<string>(Split(Settings.Default.FlickrQuery.Trim()));
                Split(Settings.Default.FlickrBans.Trim()).ToList().ForEach(ban => flickrQuery.Add(Processor.NegativeQueryMarker + ban));

                ObservableCollection<string> newsQuery = new ObservableCollection<string>(Split(Settings.Default.NewsQuery.Trim()));
                Split(Settings.Default.NewsBans.Trim()).ToList().ForEach(ban => newsQuery.Add(Processor.NegativeQueryMarker + ban));

                ObservableCollection<string> twitterQuery = new ObservableCollection<string>(Split(Settings.Default.TwitterQuery.Trim()));
                Split(Settings.Default.TwitterBans.Trim()).ToList().ForEach(ban => twitterQuery.Add(Processor.NegativeQueryMarker + ban));

                FeedProcessor = new FeedProcessor.Processor(
                    Settings.Default.FlickrApiKey,
                    Settings.Default.FlickrPollInterval,
                    Settings.Default.TwitterPollInterval,
                    Settings.Default.NewsPollInterval,
                    Settings.Default.MinFeedItemDate)
                {
                    Profanity = new ObservableCollection<string>(File.Exists("Profanity.txt") ? File.ReadAllLines("Profanity.txt") : new string[0]),
                    DistributeContentEvenly = Settings.Default.DistributeContentEvenly,
                    IsProfanityFilterEnabled = IsProfanityFilterEnabled,
                    RetrievalOrder = RetrievalOrder,
                    FlickrQuery = flickrQuery,
                    NewsQuery = newsQuery,
                    TwitterQuery = twitterQuery
                };

                IsContentLoaded = FeedProcessor.FeedCount == 0;
                var uiDisp = Dispatcher.CurrentDispatcher;
                FeedProcessor.CachePurged += (sender, e) => uiDisp.Invoke(new Action(() => { river.PurgeHistory(e.ValidData); }));
                FeedProcessor.FeedUpdated += FeedProcessor_FeedUpdated;

                IsInitialized = true;
                IsPaused = false;

                // Cache the tag client, otherwise the first instance takes a while to start up.
                using (MicrosoftTagService.MIBPContractClient tagClient = new MicrosoftTagService.MIBPContractClient())
                {
                }
            });
            thread.Priority = ThreadPriority.Lowest;
            thread.Start();
        }

        /// <summary>
        /// Keep track of how many feeds have completed their first run. We wait until all of them have updated once before beginning the stream.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void FeedProcessor_FeedUpdated(object sender, EventArgs e)
        {
            _feedsLoaded++;
            IsContentLoaded = _feedsLoaded > 0;

            if (IsContentLoaded)
            {
                FeedProcessor.FeedUpdated -= FeedProcessor_FeedUpdated;
            }
        }

        /// <summary>
        /// Backing store for IsContentLoaded.
        /// </summary>
        private bool _isContentLoaded;

        /// <summary>
        /// Gets a value indicating whether each feed has loaded at least once.
        /// </summary>
        /// <value>
        /// <c>true</c> if each feed has loaded at least once; otherwise, <c>false</c>.
        /// </value>
        public bool IsContentLoaded
        {
            get
            {
                return _isContentLoaded;
            }

            private set
            {
                _isContentLoaded = value;
                NotifyPropertyChanged("IsContentLoaded");
            }
        }

        /// <summary>
        /// Shortcut for splitting a comma-separated list.
        /// </summary>
        /// <param name="list">The comma-separated list.</param>
        /// <returns>The list split into a string array.</returns>
        private static string[] Split(string list)
        {
            return list.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Backing store for FeedProcessor.
        /// </summary>
        private FeedProcessor.Processor _feedProcessor;

        /// <summary>
        /// Gets the feed processor.
        /// </summary>
        /// <value>The feed processor.</value>
        public FeedProcessor.Processor FeedProcessor
        {
            get
            {
                return _feedProcessor;
            }

            private set
            {
                _feedProcessor = value;
                NotifyPropertyChanged("FeedProcessor");
            }
        }

        /// <summary>
        /// Backing store for IsInitialized.
        /// </summary>
        private bool _isInitialized;

        /// <summary>
        /// Gets a value indicating whether this instance is initialized.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is initialized; otherwise, <c>false</c>.
        /// </value>
        public bool IsInitialized
        {
            get
            {
                return _isInitialized;
            }

            private set
            {
                _isInitialized = value;
                NotifyPropertyChanged("IsInitialized");
            }
        }

        /// <summary>
        /// Backing store for IsPaused.
        /// </summary>
        private bool _isPaused = true;

        /// <summary>
        /// Gets or sets a value indicating whether this instance is paused.
        /// </summary>
        /// <value><c>true</c> if this instance is paused; otherwise, <c>false</c>.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used in binding.")]
        public bool IsPaused
        {
            get
            {
                return _isPaused;
            }

            set
            {
                _isPaused = value;

                if (_feedProcessor != null)
                {
                    if (_isPaused)
                    {
                        _feedProcessor.Stop();
                    }
                    else
                    {
                        _feedProcessor.Start();
                    }
                }

                AutoScrollSpeed = _isPaused ? 0 : Settings.Default.AutoScrollSpeed * AutoScrollDirection;

                NotifyPropertyChanged("IsPaused");
            }
        }

        /// <summary>
        /// Backing store for AutoScrollSpeed.
        /// </summary>
        private double _autoScrollSpeed = 84;

        /// <summary>
        /// Gets or sets the auto scroll speed.
        /// </summary>
        /// <value>The auto scroll speed.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used in binding.")]
        public double AutoScrollSpeed
        {
            get
            {
                return _autoScrollSpeed;
            }

            set
            {
                _autoScrollSpeed = value;
                NotifyPropertyChanged("AutoScrollSpeed");
            }
        }

        /// <summary>
        /// Backing store for AutoScrollDirection.
        /// </summary>
        private int _autoScrollDirection = 1;

        /// <summary>
        /// Gets or sets the auto scroll Direction.
        /// </summary>
        /// <value>The auto scroll Direction.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used in binding.")]
        public int AutoScrollDirection
        {
            get
            {
                return _autoScrollDirection;
            }

            set
            {
                _autoScrollDirection = value;
                NotifyPropertyChanged("AutoScrollDirection");
                Settings.Default.AutoScrollDirection = value;
                Settings.Default.Save();

                if (_autoScrollDirection == 0)
                {
                    AutoScrollSpeed = 0;
                }
                else if (_autoScrollDirection > 0)
                {
                    AutoScrollSpeed = Math.Abs(Settings.Default.AutoScrollSpeed);
                }
                else if (_autoScrollDirection < 0)
                {
                    AutoScrollSpeed = Math.Abs(Settings.Default.AutoScrollSpeed) * -1;
                }
            }
        }

        /// <summary>
        /// Backing store for IsAdminTagPresent.
        /// </summary>
        private bool _isAdminTagPresent;

        /// <summary>
        /// Gets or sets a value indicating whether the admin tag is present on the Surface display.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is admin tag present; otherwise, <c>false</c>.
        /// </value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Used in binding.")]
        public bool IsAdminTagPresent
        {
            get
            {
                return _isAdminTagPresent;
            }

            set
            {
                _isAdminTagPresent = value;
                NotifyPropertyChanged("IsAdminTagPresent");
            }
        }

        /// <summary>
        /// Backing store for IsProfanityFilterEnabled.
        /// </summary>
        private bool _isProfanityFilterEnabled;

        /// <summary>
        /// Gets or sets a value indicating whether the profanity filter is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if the profanity filter is enabled; otherwise, <c>false</c>.
        /// </value>
        public bool IsProfanityFilterEnabled
        {
            get
            {
                return _isProfanityFilterEnabled;
            }

            set
            {
                _isProfanityFilterEnabled = value;
                if (_feedProcessor != null)
                {
                    _feedProcessor.IsProfanityFilterEnabled = value;
                }

                Settings.Default.IsProfanityFilterEnabled = value;
                NotifyPropertyChanged("IsProfanityFilterEnabled");
                Settings.Default.Save();
            }
        }

        /// <summary>
        /// Backing store for RetrievalOrder.
        /// </summary>
        private RetrievalOrder _retrievalOrder;

        /// <summary>
        /// Gets or sets the retrieval order.
        /// </summary>
        /// <value>The retrieval order.</value>
        public RetrievalOrder RetrievalOrder
        {
            get
            {
                return _retrievalOrder;
            }

            set
            {
                _retrievalOrder = value;
                if (_feedProcessor != null)
                {
                    _feedProcessor.RetrievalOrder = value;
                }

                Settings.Default.RetrievalOrder = value;
                NotifyPropertyChanged("RetrievalOrder");
                Settings.Default.Save();
            }
        }

        #region INotifyPropertyChanged

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Helper method to fire the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged == null)
            {
                return;
            }

            PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        /// <summary>
        /// Adds a banned query term to the processor.
        /// </summary>
        /// <param name="sourceType">The source type this ban applies to.</param>
        /// <param name="term">The banned term.</param>
        internal void AddBan(SourceType sourceType, string term)
        {
            switch (sourceType)
            {
                case SourceType.Flickr:
                    FeedProcessor.FlickrQuery.Add(Processor.NegativeQueryMarker + term);
                    Settings.Default.FlickrBans += "," + term;
                    break;

                case SourceType.News:
                    FeedProcessor.NewsQuery.Add(Processor.NegativeQueryMarker + term);
                    Settings.Default.NewsBans += "," + term;
                    break;

                case SourceType.Twitter:
                    FeedProcessor.TwitterQuery.Add(Processor.NegativeQueryMarker + term);
                    Settings.Default.TwitterBans += "," + term;
                    break;
            }

            Settings.Default.Save();
        }

        /// <summary>
        /// Removes the bans.
        /// </summary>
        internal void RemoveBans()
        {
            Split(Settings.Default.FlickrBans).ToList().ForEach(b => FeedProcessor.FlickrQuery.Remove(Processor.NegativeQueryMarker + b));
            Settings.Default.FlickrBans = string.Empty;
            Split(Settings.Default.TwitterBans).ToList().ForEach(b => FeedProcessor.TwitterQuery.Remove(Processor.NegativeQueryMarker + b));
            Settings.Default.TwitterBans = string.Empty;
            Split(Settings.Default.NewsBans).ToList().ForEach(b => FeedProcessor.NewsQuery.Remove(Processor.NegativeQueryMarker + b));
            Settings.Default.NewsBans = string.Empty;
            Settings.Default.Save();
        }
    }
}
