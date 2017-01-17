// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Cors.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System.Collections.Generic;

    /// <summary>
    ///     Object representing CORS settings.
    /// </summary>
    public class Cors
    {
        /// <summary>
        /// Gets or sets a value indicating whether credentials are supported.
        /// </summary>
        /// <value>
        ///   <c>true</c> if credentials are supported; otherwise, <c>false</c>.
        /// </value>
        public bool SupportsCredentials { get; set; }

        /// <summary>
        /// Gets or sets the preflight maximum age.
        /// </summary>
        /// <value>
        /// The preflight maximum age.
        /// </value>
        public long? PreflightMaxAge { get; set; }

        /// <summary>
        /// Gets the origins.
        /// </summary>
        /// <value>
        /// The origins.
        /// </value>
        public ICollection<string> Origins { get; } = new List<string>();

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public ICollection<string> Headers { get; } = new List<string>();

        /// <summary>
        /// Gets the methods.
        /// </summary>
        /// <value>
        /// The methods.
        /// </value>
        public ICollection<string> Methods { get; } = new List<string>();

        /// <summary>
        /// Gets the exposed headers.
        /// </summary>
        /// <value>
        /// The exposed headers.
        /// </value>
        public ICollection<string> ExposedHeaders { get; } = new List<string>();
    }
}
