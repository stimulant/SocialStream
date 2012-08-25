using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using FeedProcessor.Enums;
using FeedProcessor.FeedItems;
using FeedProcessor.Feeds;

namespace FeedProcessor
{
    /// <summary>
    /// The FeedProcessor is responsible for retrieving and parsing data from Flickr, Twitter, and news feeds.
    /// </summary>
    public class Processor : INotifyPropertyChanged
    {
        /// <summary>
        /// The lock object.
        /// </summary>
        private object _lockObject = new object();

        /// <summary>
        /// The first character of query terms representing a negative match.
        /// </summary>
        public const string NegativeQueryMarker = "!";

        /// <summary>
        /// The first character of query terms representing an author.
        /// </summary>
        public const string AuthorQueryMarker = "@";

        /// <summary>
        /// The first character of query terms representing a group.
        /// </summary>
        public const string GroupQueryMarker = "+";

        #region Private Members

        /// <summary>
        /// The API key to use for Flickr.
        /// </summary>
        private string _flickrApiKey;

        /// <summary>
        /// The Client Id to use for Facebook.
        /// </summary>
        private string _facebookClientId;

        /// <summary>
        /// The Client Secret to use for Facebook.
        /// </summary>
        private string _facebookClientSecret;

        /// <summary>
        /// Determine whether to query others' posts for a Facebook Page.
        /// </summary>
        private bool _displayFbContentFromOthers;

        /// <summary>
        /// Whether or not the processor is running.
        /// </summary>
        private bool _isRunning;

        /// <summary>
        /// A regular expression used to detect profanity in feed item content.
        /// </summary>
        private Regex _profanityRegex;

        /// <summary>
        /// The internal list of feeds.
        /// </summary>
        private List<Feed> _feeds = new List<Feed>();

        /// <summary>
        /// The internal list of feed items.
        /// </summary>
        private List<FeedItem> _feedItems = new List<FeedItem>();

        /// <summary>
        /// The requested interval between queries to the Flickr service.
        /// </summary>
        private TimeSpan _flickrPollInterval;

        /// <summary>
        /// The requested interval between queries to the twitter service.
        /// </summary>
        private TimeSpan _twitterPollInterval;

        /// <summary>
        /// The requested interval between queries to the news service.
        /// </summary>
        private TimeSpan _newsPollInterval;

        /// <summary>
        /// The requested interval between queries to the facebook service.
        /// </summary>
        private TimeSpan _facebookPollInterval;

        /// <summary>
        /// The oldest allowable date for a feed item.
        /// </summary>
        private DateTime _minDate;

        /// <summary>
        /// Used for retrieving items in random mode.
        /// </summary>
        private Random _rnd = new Random();

        /// <summary>
        /// The current index into the list of a given content type.
        /// </summary>
        private Dictionary<ContentType, int> _itemIndexes = new Dictionary<ContentType, int>();

        /// <summary>
        /// List of items of each requested content type, in the order specified by RetrievalOrder.
        /// </summary>
        private Dictionary<ContentType, List<FeedItem>> _feedItemLists = new Dictionary<ContentType, List<FeedItem>>();

        /// <summary>
        /// When GetNextItem is passed a type with flags, keep track of which flag is returned in the resulting item so that
        /// there's an even distribution.
        /// </summary>
        private Dictionary<ContentType, int> _typeIndexes = new Dictionary<ContentType, int>();

        /// <summary>
        /// A cached list of the available content types.
        /// </summary>
        private Collection<ContentType> _contentTypes = EnumToList<ContentType>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="Processor"/> class.
        /// </summary>
        /// <param name="flickrApiKey">The flickr API key.</param>
        /// <param name="flickrPollInterval">The requested interval between queries to the Flickr service.</param>
        /// <param name="twitterPollInterval">The requested interval between queries to the twitter service.</param>
        /// <param name="newsPollInterval">The requested interval between queries to the news service.</param>
        /// <param name="minDate">The oldest allowable date for a feed item.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Intentionally catching all exceptions.")]
        public Processor(string flickrApiKey, string facebookClientId, string facebookClientSecret, bool displayFbContentFromOthers, TimeSpan flickrPollInterval, TimeSpan twitterPollInterval, TimeSpan newsPollInterval, TimeSpan facebookPollInterval, DateTime minDate)
        {
            _flickrApiKey = flickrApiKey;
            _facebookClientId = facebookClientId;
            _facebookClientSecret = facebookClientSecret;
            _displayFbContentFromOthers = displayFbContentFromOthers;
            _flickrPollInterval = flickrPollInterval;
            _twitterPollInterval = twitterPollInterval;
            _newsPollInterval = newsPollInterval;
            _facebookPollInterval = facebookPollInterval;
            _minDate = minDate;
            TwitterQuery = new ObservableCollection<string>();
            FlickrQuery = new ObservableCollection<string>();
            NewsQuery = new ObservableCollection<string>();
            FacebookQuery = new ObservableCollection<string>();

            ServicePointManager.DefaultConnectionLimit = 200;
        }

        /// <summary>
        /// Start polling on all the feeds.
        /// </summary>
        public void Start()
        {
            if (_isRunning)
            {
                return;
            }

            _feeds.ToList().ForEach(f => f.Start());
            _isRunning = true;
        }

        /// <summary>
        /// Stop polling on all the feeds.
        /// </summary>
        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _feeds.ToList().ForEach(f => f.Stop());
            _isRunning = false;
        }

        #region RetrievalOrder

        /// <summary>
        /// Backing store for RetrievalOrder.
        /// </summary>
        private RetrievalOrder _retrievalOrder = RetrievalOrder.Random;

