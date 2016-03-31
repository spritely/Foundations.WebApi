// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HostingSettings.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    /// <summary>
    /// Object representing the hosting settings.
    /// </summary>
    public class HostingSettings
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "This is the type required by WebApi.")]
        public string Url { get; set; }
    }
}
