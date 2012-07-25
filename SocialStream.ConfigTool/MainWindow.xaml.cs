// -------------------------------------------------------------
// <copyright file="MainWindow.xaml.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation 2011. All rights reserved.
// </copyright>
// -------------------------------------------------------------

using System.ComponentModel;
using System.Windows;

namespace SocialStream.ConfigTool
{
    /// <summary>
    /// Interaction logic for the main window.
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            Title = Properties.Resources.ProductName + " " + Properties.Resources.ConfigTool;

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                // Allows for a big d:DesignHeight in Blend for easier editing.
                Height = 700;
            }

            InitializeComponent();
        }
    }
}