        /// <summary>
        /// Gets or sets the order in which items will be returned from GetNextItem.
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
                _itemIndexes = new Dictionary<ContentType, int>();
                _feedItemLists = new Dictionary<ContentType, List<FeedItem>>();
                NotifyPropertyChanged("RetrievalOrder");
            }
        }

        #endregion

        #region DistributeEvenly

        /// <summary>
        /// Backing store for DistributeEvenly.
        /// </summary>
        private bool _distributeContentEvenly = true;

        /// <summary>
        /// Gets or sets a value indicating whether an attempt is made to distribute item types more evenly.
        /// </summary>
        /// <value><c>true</c> to distribute item types more evently; otherwise, <c>false</c>.</value>
        public bool DistributeContentEvenly
        {
            get
            {
                return _distributeContentEvenly;
            }

            set
            {
                _distributeContentEvenly = value;
                NotifyPropertyChanged("DistributeEvenly");
            }
        }

        #endregion

        #region CacheSize

        /// <summary>
        /// Backing store for CacheSize.
        /// </summary>
        private int _cacheSize = 10000;

        /// <summary>
        /// Gets or sets the size of the news item cache. This is how many items will be kept in memory and appear in the app.
        /// </summary>
        /// <value>The size of the cache.</value>
        public int CacheSize
        {
            get
            {
                return _cacheSize;
            }

            set
            {
                _cacheSize = value;
                PurgeCache();
            }
        }

        #endregion

        #region TwitterQuery

        /// <summary>
        /// Backing store for TwitterQuery.
        /// </summary>
        private ObservableCollection<string> _twitterQuery;

        /// <summary>
        /// Gets or sets the list of query terms for Twitter. A query term can be:
        /// @username – Include items from this user
        /// !@username – Exclude items from this user
        /// keyword – Include this keyword in the search
        /// !keyword – Exclude items containing this word
        /// !http://url/ – Exclude specific items by URL
        /// </summary>
        /// <value>The list of query terms for Twitter.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "No."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework", MessageId = "System.Collections.ObjectModel.ObservableCollection`1<System.String>.#.ctor(System.Collections.Generic.IEnumerable`1<System.String>)", Justification = "Not worried about old framework versions.")]
        public ObservableCollection<string> TwitterQuery
        {
            get
            {
                return _twitterQuery;
            }

            set
            {
                if (_twitterQuery != null)
                {
                    _twitterQuery.CollectionChanged -= TwitterQuery_CollectionChanged;
                }

                if (value != null)
                {
                    _twitterQuery = new ObservableCollection<string>((from q in value select q.Trim()).Distinct());

                    if (_twitterQuery != null)
                    {
                        _twitterQuery.CollectionChanged += TwitterQuery_CollectionChanged;
                    }
                }

                UpdateTwitterQuery();
            }
        }

        /// <summary>
        /// Update feed items when the Twitter query changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void TwitterQuery_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFilters(TwitterQuery, SourceType.Twitter, e.NewItems, e.OldItems);
        }

        /// <summary>
        /// When the Twitter query changes, create new Twitter feeds to poll for it.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Feeds are disposed in RemoveFeed().")]
        private void UpdateTwitterQuery()
        {
            List<TwitterSearchFeed> feeds = (from f in _feeds where f.SourceType == SourceType.Twitter select f).Cast<TwitterSearchFeed>().ToList();
            foreach (TwitterSearchFeed oldFeed in feeds)
            {
                RemoveFeed(oldFeed);
            }

            if (TwitterQuery == null || TwitterQuery.Count == 0)
            {
                return;
            }

            // Build queries for the search API.
            List<string> terms =
                (from q in TwitterQuery
                 where !q.StartsWith(AuthorQueryMarker, StringComparison.OrdinalIgnoreCase) &&
                   !q.StartsWith(NegativeQueryMarker, StringComparison.OrdinalIgnoreCase)
                 select q).ToList();

            List<string> queries = new List<string>();

            if (terms.Count != 0)
            {
                StringBuilder query = new StringBuilder();
                string or = HttpUtility.UrlEncode(" OR ");

                foreach (string term in terms)
                {
                    string queryPart = term;

                    if (query.Length == 0)
                    {
                        query.Append(HttpUtility.UrlEncode(queryPart));
                    }
                    else if (query.Length + or.Length + queryPart.Length <= 140)
                    {
                        query.Append(or);
                        query.Append(HttpUtility.UrlEncode(queryPart));
                    }
                    else
                    {
                        // If the query is longer than 140 characters, finish up this query and make a new feed with the next batch of query terms.
                        queries.Add(query.ToString());
                        query = new StringBuilder();
                    }
                }

                queries.Add(query.ToString());
            }

            // Build queries for user searches.
            List<string> users = (from q in TwitterQuery where q.StartsWith(AuthorQueryMarker, StringComparison.OrdinalIgnoreCase) select q.Substring(1)).ToList();

            // Increase the poll interval so that the total number of requests don't exceed the requested poll interval.
            TimeSpan pollInterval = TimeSpan.FromMilliseconds(_twitterPollInterval.TotalMilliseconds * (queries.Count + users.Count));

            // Build feeds for the search API.
            foreach (string query in queries)
            {
                TwitterSearchFeed feed = new TwitterSearchFeed(pollInterval, _minDate);
                feed.Query = query;
                AddFeed(feed);
            }

            // Build feeds for user searches.
            foreach (string user in users)
            {
                TwitterUserFeed feed = new TwitterUserFeed(pollInterval, _minDate);
                feed.Query = HttpUtility.UrlEncode(user);
                AddFeed(feed);
            }
        }

        /*
         * The below implementation of UpdateTwitterQuery uses TwitterStreamingFeed instead of the search/user feeds. This
         * maps to twitter's streaming API (http://dev.twitter.com/pages/streaming_api). The streaming API has the advantage
         * of not being rate-limited, so if you have many instances of Social Stream running behind the same IP, you'll
         * continue to get tweets. However, it has some disadvantages.
         * 
         * It's the real-time stream, so if you follow @user, you won't see anything from them until they post something new.
         * 
         * Because of this, all tweets will start with the timestamp "just now".
         * 
         * You can't search for #hashtags in the same way. If you search for "microsoft surface" you will get things about surface, 
         * but if you do "#surface", you will get lots of irrelevant things about the general term "surface".
         * 
         * With the search api you can use "to:surface" to get any reply to @surface, bit you can't in the streaming API. 
         */

        /*
        /// <summary>
        /// When the Twitter query changes, create new Twitter feeds to poll for it.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Feeds are disposed in RemoveFeed().")]
        private void UpdateTwitterQuery()
        {
            List<TwitterSearchFeed> feeds = (from f in _feeds where f.SourceType == SourceType.Twitter select f).Cast<TwitterSearchFeed>().ToList();
            foreach (TwitterSearchFeed oldFeed in feeds)
            {
                RemoveFeed(oldFeed);
            }

            if (TwitterQuery == null || TwitterQuery.Count == 0)
            {
                return;
            }

            List<string> terms =
                (from q in TwitterQuery
                 where !q.StartsWith(AuthorQueryMarker, StringComparison.OrdinalIgnoreCase) &&
                   !q.StartsWith(NegativeQueryMarker, StringComparison.OrdinalIgnoreCase)
                 select q.Replace("#", string.Empty)).ToList();

            TwitterStreamingFeed feed = new TwitterStreamingFeed(_twitterUsername, _twitterPassword);
            feed.Query += "&track=" + string.Join(",", terms);

            // Get all the usernames to follow.
            List<string> usernames =
                (from q in TwitterQuery
                 where q.StartsWith(AuthorQueryMarker, StringComparison.OrdinalIgnoreCase)
                 select q.Substring(1)).ToList();

            if (usernames.Count == 0)
            {
                // If there aren't any, just add the feed.
                AddFeed(feed);
                return;
            }

            // If there are usernames, get all the user ids for them, since that's what the streaming API wants.
            int userIdRequests = 0;
            List<string> userIds = new List<string>();

            foreach (string username in usernames)
            {
                TwitterStreamingFeed.GetTwitterUserIdFromUserNameCallback callback = new TwitterStreamingFeed.GetTwitterUserIdFromUserNameCallback((userId) =>
                {
                    userIdRequests++;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        userIds.Add(userId);
                    }

                    if (userIdRequests < usernames.Count)
                    {
                        return;
                    }

                    feed.Query += "&follow=" + string.Join(",", userIds);
                    AddFeed(feed);
                });

                TwitterStreamingFeed.GetTwitterUserIdFromUserName(username, callback);
            }
        }
         */

        #endregion

        #region FlickrQuery

        /// <summary>
        /// Backing store for FlickrQuery.
        /// </summary>
        private ObservableCollection<string> _flickrQuery;

        /// <summary>
        /// Gets or sets the list of query terms for Flickr. A query term can be:
        /// @username – Include items from this user
        /// !@username – Exclude items from this user
        /// +groupname - Include items from this group pool
        /// keyword – Include this keyword in the search
        /// !keyword – Exclude items containing this word
        /// !http://url/ – Exclude specific items by URL
        /// </summary>
        /// <value>The list of query terms for Flickr.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "No."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework", MessageId = "System.Collections.ObjectModel.ObservableCollection`1<System.String>.#.ctor(System.Collections.Generic.IEnumerable`1<System.String>)", Justification = "Not worried about old framework versions.")]
        public ObservableCollection<string> FlickrQuery
        {
            get
            {
                return _flickrQuery;
            }

            set
            {
                if (_flickrQuery != null)
                {
                    _flickrQuery.CollectionChanged -= FlickrQuery_CollectionChanged;
                }

                if (value != null)
                {
                    _flickrQuery = new ObservableCollection<string>((from q in value select q.Trim()).Distinct());

                    if (_flickrQuery != null)
                    {
                        _flickrQuery.CollectionChanged += FlickrQuery_CollectionChanged;
                    }
                }

                UpdateFlickrQuery();
            }
        }

        /// <summary>
        /// Update feed items when the Flickr query changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void FlickrQuery_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFilters(FlickrQuery, SourceType.Flickr, e.NewItems, e.OldItems);
        }

        /// <summary>
        /// When the Flickr query changes, create new Flickr feeds to poll for it.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Feeds are disposed in RemoveFeed().")]
        private void UpdateFlickrQuery()
        {
            List<FlickrSearchFeed> feeds = (from f in _feeds where f.SourceType == SourceType.Flickr select f).Cast<FlickrSearchFeed>().ToList();
            foreach (FlickrSearchFeed oldFeed in feeds)
            {
                RemoveFeed(oldFeed);
            }

            if (FlickrQuery == null || FlickrQuery.Count == 0)
            {
                return;
            }

            // Build queries for the search API.
            List<string> terms =
                (from q in FlickrQuery
                 where !q.StartsWith(AuthorQueryMarker, StringComparison.OrdinalIgnoreCase) &&
                   !q.StartsWith(NegativeQueryMarker, StringComparison.OrdinalIgnoreCase) &&
                   !q.StartsWith(GroupQueryMarker, StringComparison.OrdinalIgnoreCase)
                 select q).ToList();

            List<string> queries = new List<string>();

            if (terms.Count != 0)
            {
                List<string> tagsForFeed = new List<string>();
                foreach (string term in terms)
                {
                    tagsForFeed.Add(HttpUtility.UrlEncode(term));

                    if (tagsForFeed.Count == 20)
                    {
                        // Max of 20 terms per search.
                        queries.Add(string.Join(",", tagsForFeed.ToArray()));
                        tagsForFeed = new List<string>();
                    }
                }

                queries.Add(string.Join(",", tagsForFeed.ToArray()));
            }

            // Build queries for user searches.
            List<string> users = (from q in FlickrQuery where q.StartsWith(AuthorQueryMarker, StringComparison.OrdinalIgnoreCase) select q.Substring(1)).ToList();

            TimeSpan userPollInterval = TimeSpan.FromMilliseconds(_flickrPollInterval.TotalMilliseconds * (queries.Count + users.Count));

            // Build feeds for the search API.
            foreach (string query in queries)
            {
                FlickrSearchFeed feed = new FlickrSearchFeed(_flickrApiKey, userPollInterval, _minDate);
                feed.Query = query;
                AddFeed(feed);
            }

            // Build feeds for user searches.
            foreach (string user in users)
            {
                FlickrSearchFeed.GetFlickrUserIdFromUserNameCallback callback = new FlickrSearchFeed.GetFlickrUserIdFromUserNameCallback((userId) =>
                {
                    if (string.IsNullOrEmpty(userId))
                    {
                        return;
                    }

                    FlickrUserFeed feed = new FlickrUserFeed(_flickrApiKey, userPollInterval, _minDate);
                    feed.Query = userId;
                    AddFeed(feed);
                });

                FlickrSearchFeed.GetFlickrUserIdFromUserName(user, _flickrApiKey, callback);
            }

            // Build queries for group searches.
            List<string> groups = (from q in FlickrQuery where q.StartsWith(GroupQueryMarker, StringComparison.OrdinalIgnoreCase) select q.Substring(1)).ToList();

            TimeSpan groupPollInterval = TimeSpan.FromMilliseconds(_flickrPollInterval.TotalMilliseconds * (queries.Count + groups.Count));

            // Build feeds for the search API.
            foreach (string query in queries)
            {
                FlickrSearchFeed feed = new FlickrSearchFeed(_flickrApiKey, groupPollInterval, _minDate);
                feed.Query = query;
                AddFeed(feed);
            }

            // Build feeds for group searches.
            foreach (string group in groups)
            {
                FlickrSearchFeed.GetFlickrGroupIdFromGroupNameCallback callback = new FlickrSearchFeed.GetFlickrGroupIdFromGroupNameCallback((groupId) =>
                {
                    if (string.IsNullOrEmpty(groupId))
                    {
                        return;
                    }

                    FlickrGroupFeed feed = new FlickrGroupFeed(_flickrApiKey, groupPollInterval, _minDate);
                    feed.Query = groupId;
                    AddFeed(feed);
                });

                FlickrSearchFeed.GetFlickrGroupIdFromGroupName(group, _flickrApiKey, callback);
            }
        }

        #endregion

        #region FacebookQuery

        /// <summary>
        /// Backing store for FacebookQuery.
        /// </summary>
        private ObservableCollection<string> _facebookQuery;

        /// <summary>
        /// TODO: Update Summary
        /// </summary>
        /// <value>The list of query terms for Facebook.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "No."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework", MessageId = "System.Collections.ObjectModel.ObservableCollection`1<System.String>.#.ctor(System.Collections.Generic.IEnumerable`1<System.String>)", Justification = "Not worried about old framework versions.")]
        public ObservableCollection<string> FacebookQuery
        {
            get
            {
                return _facebookQuery;
            }

            set
            {
                if (_facebookQuery != null)
                {
                    _facebookQuery.CollectionChanged -= FacebookQuery_CollectionChanged;
                }

                if (value != null)
                {
                    _facebookQuery = new ObservableCollection<string>((from q in value select q.Trim()).Distinct());

                    if (_facebookQuery != null)
                    {
                        _facebookQuery.CollectionChanged += FacebookQuery_CollectionChanged;
                    }
                }

                UpdateFacebookQuery();
            }
        }

        /// <summary>
        /// Update feed items when the Facebook query changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void FacebookQuery_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFilters(FacebookQuery, SourceType.Facebook, e.NewItems, e.OldItems);
        }

        /// <summary>
        /// When the Facebook query changes, create new Twitter feeds to poll for it.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Feeds are disposed in RemoveFeed().")]
        private void UpdateFacebookQuery()
        {
            List<FacebookPageFeed> feeds = (from f in _feeds where f.SourceType == SourceType.Facebook select f).Cast<FacebookPageFeed>().ToList();
            foreach (FacebookPageFeed oldFeed in feeds)
            {
                RemoveFeed(oldFeed);
            }

            if (FacebookQuery == null || FacebookQuery.Count == 0)
            {
                return;
            }

            // Build queries for page searches.
            List<string> pages = (from q in FacebookQuery where q.StartsWith(AuthorQueryMarker, StringComparison.OrdinalIgnoreCase) select q.Substring(1)).ToList();

            // Increase the poll interval so that the total number of requests don't exceed the requested poll interval.
            TimeSpan pollInterval = TimeSpan.FromMilliseconds(_facebookPollInterval.TotalMilliseconds * (pages.Count));

            // Build feeds for user searches.
            foreach (string page in pages)
            {
                FacebookPageFeed feed = new FacebookPageFeed(_displayFbContentFromOthers, _facebookClientId, _facebookClientSecret, pollInterval, _minDate);
                feed.Query = HttpUtility.UrlEncode(page);
                Console.WriteLine(feed.Query);
                AddFeed(feed);
            }
        }

        #endregion


        #region NewsQuery

        /// <summary>
        /// Backing store for NewsQuery.
        /// </summary>
        private ObservableCollection<string> _newsQuery;

        /// <summary>
        /// Gets or sets the list of query terms for news. A query term can be:
        /// http://url/ – A URL to an RSS or ATOM feed to load.
        /// !@username – Exclude items from this user
        /// !keyword – Exclude items containing this word
        /// !http://url/ – Exclude specific items by URL
        /// </summary>
        /// <value>The list of query terms for news.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "No."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework", MessageId = "System.Collections.ObjectModel.ObservableCollection`1<System.String>.#.ctor(System.Collections.Generic.IEnumerable`1<System.String>)", Justification = "Not worried about old framework versions.")]
        public ObservableCollection<string> NewsQuery
        {
            get
            {
                return _newsQuery;
            }

            set
            {
                if (_newsQuery != null)
                {
                    _newsQuery.CollectionChanged -= NewsQuery_CollectionChanged;
                }

                if (value != null)
                {
                    _newsQuery = new ObservableCollection<string>((from q in value select q.Trim()).Distinct());

                    if (_newsQuery != null)
                    {
                        _newsQuery.CollectionChanged += NewsQuery_CollectionChanged;
                    }
                }

                UpdateNewsQuery();
            }
        }

        /// <summary>
        /// Update feed items when the news query changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void NewsQuery_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateFilters(NewsQuery, SourceType.News, e.NewItems, e.OldItems);
        }

        /// <summary>
        /// When the news query changes, create new news feeds to poll for it.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Feeds are disposed in RemoveFeed().")]
        private void UpdateNewsQuery()
        {
            List<NewsFeed> feeds = (from f in _feeds where f.SourceType == SourceType.News select f).Cast<NewsFeed>().ToList();
            foreach (NewsFeed oldFeed in feeds)
            {
                RemoveFeed(oldFeed);
            }

            if (NewsQuery == null || NewsQuery.Count == 0)
            {
                return;
            }

            List<string> terms = (from q in NewsQuery where !q.StartsWith(NegativeQueryMarker, StringComparison.OrdinalIgnoreCase) select q).ToList();
            if (terms.Count == 0)
            {
                return;
            }

            foreach (string term in terms)
            {
                Uri uri = null;
                if (Uri.TryCreate(term, UriKind.Absolute, out uri))
                {
                    AddFeed(new NewsFeed(_newsPollInterval, _minDate) { Query = term });
                }
            }
        }

        #endregion

        #region Profanity

        /// <summary>
        /// Backing store for Profanity.
        /// </summary>
        private ObservableCollection<string> _profanity;

        /// <summary>
        /// Gets or sets the list of words which are considered profanity.
        /// </summary>
        /// <value>The list of words which are considered profanity.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "No."), System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Portability", "CA1903:UseOnlyApiFromTargetedFramework", MessageId = "System.Collections.ObjectModel.ObservableCollection`1<System.String>.#.ctor(System.Collections.Generic.IEnumerable`1<System.String>)", Justification = "Not worried about old framework versions.")]
        public ObservableCollection<string> Profanity
        {
            get
            {
                return _profanity;
            }

            set
            {
                if (_profanity != null)
                {
                    _profanity.CollectionChanged -= Profanity_CollectionChanged;
                }

                value = new ObservableCollection<string>((from q in value select q.Trim()).Distinct());
                _profanity = value;

                if (_profanity != null)
                {
                    _profanity.CollectionChanged += Profanity_CollectionChanged;
                }

                UpdateProfanity();
            }
        }

        /// <summary>
        /// Update feed items when the profanity list changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Collections.Specialized.NotifyCollectionChangedEventArgs"/> instance containing the event data.</param>
        private void Profanity_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateProfanity();
        }

        /// <summary>
        /// Update feed items when the profanity list changes.
        /// </summary>
        private void UpdateProfanity()
        {
            List<string> parts = new List<string>();
            foreach (string term in Profanity)
            {
                parts.Add(Regex.Escape(term));
            }

            _profanityRegex = new Regex(string.Format(CultureInfo.InvariantCulture, @"\b({0})\b", string.Join("|", parts.ToArray())), RegexOptions.IgnoreCase);
            _feedItems.ToList().ForEach(item => FilterProfanity(item));
        }

        #endregion

        #region IsProfanityFilterEnabled

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
                _feedItems.ToList().ForEach(feedItem => FilterProfanity(feedItem));
            }
        }

        #endregion

        #region Filtering

        /// <summary>
        /// When a term is added to or removed from a query, update all the items in that query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="sourceType">Type of the source.</param>
        /// <param name="newItems">The new items.</param>
        /// <param name="oldItems">The old items.</param>
        private void UpdateFilters(ObservableCollection<string> query, SourceType sourceType, IList newItems, IList oldItems)
        {
            lock (_lockObject)
            {
                // Block and unblock items based on the new query terms.
                _feedItems.Where(i => i.SourceType == sourceType).ToList().ForEach(item => FilterOnQuery(item, query));
            }

            // Reboot all the feeds if a new positive query term has been added or removed.
            bool update = false;

            if (newItems != null)
            {
                foreach (string term in newItems)
                {
                    if (!term.StartsWith(NegativeQueryMarker, StringComparison.OrdinalIgnoreCase))
                    {
                        update = true;
                        break;
                    }
                }
            }

            if (oldItems != null)
            {
                foreach (string term in oldItems)
                {
                    if (!term.StartsWith(NegativeQueryMarker, StringComparison.OrdinalIgnoreCase))
                    {
                        update = true;
                        break;
                    }
                }
            }

            if (!update)
            {
                return;
            }

            if (sourceType == SourceType.Flickr)
            {
                UpdateFlickrQuery();
            }
            else if (sourceType == SourceType.News)
            {
                UpdateNewsQuery();
            }
            else if (sourceType == SourceType.Twitter)
            {
                UpdateTwitterQuery();
            }
            else if (sourceType == SourceType.Facebook)
            {
                UpdateFacebookQuery();
            }
        }

        /// <summary>
        /// Filters an item based on the flickr/twitter/news query.
        /// </summary>
        /// <param name="feedItem">The feed item to filter.</param>
        /// <param name="query">The query to filter on.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Not that complicated.")]
        private void FilterOnQuery(FeedItem feedItem, ObservableCollection<string> query)
        {
            if (feedItem.BlockReason == BlockReason.Profanity)
            {
                return;
            }

            feedItem.BlockReason = BlockReason.None;

            foreach (string term in query)
            {
                if (!term.StartsWith(NegativeQueryMarker, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Block anything by a blocked author.
                if (!string.IsNullOrEmpty(feedItem.Author) && term.StartsWith(NegativeQueryMarker + AuthorQueryMarker, StringComparison.OrdinalIgnoreCase))
                {
                    if (feedItem.Author.ToUpper(CultureInfo.InvariantCulture) == term.Substring(2).ToUpper(CultureInfo.InvariantCulture))
                    {
                        SetBlockReason(feedItem, BlockReason.Author);
                    }
                }

                // Block anything with a blocked URI.
                if (term.StartsWith(NegativeQueryMarker + "http", StringComparison.OrdinalIgnoreCase))
                {
                    if (feedItem.Uri.OriginalString.ToUpper(CultureInfo.InvariantCulture) == term.Substring(1).ToUpper(CultureInfo.InvariantCulture))
                    {
                        SetBlockReason(feedItem, BlockReason.Uri);
                    }
                }

                // Block anything that contains blocked keywords.
                if (!term.StartsWith(AuthorQueryMarker, StringComparison.OrdinalIgnoreCase))
                {
                    // Escape any special characters from the banned keyword.
                    Regex regex = new Regex(string.Format(CultureInfo.InvariantCulture, @"\b{0}\b", Regex.Escape(term.Substring(1))), RegexOptions.IgnoreCase);

                    if (!string.IsNullOrEmpty(feedItem.Author) && regex.Match(feedItem.Author).Success)
                    {
                        SetBlockReason(feedItem, BlockReason.Keyword);
                    }

                    StatusFeedItem statusFeedItem = feedItem as StatusFeedItem;
                    if (statusFeedItem != null)
                    {
                        if (regex.Match(statusFeedItem.Status).Success)
                        {
                            SetBlockReason(feedItem, BlockReason.Keyword);
                        }
                    }

                    ImageFeedItem imageFeedItem = feedItem as ImageFeedItem;
                    if (imageFeedItem != null)
                    {
                        if ((!string.IsNullOrEmpty(imageFeedItem.Title) && regex.Match(imageFeedItem.Title).Success) ||
                            (!string.IsNullOrEmpty(imageFeedItem.Caption) && regex.Match(imageFeedItem.Caption).Success))
                        {
                            SetBlockReason(feedItem, BlockReason.Keyword);
                        }
                    }

                    NewsFeedItem newsFeedItem = feedItem as NewsFeedItem;
                    if (newsFeedItem != null)
                    {
                        if ((!string.IsNullOrEmpty(newsFeedItem.Title) && regex.Match(newsFeedItem.Title).Success) ||
                            (!string.IsNullOrEmpty(newsFeedItem.Summary) && regex.Match(newsFeedItem.Summary).Success) ||
                            (!string.IsNullOrEmpty(newsFeedItem.Body) && regex.Match(newsFeedItem.Body).Success))
                        {
                            SetBlockReason(feedItem, BlockReason.Keyword);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Filters items based on the profanity list.
        /// </summary>
        /// <param name="feedItem">The feed item to filter.</param>
        private void FilterProfanity(FeedItem feedItem)
        {
            if (!IsProfanityFilterEnabled)
            {
                // The profanity filter is turned off, so get rid of any profanity blocks and bail.
                if (feedItem.BlockReason == BlockReason.Profanity)
                {
                    feedItem.BlockReason = BlockReason.None;
                }

                return;
            }

            if (feedItem.BlockReason != BlockReason.None)
            {
                // If the item is blocked for some other reason, don't do the profanity check.
                return;
            }

            StringBuilder content = new StringBuilder();
            content.Append(feedItem.Author);

            ImageFeedItem imageFeedItem = feedItem as ImageFeedItem;
            if (imageFeedItem != null)
            {
                content.Append(" ");
                content.Append(imageFeedItem.Title);
                content.Append(" ");
                content.Append(imageFeedItem.Caption);
            }

            NewsFeedItem newsFeedItem = feedItem as NewsFeedItem;
            if (newsFeedItem != null)
            {
                content.Append(" ");
                content.Append(newsFeedItem.Title);
                content.Append(" ");
                content.Append(newsFeedItem.Summary);
                content.Append(" ");
                content.Append(newsFeedItem.Body);
            }

            StatusFeedItem statusFeedItem = feedItem as StatusFeedItem;
            if (statusFeedItem != null)
            {
                content.Append(" ");
                content.Append(statusFeedItem.Status);
            }

            if (_profanityRegex.Match(content.ToString()).Success)
            {
                SetBlockReason(feedItem, BlockReason.Profanity);
            }
        }

        /// <summary>
        /// Sets a blocking reason on a feed item and logs the reason.
        /// </summary>
        /// <param name="feedItem">The feed item.</param>
        /// <param name="blockReason">The block reason.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "FeedProcessor.Processor.Log(System.String)", Justification = "It's just a log message.")]
        private void SetBlockReason(FeedItem feedItem, BlockReason blockReason)
        {
            if (feedItem.BlockReason != BlockReason.None && blockReason != BlockReason.None)
            {
                return;
            }

            feedItem.BlockReason = blockReason;
            Log(string.Format(CultureInfo.InvariantCulture, "Blocked {0} {1}", feedItem.Uri, blockReason));
        }

        #endregion

        #region Add items

        /// <summary>
        /// When a new feed item arrives, filter it and add it to the list.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="FeedProcessor.GotNewFeedItemEventArgs"/> instance containing the event data.</param>
        private void Feed_GotNewFeedItem(Feed sender, GotNewFeedItemEventArgs e)
        {
            lock (_lockObject)
            {
                // Ignore if this item already exists
                FeedItem existingItem =
                    (from item in _feedItems
                     where item.Uri.OriginalString == e.FeedItem.Uri.OriginalString
                     select item).FirstOrDefault();

                if (existingItem != null)
                {
                    return;
                }

                // Filter, filter, filter.
                if (e.FeedItem.BlockReason == BlockReason.None && e.FeedItem.SourceType == SourceType.Twitter)
                {
                    FilterOnQuery(e.FeedItem, TwitterQuery);
                }

                if (e.FeedItem.BlockReason == BlockReason.None && e.FeedItem.SourceType == SourceType.Flickr)
                {
                    FilterOnQuery(e.FeedItem, FlickrQuery);
                }

                if (e.FeedItem.BlockReason == BlockReason.None && e.FeedItem.SourceType == SourceType.News)
                {
                    FilterOnQuery(e.FeedItem, NewsQuery);
                }

                if (e.FeedItem.BlockReason == BlockReason.None && e.FeedItem.SourceType == SourceType.Facebook)
                {
                    FilterOnQuery(e.FeedItem, FacebookQuery);
                }

                if (e.FeedItem.BlockReason == BlockReason.None)
                {
                    FilterProfanity(e.FeedItem);
                }

                StatusFeedItem statusFeedItem = e.FeedItem as StatusFeedItem;
                if (statusFeedItem != null)
                {
                    TwitterSearchFeed.GetImageLinkCallback callback = new TwitterSearchFeed.GetImageLinkCallback((imageLink, newStatus) =>
                    {
                        if (imageLink != null)
                        {
                            // There's an image link, so convert the item to an image and add it.
                            ImageFeedItem imageFeedItem = new ImageFeedItem(statusFeedItem);
                            imageFeedItem.Caption = newStatus;
                            imageFeedItem.ThumbnailUri = imageLink;
                            AddItem(imageFeedItem);
                        }
                        else
                        {
                            AddItem(statusFeedItem);
                        }
                    });

                    if (!TwitterSearchFeed.GetImageLink(statusFeedItem.Status, callback))
                    {
                        // No image link, so just add it as a status item.
                        AddItem(statusFeedItem);
                    }
                }
                else
                {
                    AddItem(e.FeedItem);
                }
            }
        }

        /// <summary>
        /// When a new feed item arrives, add it to the item list for that type.
        /// </summary>
        /// <param name="feedItem">The item to add.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "FeedProcessor.Processor.Log(System.String)", Justification = "It's just a log message.")]
        private void AddItem(FeedItem feedItem)
        {
            lock (_lockObject)
            {
                // Add it to the global list of items.
                _feedItems.Add(feedItem);

                // Collect the item lists which match the type of the new item.
                List<List<FeedItem>> itemLists = new List<List<FeedItem>>();
                foreach (ContentType listType in _feedItemLists.Keys)
                {
                    if ((feedItem.ContentType & listType) == feedItem.ContentType)
                    {
                        itemLists.Add(_feedItemLists[listType]);
                    }
                }

                // Add the new item to each of the item lists.
                foreach (List<FeedItem> feedItemList in itemLists)
                {
                    int newItemIndex = 0;

                    if (RetrievalOrder == RetrievalOrder.Random)
                    {
                        // We're in random mode so insert it in some random place.
                        newItemIndex = _rnd.Next(Math.Max(0, feedItemList.Count - 1));
                    }
                    else if (RetrievalOrder == RetrievalOrder.Chronological)
                    {
                        // We're in chronological mode so insert it in the right spot.
                        DateTime itemDate = feedItem.Date;
                        FeedItem previousItem = (from i in feedItemList where i.Date > itemDate select i).LastOrDefault();
                        if (previousItem != null)
                        {
                            newItemIndex = feedItemList.IndexOf(previousItem) + 1;
                        }
                    }

                    if (_itemIndexes.ContainsKey(feedItem.ContentType))
                    {
                        // Insert new items so they come up next in the stream.
                        newItemIndex = Math.Max(newItemIndex, _itemIndexes[feedItem.ContentType]);
                    }

                    newItemIndex = Math.Min(newItemIndex, feedItemList.Count);
                    feedItemList.Insert(newItemIndex, feedItem);
                }

                Log(string.Format(CultureInfo.InvariantCulture, "Added item {0} ", feedItem.Uri));
                PurgeCache();
            }
        }

        /// <summary>
        /// Remove old items from the cache.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "FeedProcessor.Processor.Log(System.String)", Justification = "It's just a log message.")]
        private void PurgeCache()
        {
            if (_feedItems.Count < CacheSize * 1.2)
            {
                return;
            }

            lock (_lockObject)
            {
                IEnumerable<FeedItem> itemsToRemove = (from i in _feedItems select i).Take(Math.Max(0, _feedItems.Count() - _cacheSize));
                if (itemsToRemove.Count() == 0)
                {
                    return;
                }

                _feedItems = (from i in _feedItems where !itemsToRemove.Contains(i) select i).ToList();
                foreach (ContentType contentType in _feedItemLists.Keys.ToList())
                {
                    List<FeedItem> list = _feedItemLists[contentType];
                    _feedItemLists[contentType] = (from i in list where !itemsToRemove.Contains(i) select i).ToList();
                }

                Log(string.Format(CultureInfo.InvariantCulture, "Purged {0} items from cache", itemsToRemove.Count()));

                if (CachePurged != null)
                {
                    CachePurged(this, new CachePurgeEventArgs(new ReadOnlyCollection<object>(_feedItems.Cast<object>().ToList())));
                }
            }
        }

        #endregion

        #region Retrieve items

        /// <summary>
        /// Gets a random item of the specified content type.
        /// </summary>
        /// <param name="requestedType">The type of content to return, flags ok.</param>
        /// <returns>A random feed item.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Not that bad.")]
        public FeedItem GetNextItem(ContentType requestedType)
        {
            lock (_lockObject)
            {
                ContentType finalType = requestedType;

                if (_distributeContentEvenly)
                {
                    finalType = GetDistributedType(requestedType);
                }

                // Figure out which item index we're at for this content type.
                int itemIndex = 0;
                if (_itemIndexes.ContainsKey(finalType))
                {
                    itemIndex = _itemIndexes[finalType];
                }
                else
                {
                    itemIndex = _itemIndexes[finalType] = 0;
                }

                // Find or create the list for this content type.
                List<FeedItem> list = GetListForContentType(finalType);

                if (list.Count == 0)
                {
                    // There's nothing for this type, just return null.
                    return null;
                }

                FeedItem nextItem = null;

                int loopIndex = 0;

                while (nextItem == null)
                {
                    // Loop around when all items have been exhausted.
                    if (itemIndex >= list.Count)
                    {
                        itemIndex = 0;
                    }

                    // Get the item at the current index.
                    nextItem = list[itemIndex];

                    if (nextItem.BlockReason != BlockReason.None)
                    {
                        // This item is blocked, don't use it.
                        nextItem = null;
                    }

                    _itemIndexes[finalType] = itemIndex = itemIndex + 1;

                    loopIndex++;
                    if (loopIndex == list.Count)
                    {
                        break;
                    }
                }

                return nextItem;
            }
        }

        /// <summary>
        /// Given a content type, find the list of items cached for that type.
        /// </summary>
        /// <param name="contentType">The content type to get the list for.</param>
        /// <returns>The list of items cached for the requested type.</returns>
        private List<FeedItem> GetListForContentType(ContentType contentType)
        {
            lock (_lockObject)
            {
                List<FeedItem> list = null;
                if (_feedItemLists.ContainsKey(contentType))
                {
                    list = _feedItemLists[contentType];
                }
                else if (RetrievalOrder == RetrievalOrder.Chronological)
                {
                    list = _feedItemLists[contentType] =
                        _feedItems
                            .Where(i => (i.ContentType & contentType) == i.ContentType)
                            .OrderBy(i => i.Date)
                            .Reverse()
                            .ToList();
                }
                else if (RetrievalOrder == RetrievalOrder.Random)
                {
                    list = _feedItemLists[contentType] =
                        _feedItems
                            .Where(i => (i.ContentType & contentType) == i.ContentType)
                            .OrderBy(i => _rnd.Next())
                            .ToList();
                }

                return list;
            }
        }

        /// <summary>
        /// Given a set of content type flags, return the flag which should be used in order to distribute content most effectively.
        /// </summary>
        /// <param name="contentTypeFlags">The requested content type flags.</param>
        /// <returns>The portion of the glad which should be used in order to distribute content most effectively.</returns>
        private ContentType GetDistributedType(ContentType contentTypeFlags)
        {
            // Find which type was last used in the requested set of flags.
            int typeIndex = 0;
            if (_typeIndexes.ContainsKey(contentTypeFlags))
            {
                typeIndex = _typeIndexes[contentTypeFlags];
            }
            else
            {
                typeIndex = _itemIndexes[contentTypeFlags] = 0;
            }

            // See which types match the requested set of flags.
            List<ContentType> matchingTypes = _contentTypes.Where(t => (t & contentTypeFlags) == t).ToList();

            if (typeIndex == matchingTypes.Count)
            {
                typeIndex = 0;
            }

            // Get the type for the current index.
            ContentType distributedType = matchingTypes[typeIndex];

            // If there are no items in the new type's list, cycle through the other lists to see if there's a type which would work.
            int listIndex = 0;
            while (GetListForContentType(distributedType).Count == 0)
            {
                typeIndex++;
                if (typeIndex == matchingTypes.Count)
                {
                    typeIndex = 0;
                }

                distributedType = matchingTypes[typeIndex];

                listIndex++;
                if (listIndex == matchingTypes.Count)
                {
                    break;
                }
            }

            _typeIndexes[contentTypeFlags] = typeIndex + 1;
            return distributedType;
        }

        #endregion

        #region Manage feeds

        /// <summary>
        /// When Flickr or Twitter goes down or comes back, update the local properties.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="FeedProcessor.SourceStatusUpdatedEventArgs"/> instance containing the event data.</param>
        private void Feed_SourceStatusUpdated(Feed sender, SourceStatusUpdatedEventArgs e)
        {
            if (sender.SourceType == SourceType.Flickr)
            {
                IsFlickrUp = e.IsSourceUp;
                if (IsFlickrUp)
                {
                    LastFlickrUpdate = DateTime.Now;
                }
            }
            else if (sender.SourceType == SourceType.Twitter)
            {
                IsTwitterUp = e.IsSourceUp;
                if (IsTwitterUp)
                {
                    LastTwitterUpdate = DateTime.Now;
                }
            }
            else if (sender.SourceType == SourceType.Facebook)
            {
                IsFacebookUp = e.IsSourceUp;
                if (IsFacebookUp)
                {
                    LastFacebookUpdate = DateTime.Now;
                }
            }

            if (FeedUpdated != null)
            {
                FeedUpdated(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Occurs when a feed is updated.
        /// </summary>
        public event EventHandler FeedUpdated;

        /// <summary>
        /// Adds a new feed to the list.
        /// </summary>
        /// <param name="feed">The feed to add.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "FeedProcessor.Processor.Log(System.String)", Justification = "It's just a log message.")]
        private void AddFeed(Feed feed)
        {
            feed.GotNewFeedItem += Feed_GotNewFeedItem;
            feed.SourceStatusUpdated += Feed_SourceStatusUpdated;
            if (_isRunning)
            {
                feed.Start();
            }

            _feeds.Add(feed);
            Log(string.Format(CultureInfo.InvariantCulture, "Added {0} {1}", feed.GetType().Name, feed.Query));
        }

        /// <summary>
        /// Remove a feed from the list.
        /// </summary>
        /// <param name="oldFeed">The feed to remove.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "FeedProcessor.Processor.Log(System.String)", Justification = "It's just a log message.")]
        private void RemoveFeed(Feed oldFeed)
        {
            oldFeed.GotNewFeedItem -= Feed_GotNewFeedItem;
            oldFeed.SourceStatusUpdated -= Feed_SourceStatusUpdated;
            oldFeed.Stop();
            _feeds.Remove(oldFeed);
            Log(string.Format(CultureInfo.InvariantCulture, "Removed {0} {1}", oldFeed.GetType().Name, oldFeed.Query));
            oldFeed.Dispose();
        }

        #region IsTwitterUp

        /// <summary>
        /// Backing store for IsTwitterUp.
        /// </summary>
        private bool _isTwitterUp;

        /// <summary>
        /// Gets a value indicating whether Twitter is available.
        /// </summary>
        /// <value>
        /// <c>true</c> if Twitter is available; otherwise, <c>false</c>.
        /// </value>
        public bool IsTwitterUp
        {
            get
            {
                return _isTwitterUp;
            }

            private set
            {
                _isTwitterUp = value;
                NotifyPropertyChanged("IsTwitterUp");
            }
        }

        #endregion

        #region IsFlickrUp

        /// <summary>
        /// Backing store for IsFlickrUp.
        /// </summary>
        private bool _isFlickrUp;

        /// <summary>
        /// Gets a value indicating whether Flickr is available.
        /// </summary>
        /// <value>
        /// <c>true</c> if Flickr is available; otherwise, <c>false</c>.
        /// </value>
        public bool IsFlickrUp
        {
            get
            {
                return _isFlickrUp;
            }

            private set
            {
                _isFlickrUp = value;
                NotifyPropertyChanged("IsFlickrUp");
            }
        }

        #endregion

        #region IsFacebookUp

        /// <summary>
        /// Backing store for IsFacebookUp.
        /// </summary>
        private bool _isFacebookUp;

        /// <summary>
        /// Gets a value indicating whether Facebook is available.
        /// </summary>
        /// <value>
        /// <c>true</c> if Facebook is available; otherwise, <c>false</c>.
        /// </value>
        public bool IsFacebookUp
        {
            get
            {
                return _isFacebookUp;
            }

            private set
            {
                _isFacebookUp = value;
                NotifyPropertyChanged("IsFacebookUp");
            }
        }

        #endregion

        #region LastFlickrUpdate

        /// <summary>
        /// Backing store for LastFlickrUpdate.
        /// </summary>
        private DateTime _lastFlickrUpdate;

        /// <summary>
        /// Gets the time of the last response from flickr.
        /// </summary>
        /// <value>The time of the last response from flickr.</value>
        public DateTime LastFlickrUpdate
        {
            get
            {
                return _lastFlickrUpdate;
            }

            private set
            {
                _lastFlickrUpdate = value;
                NotifyPropertyChanged("LastFlickrUpdate");
            }
        }

        #endregion

        #region LastTwitterUpdate

        /// <summary>
        /// Backing store for LastTwitterUpdate.
        /// </summary>
        private DateTime _lastTwitterUpdate;

        /// <summary>
        /// Gets the time of the last response from twitter.
        /// </summary>
        /// <value>The time of the last response from twitter.</value>
        public DateTime LastTwitterUpdate
        {
            get
            {
                return _lastTwitterUpdate;
            }

            private set
            {
                _lastTwitterUpdate = value;
                NotifyPropertyChanged("LastTwitterUpdate");
            }
        }

        #endregion

        #region LastFacebookUpdate

        /// <summary>
        /// Backing store for LastFacebookUpdate.
        /// </summary>
        private DateTime _lastFacebookUpdate;

        /// <summary>
        /// Gets the time of the last response from facebook.
        /// </summary>
        /// <value>The time of the last response from facebook.</value>
        public DateTime LastFacebookUpdate
        {
            get
            {
                return _lastFacebookUpdate;
            }

            private set
            {
                _lastFacebookUpdate = value;
                NotifyPropertyChanged("LastFacebookUpdate");
            }
        }

        #endregion

        /// <summary>
        /// Gets the number of active feeds.
        /// </summary>
        /// <value>The number of active feeds.</value>
        public int FeedCount
        {
            get { return _feeds.Count; }
        }

        /// <summary>
        /// Occurs when cache is purged.
        /// </summary>
        public event EventHandler<CachePurgeEventArgs> CachePurged;

        #endregion

        #region Misc

        /// <summary>
        /// Logs the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected virtual void Log(string message)
        {
            Debug.WriteLine(message);
        }

        /// <summary>
        /// Returns a list of the values in an Enum.
        /// </summary>
        /// <typeparam name="T">The enum type to enumerate</typeparam>
        /// <returns>A list of the values in an enum.</returns>
        public static Collection<T> EnumToList<T>()
        {
            Type enumType = typeof(T);

            // Can't use type constraints on value types, so have to do check like this
            if (enumType.BaseType != typeof(Enum))
            {
                throw new ArgumentException("T must be of type System.Enum");
            }

            return new Collection<T>(Enum.GetValues(enumType) as IList<T>);
        }

        #endregion

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
    }
}
