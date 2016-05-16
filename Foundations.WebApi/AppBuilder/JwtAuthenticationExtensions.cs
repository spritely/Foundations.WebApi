// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JwtAuthenticationExtensions.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using Owin;

    /// <summary>
    /// JWT authentication extensions for IAppBuilder.
    /// </summary>
    public static class JwtAuthenticationExtensions
    {
        /// <summary>
        /// Adds JWT bearer token middleware to your web application pipeline with client settings resolved from the container.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="clientSettings">The client settings. If unspecified an attempt will be made to load from the container.</param>
        /// <returns>
        /// The modified application.
        /// </returns>
        public static IAppBuilder UseJwtAuthenticationClientSettings(this IAppBuilder app, JwtAuthenticationClientSettings clientSettings = null)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var settings = clientSettings ?? app.GetInstance<JwtAuthenticationClientSettings>();

            if (settings == null)
            {
                throw new InvalidOperationException(Messages.Exception_UseJwtAuthenticationClientSettings_NoSettingsProvided);
            }

            return app.UseJwtBearerAuthentication(settings);
        }
    }
}
