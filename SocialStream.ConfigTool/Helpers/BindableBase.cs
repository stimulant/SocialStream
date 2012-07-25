// -------------------------------------------------------------
// <copyright file="BindableBase.cs" company="Microsoft">
// Copyright (c) Microsoft Corporation 2011. All rights reserved.
// </copyright>
// -------------------------------------------------------------

using System.ComponentModel;

namespace SocialStream.Helpers
{
    /// <summary>
    /// Base class for anything implementing INotifyPropertyChanged.
    /// </summary>
    public abstract class BindableBase : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged Members

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Utility method for firing the PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
