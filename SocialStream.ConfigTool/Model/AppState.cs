using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using FeedProcessor.Enums;
using SocialStream.ConfigTool.VO;
using SocialStream.Helpers;

namespace SocialStream.ConfigTool.Model
{
    /// <summary>
    /// Maintains the global application state.
    /// </summary>
    public class AppState : BindableBase
    {
        #region Private Props

        /// <summary>
        /// The parsed XML configuration.
        /// </summary>
        private Dictionary<string, XmlDocument> _ConfigXml = new Dictionary<string, XmlDocument>();

        /// <summary>
        /// Backing store for Instance.
        /// </summary>
        private static AppState _Instance;

        /// <summary>
        /// The filename from which to load the config data.
        /// </summary>
#if DEBUG
        private const string _AppAssemblyName = "SocialStream";
#else
        private const string _AppAssemblyName = "SocialStream.vshost";
#endif

        /// <summary>
        /// The account which is used in surface mode.
        /// </summary>
        private const string _TableUser = "TableUser";

        /// <summary>
        /// The filename from which to load the profanity list.
        /// </summary>
        private const string _ProfanityListFileName = "Profanity.txt";

        /// <summary>
        /// The name of the news theme color setting in the config file.
        /// </summary>
        private const string _NewsThemeColorSetting = "NewsThemeColor";

        /// <summary>
        /// The name of the news foreground color setting in the config file.
        /// </summary>
        private const string _NewsForegroundColorSetting = "NewsForegroundColor";

        /// <summary>
        /// The name of the message theme color setting in the config file.
        /// </summary>
        private const string _MessageThemeColorSetting = "SocialThemeColor";

        /// <summary>
        /// The name of the message foreground color setting in the config file.
        /// </summary>
        private const string _MessageForegroundColorSetting = "SocialForegroundColor";

        /// <summary>
        /// The name of the new item border color setting in the config file.
        /// </summary>
        private const string _NewItemBorderColorSetting = "NewItemBorderColor";

        /// <summary>
        /// The name of the Item Timeout Front setting in the config file.
        /// </summary>
        private const string _ItemTimeoutFrontSetting = "ItemTimeoutFront";

        /// <summary>
        /// The name of the item timeout back setting in the config file.
        /// </summary>
        private const string _ItemTimeoutBackSetting = "ItemTimeoutBack";

        /// <summary>
        /// The name of the Flickr Poll Interval setting in the config file.
        /// </summary>
        private const string _FlickrPollIntervalSetting = "FlickrPollInterval";

        /// <summary>
        /// The name of the Twitter query setting in the config file.
        /// </summary>
        private const string _TwitterQuerySetting = "TwitterQuery";

        /// <summary>
        /// The name of the Flickr query setting in the config file.
        /// </summary>
        private const string _FlickrQuerySetting = "FlickrQuery";

        /// <summary>
        /// The name of the news query setting in the config file.
        /// </summary>
        private const string _NewsQuerySetting = "NewsQuery";

        /// <summary>
        /// The name of the Facebook query setting in the config file.
        /// </summary>
        private const string _FacebookQuerySetting = "FacebookQuery";

        /// <summary>
        /// The name of the Flickr API Key in the config file.
        /// </summary>
        private const string _FlickrApiKeySetting = "FlickrApiKey";

        /// <summary>
        /// The name of the Microsoft Tag API key in the config file.
        /// </summary>
        private const string _MicrosoftTagApiKeySetting = "MicrosoftTagApiKey";

        /// <summary>
        /// The name of the minimum feed item date setting in the config file.
        /// </summary>
        private const string _MinFeedItemDateSetting = "MinFeedItemDate";

        /// <summary>
        /// The name of the Facebook poll interval setting in the config file.
        /// </summary>
        private const string _FacebookPollIntervalSetting = "FacebookPollInterval";

        /// <summary>
        /// The name of the Twitter poll interval setting in the config file.
        /// </summary>
        private const string _TwitterPollIntervalSetting = "TwitterPollInterval";

        /// <summary>
        /// The name of the News poll interval setting in the config file.
        /// </summary>
        private const string _NewsPollIntervalSetting = "NewsPollInterval";

        /// <summary>
        /// The name of the min news size setting in the config file.
        /// </summary>
        private const string _MinNewsSizeSetting = "MinNewsSize";

        /// <summary>
        /// The name of the max news size setting in the config file.
        /// </summary>
        private const string _MaxNewsSizeSetting = "MaxNewsSize";

        /// <summary>
        /// The name of the Min status size setting in the config file.
        /// </summary>
        private const string _MinStatusSizeSetting = "MinStatusSize";

        /// <summary>
        /// The name of the max status size setting in the config file.
        /// </summary>
        private const string _MaxStatusSizeSetting = "MaxStatusSize";

        /// <summary>
        /// The name of the min image size setting in the config file.
        /// </summary>
        private const string _MinImageSizeSetting = "MinImageSize";

        /// <summary>
        /// The name of the max image size setting in the config file.
        /// </summary>
        private const string _MaxImageSizeSetting = "MaxImageSize";

        /// <summary>
        /// The name of the visualization color setting in the config file.
        /// </summary>
        private const string _VisualizationColorSetting = "VisualizationColor";

        /// <summary>
        /// The name of the distrivute content evenly setting in the config file.
        /// </summary>
        private const string _DistributeContentEvenlySetting = "DistributeContentEvenly";

        /// <summary>
        /// The name of the new item alert setting in the config file.
        /// </summary>
        private const string _NewItemAlertSetting = "NewItemAlert";

        /// <summary>
        /// The name of the admin byte tag series setting in the config file.
        /// </summary>
        private const string _AdminByteTagSeriesSetting = "AdminByteTagSeries";

        /// <summary>
        /// The name of the admin byte tag value setting in the config file.
        /// </summary>
        private const string _AdminByteTagValueSetting = "AdminByteTagValue";

        /// <summary>
        /// The name of the tweet opacity setting in the config file.
        /// </summary>
        private const string _TweetOpacitySetting = "TweetOpacity";

        /// <summary>
        /// The name of the news opacity setting in the config file.
        /// </summary>
        private const string _NewsOpacitySetting = "NewsOpacity";

        /// <summary>
        /// The name of the admin timeout delay setting in the config file.
        /// </summary>
        private const string _AdminTimeoutDelaySetting = "AdminTimeoutDelay";

        /// <summary>
        /// The name of the enable content resizing setting in the config file.
        /// </summary>
        private const string _EnableContentResizingSetting = "EnableContentResizing";

        /// <summary>
        /// The name of the auto scroll speed setting in the config file.
        /// </summary>
        private const string _AutoScrollDirectionSetting = "AutoScrollDirection";

        /// <summary>
        /// The name of the is profanity filter enabled setting in the config file.
        /// </summary>
        private const string _IsProfanityFilterEnabledSetting = "IsProfanityFilterEnabled";

