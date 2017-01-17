// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ApiControllerSelector.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Net.Http;
    using System.Web.Http;
    using System.Web.Http.Dispatcher;

    /// <summary>
    /// Adds ability to query a controller by hyphenated words
    /// </summary>
    /// <seealso cref="System.Web.Http.Dispatcher.DefaultHttpControllerSelector" />
    public class ApiControllerSelector : DefaultHttpControllerSelector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApiControllerSelector"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public ApiControllerSelector(HttpConfiguration configuration) : base(configuration) { }

        /// <summary>
        /// Gets the name of the controller for the specified <see cref="T:System.Net.Http.HttpRequestMessage" />.
        /// </summary>
        /// <param name="request">The HTTP request message.</param>
        /// <returns>
        /// The name of the controller for the specified <see cref="T:System.Net.Http.HttpRequestMessage" />.
        /// </returns>
        public override string GetControllerName(HttpRequestMessage request)
        {
            // Remove hyphen from controller name lookup
            return base.GetControllerName(request).Replace("-", String.Empty);
        }
    }
}