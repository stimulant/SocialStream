// -------------------------------------------------------------
// <copyright file="App.xaml.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation 2011. All rights reserved.
// </copyright>
// -------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace SocialStream.ConfigTool
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="App"/> class.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">More than one instance of the <see cref="T:System.Windows.Application"/> class is created per <see cref="T:System.AppDomain"/>.</exception>
        public App()
        {
            // This lets you change the culture in the Region and Language control panel and render the right culture.
            Thread.CurrentThread.CurrentCulture = CultureInfo.CurrentCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.CurrentCulture;

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }

        /// <summary>
        /// Handles the DispatcherUnhandledException event of the App control. Logs crashes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Threading.DispatcherUnhandledExceptionEventArgs"/> instance containing the event data.</param>
        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
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