        /// <summary>
        /// The name of the retrieval order setting in the config file.
        /// </summary>
        private const string _RetrievalOrderSetting = "RetrievalOrder";

        /// <summary>
        /// The name of the flickr bans setting in the config file.
        /// </summary>
        private const string _FlickrBansSetting = "FlickrBans";

        /// <summary>
        /// The name of the Twitter bans setting in the config file.
        /// </summary>
        private const string _TwitterBansSetting = "TwitterBans";

        /// <summary>
        /// The name of the News bans setting in the config file.
        /// </summary>
        private const string _NewsBansSetting = "NewsBans";

        /// <summary>
        /// The name of the Facebook bans setting in the config file.
        /// </summary>
        private const string _FacebookBansSetting = "FacebookBans";

        /// <summary>
        /// The name of the display fb content from others setting in the config file.
        /// </summary>
        private const string _DisplayFbContentFromOthersSetting = "DisplayFbContentFromOthers";

        /// <summary>
        /// The location of the horizontal background image.
        /// </summary>
        private readonly static string _HorizontalPreviewSource = Path.GetFullPath(@"Resources\WindowBackground.png");

        /// <summary>
        /// The location of the vertical background image.
        /// </summary>
        private readonly static string _VerticalPreviewSource = Path.GetFullPath(@"Resources\WindowBackground_Vertical.png");

        #endregion

        /// <summary>
        /// Gets the singleton instance of this class.
        /// </summary>
        /// <value>The singleton instance of this class.</value>
        public static AppState Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new AppState();
                }

