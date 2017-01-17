// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HostingSettings.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System.Collections.Generic;

    /// <summary>
    /// Object representing the hosting settings.
    /// </summary>
    public class HostingSettings
    {
        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public ICollection<string> Urls { get; } = new List<string>();

        /// <summary>
        /// Gets or sets the CORS settings instance.
        /// </summary>
        /// <value>
        /// The CORS settings instance.
        /// </value>
        public Cors Cors { get; set; }
    }
}
