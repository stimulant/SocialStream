using System;
using System.Configuration;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using SocialStream.Properties;

namespace SocialStream
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// A global random number generator for the app.
        /// </summary>
        internal static readonly Random Random = new Random();

        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// More than one instance of the <see cref="T:System.Windows.Application"/> class is created per <see cref="T:System.AppDomain"/>.
        /// </exception>
        public App()
        {
            // This lets you change the culture in the Region and Language control panel and render the right culture.
            Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentCulture;

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            try
            {
                // If the app config file was modified externally, override the user config file.
                DateTime appConfigTime = new FileInfo(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath).LastWriteTime;
                DateTime userConfigTime = new FileInfo(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath).LastWriteTime;
                if (appConfigTime > userConfigTime)
                {
                    Settings.Default.Reset();
                }

                // If the app config file is modified externally, shut down the app.
                Task.Factory.StartNew(new Action(() =>
                {
                    System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromSeconds(10).TotalMilliseconds);
                    timer.Elapsed += (sender, e) =>
                    {
                        DateTime newAppConfigTime = new FileInfo(ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None).FilePath).LastWriteTime;
                        if (newAppConfigTime > appConfigTime)
                        {
                            Application.Current.Dispatcher.BeginInvoke(new Action(() => Application.Current.Shutdown()), null);
                        }
                        else
                        {
                            timer.Start();
                        }
                    };

                    timer.Start();
                }));
            }
            catch
            {
            }

            ValidateSettings();
        }

        /// <summary>
        /// Validates the settings.
        /// </summary>
        private static void ValidateSettings()
        {
            if (Settings.Default.MinImageSize.Width > Settings.Default.MaxImageSize.Width ||
                Settings.Default.MinImageSize.Height > Settings.Default.MaxImageSize.Height ||
                Settings.Default.MinNewsSize.Width > Settings.Default.MaxNewsSize.Width ||
                Settings.Default.MinNewsSize.Height > Settings.Default.MaxNewsSize.Height ||
                Settings.Default.MinStatusSize.Width > Settings.Default.MaxStatusSize.Width ||
                Settings.Default.MinStatusSize.Height > Settings.Default.MaxStatusSize.Height)
            {
                string error = "The minimum sizes in the settings file must be smaller than the maximum sizes.";
                MessageBox.Show(error);
                throw new InvalidOperationException(error);
            }
        }

        /// <summary>
        /// Handles the DispatcherUnhandledException event of the App control. Logs crashes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Threading.DispatcherUnhandledExceptionEventArgs"/> instance containing the event data.</param>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // http://stackoverflow.com/questions/2732140/how-do-i-catch-this-wpf-bitmap-loading-exception
            if (e.Exception.StackTrace.Contains("BitmapFrameDecode.get_ColorContexts"))
            {
                e.Handled = true;
                return;
            }

            if (e.Exception.Message != null && e.Exception.Message.Contains("No information was found about this pixel format"))
            {
                e.Handled = true;
                return;
            }

            LogCrash(e.Exception);
            throw e.Exception;
        }

        /// <summary>
        /// Handles the UnhandledException event of the CurrentDomain control. Logs crashes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.UnhandledExceptionEventArgs"/> instance containing the event data.</param>
        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogCrash(e.ExceptionObject);
            throw e.ExceptionObject as Exception;
        }

        /// <summary>
        /// Logs the crash.
        /// </summary>
        /// <param name="exceptionObject">The exception object.</param>
        private static void LogCrash(object exceptionObject)
        {
            // C:\Users\Public\Documents\SocialStream.exe.log
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments), Process.GetCurrentProcess().MainModule.ModuleName + ".log");
            File.WriteAllText(path, exceptionObject.ToString());
        }
    }
}