                return _Instance;
            }
        }

        #region Setup, Loading, Saving

        /// <summary>
        /// Initializes a new instance of the <see cref="AppState"/> class.
        /// </summary>
        public AppState()
        {
            _Instance = this;

            LoadSettings(false);
        }

        /// <summary>
        /// Loads the defaults.
        /// </summary>
        public void LoadDefaults()
        {
            // Load default background images.
            string defaultHorizontal = _HorizontalPreviewSource + ".default";
            if (File.Exists(defaultHorizontal))
            {
                HorizontalBackgroundPath = defaultHorizontal;
            }

            string defaultVertical = _VerticalPreviewSource + ".default";
            if (File.Exists(defaultVertical))
            {
                VerticalBackgroundPath = defaultVertical;
            }

            SaveBackgroundImages();

            // Load all other defaults.
            LoadSettings(true);
        }

        /// <summary>
        /// Load all of the config files for the app, including the application-level config and the user-level config.
        /// Do so in an order such that files loaded last override the settings in earlier files appropriately.
        /// </summary>
        /// <param name="defaults">if set to <c>true</c> to load only default values.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands", Justification = "Shouldn't be an issue.")]
        private void LoadConfigFiles(bool defaults)
        {
            XmlDocument appConfig = new XmlDocument();
            string filename = _AppAssemblyName + ".exe.config";
            if (defaults)
            {
                filename += ".default";
            }

            appConfig.Load(filename);
            _ConfigXml[_AppAssemblyName + ".exe.config"] = appConfig;

            string[] users = new string[] { _TableUser, Environment.UserName };
            string[] profiles = new string[] { "LocalLow", "Roaming", "Local" };
            string fileVersion = FileVersionInfo.GetVersionInfo(_AppAssemblyName + ".exe").FileVersion;

            foreach (string user in users)
            {
                foreach (string profile in profiles)
                {
                    string profileDir = string.Format(CultureInfo.InvariantCulture, @"{0}Users\{1}\AppData\{2}\Microsoft", Path.GetPathRoot(Environment.SystemDirectory), user, profile);
                    if (Directory.Exists(profileDir))
                    {
                        string[] appConfigDirs = Directory.GetDirectories(profileDir, string.Format(CultureInfo.InvariantCulture, "{0}*", _AppAssemblyName), SearchOption.TopDirectoryOnly);
                        foreach (string appConfigDir in appConfigDirs)
                        {
                            string[] appVersionDirs = Directory.GetDirectories(appConfigDir, fileVersion, SearchOption.TopDirectoryOnly);
                            if (appVersionDirs.Length == 1)
                            {
                                string[] configFiles = Directory.GetFiles(appVersionDirs[0], "user.config", SearchOption.AllDirectories);
                                if (configFiles.Length == 1)
                                {
                                    appConfig = new XmlDocument();
                                    appConfig.Load(configFiles[0]);
                                    _ConfigXml[configFiles[0]] = appConfig;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads the settings.
        /// </summary>
        /// <param name="defaults">if set to <c>true</c> use only default settings.</param>
        private void LoadSettings(bool defaults)
        {
            LoadConfigFiles(defaults);
            LoadGeneral(defaults);
            LoadSizes(defaults);
            LoadQueries(defaults);
            LoadColors(defaults);
            LoadProfanityList(defaults);
            FlickrApiKey = GetSetting(_FlickrApiKeySetting, defaults);
            MicrosoftTagApiKey = GetSetting(_MicrosoftTagApiKeySetting, defaults);
        }

        /// <summary>
        /// Loads the general.
        /// </summary>
        /// <param name="defaults">if set to <c>true</c> use only default settings.</param>
        private void LoadGeneral(bool defaults)
        {
            if (TimeSpan.TryParse(GetSetting(_ItemTimeoutBackSetting, defaults), out _ItemTimeoutBack))
            {
                NotifyPropertyChanged("ItemTimeoutBack");
            }

            if (TimeSpan.TryParse(GetSetting(_FlickrPollIntervalSetting, defaults), out _FlickrPollInterval))
            {
                NotifyPropertyChanged("FlickrPollInterval");
            }

            if (TimeSpan.TryParse(GetSetting(_TwitterPollIntervalSetting, defaults), out _TwitterPollInterval))
            {
                NotifyPropertyChanged("TwitterPollInterval");
            }

            if (TimeSpan.TryParse(GetSetting(_NewsPollIntervalSetting, defaults), out _NewsPollInterval))
            {
                NotifyPropertyChanged("NewsPollInterval");
            }

            if (TimeSpan.TryParse(GetSetting(_FacebookPollIntervalSetting, defaults), out _FacebookPollInterval))
            {
                NotifyPropertyChanged("FacebookPollInterval");
            }

            if (TimeSpan.TryParse(GetSetting(_NewItemAlertSetting, defaults), out _NewItemAlert))
            {
                NotifyPropertyChanged("NewItemAlert");
            }

            if (TimeSpan.TryParse(GetSetting(_AdminTimeoutDelaySetting, defaults), out _AdminTimeoutDelay))
            {
                NotifyPropertyChanged("AdminTimeoutDelay");
            }

            if (DateTime.TryParse(GetSetting(_MinFeedItemDateSetting, defaults), out _MinFeedItemDate))
            {
                NotifyPropertyChanged("MinFeedItemDate");
            }

            if (bool.TryParse(GetSetting(_DistributeContentEvenlySetting, defaults), out _DistributeContentEvenly))
            {
                NotifyPropertyChanged("DistributeContentEvenly");
            }

            if (bool.TryParse(GetSetting(_EnableContentResizingSetting, defaults), out _EnableContentResizing))
            {
                NotifyPropertyChanged("EnableContentResizing");
            }

            if (bool.TryParse(GetSetting(_DisplayFbContentFromOthersSetting, defaults), out _DisplayFbContentFromOthers))
            {
                NotifyPropertyChanged("DisplayFbContentFromOthers");
            }

            if (long.TryParse(GetSetting(_AdminByteTagSeriesSetting, defaults), out _AdminByteTagSeries))
            {
                NotifyPropertyChanged("AdminByteTagSeries");
            }

            if (long.TryParse(GetSetting(_AdminByteTagValueSetting, defaults), out _AdminByteTagValue))
            {
                NotifyPropertyChanged("AdminByteTagValue");
            }

            if (double.TryParse(GetSetting(_TweetOpacitySetting, defaults), out _TweetOpacity))
            {
                NotifyPropertyChanged("TweetOpacity");
            }

            if (double.TryParse(GetSetting(_NewsOpacitySetting, defaults), out _NewsOpacity))
            {
                NotifyPropertyChanged("NewsOpacity");
            }

            if (bool.TryParse(GetSetting(_IsProfanityFilterEnabledSetting, defaults), out _IsProfanityFilterEnabled))
            {
                NotifyPropertyChanged("IsProfanityFilterEnabled");
            }

            if (int.TryParse(GetSetting(_AutoScrollDirectionSetting, defaults), out _AutoScrollDirection))
            {
                _AutoScrollDirection = Math.Sign(_AutoScrollDirection);
                NotifyPropertyChanged("AutoScrollDirection");
            }

            if (Enum.TryParse<RetrievalOrder>(GetSetting(_RetrievalOrderSetting, defaults), out _RetrievalOrder))
            {
                NotifyPropertyChanged("RetrievalOrder");
            }
        }

        /// <summary>
        /// Loads the sizes.
        /// </summary>
        /// <param name="defaults">if set to <c>true</c> use only default settings.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want to catch all exceptions.")]
        private void LoadSizes(bool defaults)
        {
            try
            {
                MinStatusSize = Size.Parse(GetSetting(_MinStatusSizeSetting, defaults));
            }
            catch
            {
            }

            try
            {
                MinImageSize = Size.Parse(GetSetting(_MinImageSizeSetting, defaults));
            }
            catch
            {
            }

            try
            {
                MinNewsSize = Size.Parse(GetSetting(_MinNewsSizeSetting, defaults));
            }
            catch
            {
            }

            try
            {
                MaxStatusSize = Size.Parse(GetSetting(_MaxStatusSizeSetting, defaults));
            }
            catch
            {
            }

            try
            {
                MaxImageSize = Size.Parse(GetSetting(_MaxImageSizeSetting, defaults));
            }
            catch
            {
            }

            try
            {
                MaxNewsSize = Size.Parse(GetSetting(_MaxNewsSizeSetting, defaults));
            }
            catch
            {
            }
        }

        /// <summary>
        /// Loads the queries.
        /// </summary>
        /// <param name="defaults">if set to <c>true</c> use only default settings.</param>
        private void LoadQueries(bool defaults)
        {
            string queryStr;

            queryStr = GetSetting(_FlickrQuerySetting, defaults);
            FlickrQueries.Clear();
            if (!string.IsNullOrEmpty(queryStr))
            {
                string[] queries = queryStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string q in queries)
                {
                    FlickrQueries.Add(new BindableStringVO() { StringValue = q });
                }
            }

            queryStr = GetSetting(_TwitterQuerySetting, defaults);
            TwitterQueries.Clear();
            if (!string.IsNullOrEmpty(queryStr))
            {
                string[] queries = queryStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string q in queries)
                {
                    TwitterQueries.Add(new BindableStringVO() { StringValue = q });
                }
            }

            queryStr = GetSetting(_NewsQuerySetting, defaults);
            NewsQueries.Clear();
            if (!string.IsNullOrEmpty(queryStr))
            {
                string[] queries = queryStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string q in queries)
                {
                    NewsQueries.Add(new BindableStringVO() { StringValue = q });
                }
            }

            queryStr = GetSetting(_FacebookQuerySetting, defaults);
            FacebookQueries.Clear();
            if (!string.IsNullOrEmpty(queryStr))
            {
                string[] queries = queryStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string q in queries)
                {
                    FacebookQueries.Add(new BindableStringVO() { StringValue = q });
                }
            }

            queryStr = GetSetting(_FlickrBansSetting, defaults);
            FlickrBans.Clear();
            if (!string.IsNullOrEmpty(queryStr))
            {
                string[] queries = queryStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string q in queries)
                {
                    FlickrBans.Add(new BindableStringVO() { StringValue = q });
                }
            }

            queryStr = GetSetting(_TwitterBansSetting, defaults);
            TwitterBans.Clear();
            if (!string.IsNullOrEmpty(queryStr))
            {
                string[] queries = queryStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string q in queries)
                {
                    TwitterBans.Add(new BindableStringVO() { StringValue = q });
                }
            }

            queryStr = GetSetting(_NewsBansSetting, defaults);
            NewsBans.Clear();
            if (!string.IsNullOrEmpty(queryStr))
            {
                string[] queries = queryStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string q in queries)
                {
                    NewsBans.Add(new BindableStringVO() { StringValue = q });
                }
            }

            queryStr = GetSetting(_FacebookBansSetting, defaults);
            FacebookBans.Clear();
            if (!string.IsNullOrEmpty(queryStr))
            {
                string[] queries = queryStr.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string q in queries)
                {
                    FacebookBans.Add(new BindableStringVO() { StringValue = q });
                }
            }
        }

        /// <summary>
        /// Loads the colors.
        /// </summary>
        /// <param name="defaults">if set to <c>true</c> use only default settings.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want to catch all exceptions.")]
        private void LoadColors(bool defaults)
        {
            try
            {
                System.Drawing.Color tempColor = System.Drawing.ColorTranslator.FromHtml(GetSetting(_NewsThemeColorSetting, defaults));
                NewsThemeColor = Color.FromRgb(tempColor.R, tempColor.G, tempColor.B);
            }
            catch
            {
            }

            try
            {
                System.Drawing.Color tempColor = System.Drawing.ColorTranslator.FromHtml(GetSetting(_MessageThemeColorSetting, defaults));
                SocialThemeColor = Color.FromRgb(tempColor.R, tempColor.G, tempColor.B);
            }
            catch
            {
            }

            try
            {
                System.Drawing.Color tempColor = System.Drawing.ColorTranslator.FromHtml(GetSetting(_VisualizationColorSetting, defaults));
                VisualizationColor = Color.FromRgb(tempColor.R, tempColor.G, tempColor.B);
            }
            catch
            {
            }

            try
            {
                System.Drawing.Color tempColor = System.Drawing.ColorTranslator.FromHtml(GetSetting(_NewsForegroundColorSetting, defaults));
                NewsForegroundColor = Color.FromRgb(tempColor.R, tempColor.G, tempColor.B);
            }
            catch
            {
            }

            try
            {
                System.Drawing.Color tempColor = System.Drawing.ColorTranslator.FromHtml(GetSetting(_MessageForegroundColorSetting, defaults));
                SocialForegroundColor = Color.FromRgb(tempColor.R, tempColor.G, tempColor.B);
            }
            catch
            {
            }

            try
            {
                System.Drawing.Color tempColor = System.Drawing.ColorTranslator.FromHtml(GetSetting(_NewItemBorderColorSetting, defaults));
                NewItemBorderColor = Color.FromRgb(tempColor.R, tempColor.G, tempColor.B);
            }
            catch
            {
            }
        }

        /// <summary>
        /// Loads the profanity list
        /// </summary>
        /// <param name="defaults">if set to <c>true</c> to load only default values.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want all exceptions.")]
        private void LoadProfanityList(bool defaults)
        {
            try
            {
                string filename = defaults ? _ProfanityListFileName + ".default" : _ProfanityListFileName;
                using (StreamReader sr = new StreamReader(filename))
                {
                    string text = sr.ReadToEnd();
                    ProfanityFilterWordList.StringValue = text;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// Saves the profanity list.
        /// </summary>
        private void SaveProfanityList()
        {
            using (StreamWriter outfile = new StreamWriter(_ProfanityListFileName))
            {
                outfile.Write(_ProfanityFilterWordList.StringValue);
            }
        }

        /// <summary>
        /// Save the config data to disk.
        /// </summary>
        internal void Save()
        {
            SetSetting(_NewsThemeColorSetting, string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", NewsThemeColor.R, NewsThemeColor.G, NewsThemeColor.B));
            SetSetting(_MessageThemeColorSetting, string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", SocialThemeColor.R, SocialThemeColor.G, SocialThemeColor.B));
            SetSetting(_VisualizationColorSetting, string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", VisualizationColor.R, VisualizationColor.G, VisualizationColor.B));
            SetSetting(_NewsForegroundColorSetting, string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", NewsForegroundColor.R, NewsForegroundColor.G, NewsForegroundColor.B));
            SetSetting(_MessageForegroundColorSetting, string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", SocialForegroundColor.R, SocialForegroundColor.G, SocialForegroundColor.B));
            SetSetting(_NewItemBorderColorSetting, string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", NewItemBorderColor.R, NewItemBorderColor.G, NewItemBorderColor.B));

            SetSetting(_IsProfanityFilterEnabledSetting, IsProfanityFilterEnabled.ToString(CultureInfo.InvariantCulture));
            SetSetting(_DistributeContentEvenlySetting, DistributeContentEvenly.ToString(CultureInfo.InvariantCulture));
            SetSetting(_EnableContentResizingSetting, EnableContentResizing.ToString(CultureInfo.InvariantCulture));
            SetSetting(_DisplayFbContentFromOthersSetting, DisplayFbContentFromOthers.ToString(CultureInfo.InvariantCulture));

            SetSetting(_MinFeedItemDateSetting, _MinFeedItemDate.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture));

            SetSetting(_ItemTimeoutBackSetting, _ItemTimeoutBack.ToString().Trim());
            SetSetting(_FlickrPollIntervalSetting, _FlickrPollInterval.ToString().Trim());
            SetSetting(_TwitterPollIntervalSetting, _TwitterPollInterval.ToString().Trim());
            SetSetting(_FacebookPollIntervalSetting, _FacebookPollInterval.ToString().Trim());
            SetSetting(_NewsPollIntervalSetting, _NewsPollInterval.ToString().Trim());
            SetSetting(_NewItemAlertSetting, _NewItemAlert.ToString().Trim());
            SetSetting(_AdminTimeoutDelaySetting, _AdminTimeoutDelay.ToString().Trim());

            StringBuilder sb = new StringBuilder();

            if (FlickrQueries.Count == 0)
            {
                SetSetting(_FlickrQuerySetting, string.Empty);
            }
            else
            {
                foreach (BindableStringVO query in FlickrQueries)
                {
                    sb.Append(query.StringValue);
                    sb.Append(",");
                }

                SetSetting(_FlickrQuerySetting, sb.ToString().Substring(0, sb.ToString().Length - 1));
            }

            sb.Clear();

            if (TwitterQueries.Count == 0)
            {
                SetSetting(_TwitterQuerySetting, string.Empty);
            }
            else
            {
                foreach (BindableStringVO query in TwitterQueries)
                {
                    sb.Append(query.StringValue);
                    sb.Append(",");
                }

                SetSetting(_TwitterQuerySetting, sb.ToString().Substring(0, sb.ToString().Length - 1));
            }

            sb.Clear();

            if (NewsQueries.Count == 0)
            {
                SetSetting(_NewsQuerySetting, string.Empty);
            }
            else
            {
                foreach (BindableStringVO query in NewsQueries)
                {
                    sb.Append(query.StringValue);
                    sb.Append(",");
                }

                SetSetting(_NewsQuerySetting, sb.ToString().Substring(0, sb.ToString().Length - 1));
            }

            sb.Clear();

            if (FacebookQueries.Count == 0)
            {
                SetSetting(_FacebookQuerySetting, string.Empty);
            }
            else
            {
                foreach (BindableStringVO query in FacebookQueries)
                {
                    sb.Append(query.StringValue);
                    sb.Append(",");
                }

                SetSetting(_FacebookQuerySetting, sb.ToString().Substring(0, sb.ToString().Length - 1));
            }

            sb.Clear();

            if (FlickrBans.Count == 0)
            {
                SetSetting(_FlickrBansSetting, string.Empty);
            }
            else
            {
                foreach (BindableStringVO query in FlickrBans)
                {
                    sb.Append(query.StringValue);
                    sb.Append(",");
                }

                SetSetting(_FlickrBansSetting, sb.ToString().Substring(0, sb.ToString().Length - 1));
            }

            sb.Clear();

            if (TwitterBans.Count == 0)
            {
                SetSetting(_TwitterBansSetting, string.Empty);
            }
            else
            {
                foreach (BindableStringVO query in TwitterBans)
                {
                    sb.Append(query.StringValue);
                    sb.Append(",");
                }

                SetSetting(_TwitterBansSetting, sb.ToString().Substring(0, sb.ToString().Length - 1));
            }

            sb.Clear();

            if (NewsBans.Count == 0)
            {
                SetSetting(_NewsBansSetting, string.Empty);
            }
            else
            {
                foreach (BindableStringVO query in NewsBans)
                {
                    sb.Append(query.StringValue);
                    sb.Append(",");
                }

                SetSetting(_NewsBansSetting, sb.ToString().Substring(0, sb.ToString().Length - 1));
            }

            sb.Clear();

            if (FacebookBans.Count == 0)
            {
                SetSetting(_FacebookBansSetting, string.Empty);
            }
            else
            {
                foreach (BindableStringVO query in FacebookBans)
                {
                    sb.Append(query.StringValue);
                    sb.Append(",");
                }

                SetSetting(_FacebookBansSetting, sb.ToString().Substring(0, sb.ToString().Length - 1));
            }

            SetSetting(_FlickrApiKeySetting, FlickrApiKey);
            SetSetting(_MicrosoftTagApiKeySetting, MicrosoftTagApiKey);
            SetSetting(_MinStatusSizeSetting, MinStatusSize.ToString(CultureInfo.InvariantCulture));
            SetSetting(_MinImageSizeSetting, MinImageSize.ToString(CultureInfo.InvariantCulture));
            SetSetting(_MinNewsSizeSetting, MinNewsSize.ToString(CultureInfo.InvariantCulture));
            SetSetting(_MaxStatusSizeSetting, MaxStatusSize.ToString(CultureInfo.InvariantCulture));
            SetSetting(_MaxImageSizeSetting, MaxImageSize.ToString(CultureInfo.InvariantCulture));
            SetSetting(_MaxNewsSizeSetting, MaxNewsSize.ToString(CultureInfo.InvariantCulture));
            SetSetting(_AutoScrollDirectionSetting, AutoScrollDirection.ToString(CultureInfo.InvariantCulture).Trim());
            SetSetting(_NewsOpacitySetting, NewsOpacity.ToString(CultureInfo.InvariantCulture).Trim());
            SetSetting(_TweetOpacitySetting, TweetOpacity.ToString(CultureInfo.InvariantCulture).Trim());
            SetSetting(_AdminByteTagSeriesSetting, AdminByteTagSeries.ToString(CultureInfo.InvariantCulture).Trim());
            SetSetting(_AdminByteTagValueSetting, AdminByteTagValue.ToString(CultureInfo.InvariantCulture).Trim());
            SetSetting(_RetrievalOrderSetting, RetrievalOrder.ToString());

            foreach (string configFile in _ConfigXml.Keys)
            {
                _ConfigXml[configFile].Save(configFile);
            }

            SaveProfanityList();
            SaveBackgroundImages();
        }

        /// <summary>
        /// Saves the background images.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "Really do want all exceptions.")]
        private void SaveBackgroundImages()
        {
            try
            {
                if (VerticalBackgroundPath != _VerticalPreviewSource)
                {
                    File.Copy(VerticalBackgroundPath, _VerticalPreviewSource, true);
                }

                if (HorizontalBackgroundPath != _HorizontalPreviewSource)
                {
                    File.Copy(HorizontalBackgroundPath, _HorizontalPreviewSource, true);
                }
            }
            catch
            {
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Backing store for NewsThemeColorSetting.
        /// </summary>
        private Color _NewsThemeColor = Color.FromArgb(255, 255, 189, 44);

        /// <summary>
        /// Gets or sets the news background color.
        /// </summary>
        /// <value>
        /// <c>The color</c>.
        /// </value>
        public Color NewsThemeColor
        {
            get
            {
                return _NewsThemeColor;
            }

            set
            {
                _NewsThemeColor = value;
                NotifyPropertyChanged("NewsThemeColor");
            }
        }

        /// <summary>
        /// Backing store for VisualizationColorSetting.
        /// </summary>
        private Color _VisualizationColor = Color.FromArgb(255, 255, 189, 44);

        /// <summary>
        /// Gets or sets the Visualization color.
        /// </summary>
        /// <value>
        /// <c>The color</c>.
        /// </value>
        public Color VisualizationColor
        {
            get
            {
                return _VisualizationColor;
            }

            set
            {
                _VisualizationColor = value;
                NotifyPropertyChanged("VisualizationColor");
            }
        }

        /// <summary>
        /// Backing store for NewsForegroundColorSetting.
        /// </summary>
        private Color _NewsForegroundColor = Color.FromArgb(255, 255, 189, 44);

        /// <summary>
        /// Gets or sets the News Foreground color.
        /// </summary>
        /// <value>
        /// <c>The color</c>.
        /// </value>
        public Color NewsForegroundColor
        {
            get
            {
                return _NewsForegroundColor;
            }

            set
            {
                _NewsForegroundColor = value;
                NotifyPropertyChanged("NewsForegroundColor");
            }
        }

        /// <summary>
        /// Backing store for SocialThemeColorSetting.
        /// </summary>
        private Color _SocialThemeColor = Color.FromArgb(255, 255, 189, 44);

        /// <summary>
        /// Gets or sets the social background color.
        /// </summary>
        /// <value>
        /// <c>The color</c>.
        /// </value>
        public Color SocialThemeColor
        {
            get
            {
                return _SocialThemeColor;
            }

            set
            {
                _SocialThemeColor = value;
                NotifyPropertyChanged("SocialThemeColor");
            }
        }

        /// <summary>
        /// Backing store for SocialForegroundColorSetting.
        /// </summary>
        private Color _SocialForegroundColor = Color.FromArgb(255, 255, 189, 44);

        /// <summary>
        /// Gets or sets the Social Foreground color.
        /// </summary>
        /// <value>
        /// <c>The color</c>.
        /// </value>
        public Color SocialForegroundColor
        {
            get
            {
                return _SocialForegroundColor;
            }

            set
            {
                _SocialForegroundColor = value;
                NotifyPropertyChanged("SocialForegroundColor");
            }
        }

        /// <summary>
        /// Backing store for NewItemBorderColorSetting.
        /// </summary>
        private Color _NewItemBorderColor = Color.FromArgb(255, 255, 189, 44);

        /// <summary>
        /// Gets or sets the color for new item alert flash.
        /// </summary>
        /// <value>
        /// The color.
        /// </value>
        public Color NewItemBorderColor
        {
            get
            {
                return _NewItemBorderColor;
            }
            set
            {
                _NewItemBorderColor = value;
                NotifyPropertyChanged("NewItemBorderColor");
            }
        }

        /// <summary>
        /// Backing store for ItemTimeoutFrontSetting.
        /// </summary>
        private TimeSpan _ItemTimeoutFront = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Gets or sets the item timeout.
        /// </summary>
        /// <value>
        /// <c>The timeout value</c>.
        /// </value>
        public TimeSpan ItemTimeoutFront
        {
            get
            {
                return _ItemTimeoutFront;
            }

            set
            {
                _ItemTimeoutFront = value;
                NotifyPropertyChanged("ItemTimeoutFront");
            }
        }

        /// <summary>
        /// Backing store for ItemTimeoutBackSetting.
        /// </summary>
        private TimeSpan _ItemTimeoutBack = TimeSpan.FromSeconds(45);

        /// <summary>
        /// Gets or sets the item timeout.
        /// </summary>
        /// <value>
        /// <c>The timeout value</c>.
        /// </value>
        public TimeSpan ItemTimeoutBack
        {
            get
            {
                return _ItemTimeoutBack;
            }

            set
            {
                _ItemTimeoutBack = value;
                NotifyPropertyChanged("ItemTimeoutBack");
            }
        }

        /// <summary>
        /// Backing store for FlickrPollIntervalSetting.
        /// </summary>
        private TimeSpan _FlickrPollInterval = TimeSpan.FromMinutes(20);

        /// <summary>
        /// Gets or sets the Flickr poll interval.
        /// </summary>
        /// <value>
        /// <c>The timespan value</c>.
        /// </value>
        public TimeSpan FlickrPollInterval
        {
            get
            {
                return _FlickrPollInterval;
            }

            set
            {
                _FlickrPollInterval = value;
                NotifyPropertyChanged("FlickrPollInterval");
            }
        }

        /// <summary>
        /// Backing store for TwitterPollIntervalSetting.
        /// </summary>
        private TimeSpan _TwitterPollInterval = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the Twitter poll interval.
        /// </summary>
        /// <value>
        /// <c>The timespan value</c>.
        /// </value>
        public TimeSpan TwitterPollInterval
        {
            get
            {
                return _TwitterPollInterval;
            }

            set
            {
                _TwitterPollInterval = value;
                NotifyPropertyChanged("TwitterPollInterval");
            }
        }

        /// <summary>
        /// Backing store for FacebookPollIntervalSetting.
        /// </summary>
        private TimeSpan _FacebookPollInterval = TimeSpan.FromMinutes(10);

        /// <summary>
        /// Gets or sets the Facebook poll interval.
        /// </summary>
        /// <value>
        /// <c>The timespan value</c>.
        /// </value>
        public TimeSpan FacebookPollInterval
        {
            get
            {
                return _FacebookPollInterval;
            }

            set
            {
                _FacebookPollInterval = value;
                NotifyPropertyChanged("FacebookPollInterval");
            }
        }

        /// <summary>
        /// Backing store for NewsPollIntervalSetting.
        /// </summary>
        private TimeSpan _NewsPollInterval = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the News poll interval.
        /// </summary>
        /// <value>
        /// <c>The timespan value</c>.
        /// </value>
        public TimeSpan NewsPollInterval
        {
            get
            {
                return _NewsPollInterval;
            }

            set
            {
                _NewsPollInterval = value;
                NotifyPropertyChanged("NewsPollInterval");
            }
        }

        /// <summary>
        /// Backing store for NewItemAlertSetting.
        /// </summary>
        private TimeSpan _NewItemAlert = TimeSpan.Parse("01:00:00", CultureInfo.InvariantCulture);

        /// <summary>
        /// Gets or sets the timing for new item alerts.
        /// </summary>
        /// <value>
        /// <c>The timespan value</c>.
        /// </value>
        public TimeSpan NewItemAlert
        {
            get
            {
                return _NewItemAlert;
            }

            set
            {
                _NewItemAlert = value;
                NotifyPropertyChanged("NewItemAlert");
            }
        }

        /// <summary>
        /// Backing store for AdminTimeoutDelaySetting.
        /// </summary>
        private TimeSpan _AdminTimeoutDelay = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Gets or sets the time before the admin panel times out.
        /// </summary>
        /// <value>
        /// <c>The timespan value</c>.
        /// </value>
        public TimeSpan AdminTimeoutDelay
        {
            get
            {
                return _AdminTimeoutDelay;
            }

            set
            {
                _AdminTimeoutDelay = value;
                NotifyPropertyChanged("AdminTimeoutDelay");
            }
        }

        /// <summary>
        /// Backing store for MinFeedItemDateSetting.
        /// </summary>
        private DateTime _MinFeedItemDate = DateTime.Parse("1/1/1970", CultureInfo.InvariantCulture);

        /// <summary>
        /// Gets or sets the minimum feed item date.
        /// </summary>
        /// <value>
        /// <c>The datetime value</c>.
        /// </value>
        public DateTime MinFeedItemDate
        {
            get
            {
                return _MinFeedItemDate;
            }

            set
            {
                _MinFeedItemDate = value;
                NotifyPropertyChanged("MinFeedItemDate");
            }
        }

        /// <summary>
        /// Backing store for TwitterQuerySetting.
        /// </summary>
        private IList<BindableStringVO> _TwitterQueries = new ObservableCollection<BindableStringVO>();

        /// <summary>
        /// Gets the comma-separated list of twitter queries.
        /// </summary>
        /// <value>
        /// <c>a comma-separated query string</c>.
        /// </value>
        public IList<BindableStringVO> TwitterQueries
        {
            get
            {
                return _TwitterQueries;
            }
        }

        /// <summary>
        /// Backing store for FlickrQuerySetting.
        /// </summary>
        private IList<BindableStringVO> _FlickrQueries = new ObservableCollection<BindableStringVO>();

        /// <summary>
        /// Gets the comma-separated list of Flickr queries.
        /// </summary>
        /// <value><c>a comma-separated query string</c>.</value>
        public IList<BindableStringVO> FlickrQueries
        {
            get
            {
                return _FlickrQueries;
            }
        }

        /// <summary>
        /// Backing store for NewsQuerySetting.
        /// </summary>
        private IList<BindableStringVO> _NewsQueries = new ObservableCollection<BindableStringVO>();

        /// <summary>
        /// Gets the comma-separated list of News queries.
        /// </summary>
        /// <value>
        /// <c>a comma-separated query string</c>.
        /// </value>
        public IList<BindableStringVO> NewsQueries
        {
            get
            {
                return _NewsQueries;
            }
        }

        /// <summary>
        /// Backing store for FacebookQuerySetting.
        /// </summary>
        private IList<BindableStringVO> _FacebookQueries = new ObservableCollection<BindableStringVO>();

        /// <summary>
        /// Gets the comma-separated list of News queries.
        /// </summary>
        /// <value>
        /// <c>a comma-separated query string</c>.
        /// </value>
        public IList<BindableStringVO> FacebookQueries
        {
            get
            {
                return _FacebookQueries;
            }
        }

        /// <summary>
        /// Backing store for FlickrApiKeySetting.
        /// </summary>
        private string _FlickrApiKey = string.Empty;

        /// <summary>
        /// Gets or sets the Twitter API Key.
        /// </summary>
        /// <value>
        /// <c>a string value</c>.
        /// </value>
        public string FlickrApiKey
        {
            get
            {
                return _FlickrApiKey;
            }

            set
            {
                _FlickrApiKey = value;
                NotifyPropertyChanged("FlickrApiKey");
            }
        }

        /// <summary>
        /// Backing store for MicrosoftTagApiKeySetting.
        /// </summary>
        private string _MicrosoftTagApiKey = string.Empty;

        /// <summary>
        /// Gets or sets the Twitter API Key.
        /// </summary>
        /// <value>
        /// <c>a string value</c>.
        /// </value>
        public string MicrosoftTagApiKey
        {
            get
            {
                return _MicrosoftTagApiKey;
            }

            set
            {
                _MicrosoftTagApiKey = value;
                NotifyPropertyChanged("MicrosoftTagApiKey");
            }
        }

        /// <summary>
        /// Backing store for HorizontalBackgroundPath.
        /// </summary>
        private string _HorizontalBackgroundPath = _HorizontalPreviewSource;

        /// <summary>
        /// Gets or sets the horizontal background path.
        /// </summary>
        /// <value>The horizontal background path.</value>
        public string HorizontalBackgroundPath
        {
            get
            {
                return _HorizontalBackgroundPath;
            }

            set
            {
                _HorizontalBackgroundPath = value;
                NotifyPropertyChanged("HorizontalBackgroundPath");
            }
        }

        /// <summary>
        /// Backing store for VerticalBackgroundPath.
        /// </summary>
        private string _VerticalBackgroundPath = _VerticalPreviewSource;

        /// <summary>
        /// Gets or sets the vertical background path.
        /// </summary>
        /// <value>The vertical background path.</value>
        public string VerticalBackgroundPath
        {
            get
            {
                return _VerticalBackgroundPath;
            }

            set
            {
                _VerticalBackgroundPath = value;
                NotifyPropertyChanged("VerticalBackgroundPath");
            }
        }

        /// <summary>
        /// Backing store for IsProfanityFilterEnabledSetting.
        /// </summary>
        private bool _IsProfanityFilterEnabled = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not the profanity filter is enabled.
        /// </summary>
        /// <value><c>true</c> if it is enabled, and if not, <c>false</c>.</value>
        public bool IsProfanityFilterEnabled
        {
            get
            {
                return _IsProfanityFilterEnabled;
            }

            set
            {
                _IsProfanityFilterEnabled = value;
                NotifyPropertyChanged("IsProfanityFilterEnabled");
            }
        }

        /// <summary>
        /// Backing store for ProfanityFilterWordList.
        /// </summary>
        private BindableStringVO _ProfanityFilterWordList = new BindableStringVO();

        /// <summary>
        /// Gets or sets the ProfanityFilterWordList.
        /// </summary>
        /// <value>
        /// <c>a BindableStringVO</c>.
        /// </value>
        public BindableStringVO ProfanityFilterWordList
        {
            get
            {
                return _ProfanityFilterWordList;
            }

            set
            {
                ProfanityFilterWordList = value;
                NotifyPropertyChanged("ProfanityFilterWordList");
            }
        }

        /// <summary>
        /// Backing store for DistributeContentEvenlySetting.
        /// </summary>
        private bool _DistributeContentEvenly = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not to distribute content evenly.
        /// </summary>
        /// <value>
        /// <c>true</c> to distribute evenly, and if not, <c>false</c>.
        /// </value>
        public bool DistributeContentEvenly
        {
            get
            {
                return _DistributeContentEvenly;
            }

            set
            {
                _DistributeContentEvenly = value;
                NotifyPropertyChanged("DistributeContentEvenly");
            }
        }

        /// <summary>
        /// Backing store for EnableContentResizingSetting.
        /// </summary>
        private bool _EnableContentResizing = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not content resizing is enabled.
        /// </summary>
        /// <value>
        /// <c>true</c> if it is enabled, and if not, <c>false</c>.
        /// </value>
        public bool EnableContentResizing
        {
            get
            {
                return _EnableContentResizing;
            }

            set
            {
                _EnableContentResizing = value;
                NotifyPropertyChanged("EnableContentResizing");
            }
        }

        /// <summary>
        /// Backing store for DisplayFbContentFromOthersSetting.
        /// </summary>
        private bool _DisplayFbContentFromOthers = true;

        /// <summary>
        /// Gets or sets a value indicating whether or not content from other users on a facebook page is displayed.
        /// </summary>
        /// <value>
        /// <c>true</c> if it is enabled, and if not, <c>false</c>.
        /// </value>
        public bool DisplayFbContentFromOthers
        {
            get
            {
                return _DisplayFbContentFromOthers;
            }

            set
            {
                _DisplayFbContentFromOthers = value;
                NotifyPropertyChanged("DisplayFbContentFromOthers");
            }
        }

        /// <summary>
        /// Backing store for AdminByteTagValueSetting.
        /// </summary>
        private long _AdminByteTagValue = 255;

        /// <summary>
        /// Gets or sets the value of the byte tag used to launch the admin panel.
        /// </summary>
        /// <value>
        /// <c>The byte value</c>.
        /// </value>
        public long AdminByteTagValue
        {
            get
            {
                return _AdminByteTagValue;
            }

            set
            {
                _AdminByteTagValue = value;
                NotifyPropertyChanged("AdminByteTagValue");
            }
        }

        /// <summary>
        /// Backing store for AdminByteTagSeriesSetting.
        /// </summary>
        private long _AdminByteTagSeries = 0;

        /// <summary>
        /// Gets or sets the Series of the byte tag used to launch the admin panel.
        /// </summary>
        /// <Series>
        /// <c>The byte Series</c>.
        /// </Series>
        public long AdminByteTagSeries
        {
            get
            {
                return _AdminByteTagSeries;
            }

            set
            {
                _AdminByteTagSeries = value;
                NotifyPropertyChanged("AdminByteTagSeries");
            }
        }

        /// <summary>
        /// Backing store for AutoScrollDirectionSetting.
        /// </summary>
        private int _AutoScrollDirection = 0;

        /// <summary>
        /// Gets or sets the auto scroll speed color.
        /// </summary>
        /// <value>
        /// <c>The speed of auto scroll: -22, 0 or 22, for best results</c>.
        /// </value>
        public int AutoScrollDirection
        {
            get
            {
                return _AutoScrollDirection;
            }

            set
            {
                _AutoScrollDirection = value;
                NotifyPropertyChanged("AutoScrollDirection");
            }
        }

        /// <summary>
        /// Backing store for NewsOpacitySetting.
        /// </summary>
        private double _NewsOpacity = .4;

        /// <summary>
        /// Gets or sets the opacity of news items.
        /// </summary>
        /// <value>
        /// <c>The opacity value from 0 to 1</c>.
        /// </value>
        public double NewsOpacity
        {
            get
            {
                return _NewsOpacity;
            }

            set
            {
                _NewsOpacity = value;
                NotifyPropertyChanged("NewsOpacity");
            }
        }

        /// <summary>
        /// Backing store for TweetOpacitySetting.
        /// </summary>
        private double _TweetOpacity = .4;

        /// <summary>
        /// Gets or sets the opacity of Tweet items.
        /// </summary>
        /// <value>
        /// <c>The opacity value from 0 to 1</c>.
        /// </value>
        public double TweetOpacity
        {
            get
            {
                return _TweetOpacity;
            }

            set
            {
                _TweetOpacity = value;
                NotifyPropertyChanged("TweetOpacity");
            }
        }

        /// <summary>
        /// Backing store for RetrievalOrderSetting.
        /// </summary>
        private RetrievalOrder _RetrievalOrder = RetrievalOrder.Chronological;

        /// <summary>
        /// Gets or sets the order in which feed items are retrieved.
        /// </summary>
        /// <value>The order in which feed items are retrieved.</value>
        public RetrievalOrder RetrievalOrder
        {
            get
            {
                return _RetrievalOrder;
            }

            set
            {
                _RetrievalOrder = value;
                NotifyPropertyChanged("RetrievalOrder");
            }
        }

        /// <summary>
        /// Backing store for NewsBansSetting.
        /// </summary>
        private IList<BindableStringVO> _NewsBans = new ObservableCollection<BindableStringVO>();

        /// <summary>
        /// Gets the comma-separated list of News bans.
        /// </summary>
        /// <value>
        /// <c>a comma-separated string of all items/users to ban</c>.
        /// </value>
        public IList<BindableStringVO> NewsBans
        {
            get
            {
                return _NewsBans;
            }
        }

        /// <summary>
        /// Backing store for FacebookBansSetting.
        /// </summary>
        private IList<BindableStringVO> _FacebookBans = new ObservableCollection<BindableStringVO>();

        /// <summary>
        /// Gets the comma-separated list of Facebook bans.
        /// </summary>
        /// <value>
        /// <c>a comma-separated string of all items/users to ban</c>.
        /// </value>
        public IList<BindableStringVO> FacebookBans
        {
            get
            {
                return _FacebookBans;
            }
        }

        /// <summary>
        /// Backing store for TwitterBansSetting.
        /// </summary>
        private IList<BindableStringVO> _TwitterBans = new ObservableCollection<BindableStringVO>();

        /// <summary>
        /// Gets the comma-separated list of Twitter bans.
        /// </summary>
        /// <value>
        /// <c>a comma-separated string of all items/users to ban</c>.
        /// </value>
        public IList<BindableStringVO> TwitterBans
        {
            get
            {
                return _TwitterBans;
            }
        }

        /// <summary>
        /// Backing store for FlickrBansSetting.
        /// </summary>
        private IList<BindableStringVO> _FlickrBans = new ObservableCollection<BindableStringVO>();

        /// <summary>
        /// Gets the comma-separated list of Flickr bans.
        /// </summary>
        /// <value>
        /// <c>a comma-separated string of all items/users to ban</c>.
        /// </value>
        public IList<BindableStringVO> FlickrBans
        {
            get
            {
                return _FlickrBans;
            }
        }

        /// <summary>
        /// Backing store for MinStatusSizeSetting.
        /// </summary>
        private Size _MinStatusSize = new Size(350, 325);

        /// <summary>
        /// Gets or sets the min status size.
        /// </summary>
        /// <value>
        /// <c>the size value</c>.
        /// </value>
        public Size MinStatusSize
        {
            get
            {
                return _MinStatusSize;
            }

            set
            {
                _MinStatusSize = value;
                NotifyPropertyChanged("MinStatusSize");
            }
        }

        /// <summary>
        /// Backing store for MinImageSizeSetting.
        /// </summary>
        private Size _MinImageSize = new Size(350, 325);

        /// <summary>
        /// Gets or sets the min Image size.
        /// </summary>
        /// <value>
        /// <c>the size value</c>.
        /// </value>
        public Size MinImageSize
        {
            get
            {
                return _MinImageSize;
            }

            set
            {
                _MinImageSize = value;
                NotifyPropertyChanged("MinImageSize");
            }
        }

        /// <summary>
        /// Backing store for MinNewsSizeSetting.
        /// </summary>
        private Size _MinNewsSize = new Size(350, 325);

        /// <summary>
        /// Gets or sets the min News size.
        /// </summary>
        /// <value>
        /// <c>the size value</c>.
        /// </value>
        public Size MinNewsSize
        {
            get
            {
                return _MinNewsSize;
            }

            set
            {
                _MinNewsSize = value;
                NotifyPropertyChanged("MinNewsSize");
            }
        }

        /// <summary>
        /// Backing store for MaxStatusSizeSetting.
        /// </summary>
        private Size _MaxStatusSize = new Size(350, 325);

        /// <summary>
        /// Gets or sets the Max status size.
        /// </summary>
        /// <value>
        /// <c>the size value</c>.
        /// </value>
        public Size MaxStatusSize
        {
            get
            {
                return _MaxStatusSize;
            }

            set
            {
                _MaxStatusSize = value;
                NotifyPropertyChanged("MaxStatusSize");
            }
        }

        /// <summary>
        /// Backing store for MaxImageSizeSetting.
        /// </summary>
        private Size _MaxImageSize = new Size(350, 325);

        /// <summary>
        /// Gets or sets the Max Image size.
        /// </summary>
        /// <value>
        /// <c>the size value</c>.
        /// </value>
        public Size MaxImageSize
        {
            get
            {
                return _MaxImageSize;
            }

            set
            {
                _MaxImageSize = value;
                NotifyPropertyChanged("MaxImageSize");
            }
        }

        /// <summary>
        /// Backing store for MaxNewsSizeSetting.
        /// </summary>
        private Size _MaxNewsSize = new Size(350, 325);

        /// <summary>
        /// Gets or sets the Max News size.
        /// </summary>
        /// <value>
        /// <c>the size value</c>.
        /// </value>
        public Size MaxNewsSize
        {
            get
            {
                return _MaxNewsSize;
            }

            set
            {
                _MaxNewsSize = value;
                NotifyPropertyChanged("MaxNewsSize");
            }
        }

        #endregion

        #region Utility

        /// <summary>
        /// Gets a setting.
        /// </summary>
        /// <param name="setting">The setting name.</param>
        /// <param name="useDefault">if set to <c>true</c> use only the default settings.</param>
        /// <returns>The setting value.</returns>
        private string GetSetting(string setting, bool useDefault)
        {
            string value = string.Empty;

            foreach (XmlDocument config in _ConfigXml.Values)
            {
                XmlNode node = config.SelectSingleNode("//setting[@name='" + setting + "']/value/text()");
                if (node != null)
                {
                    value = node.Value;
                }

                if (useDefault)
                {
                    break;
                }
            }

            return value;
        }

        /// <summary>
        /// Sets a setting.
        /// </summary>
        /// <param name="setting">The setting name.</param>
        /// <param name="value">The setting value.</param>
        private void SetSetting(string setting, string value)
        {
            foreach (XmlDocument config in _ConfigXml.Values)
            {
                XmlNode node = config.SelectSingleNode("//setting[@name='" + setting + "']/value");
                if (node != null)
                {
                    node.InnerText = value;
                }
            }
        }

        #endregion
    }
}
