// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JwtBearerAuthenticationExtensions.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using Owin;

    /// <summary>
    /// JWT bearer authentication extensions for IAppBuilder.
    /// </summary>
    public static class JwtBearerAuthenticationExtensions
    {
        /// <summary>
        /// Adds JWT bearer token middleware to your web application pipeline with client settings resolved from the container.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="settings">The JWT bearer authentication settings. If unspecified an attempt will be made to load from the container.</param>
        /// <returns>
        /// The modified application.
        /// </returns>
        public static IAppBuilder UseJwtBearerAuthentication(this IAppBuilder app, JwtBearerAuthenticationSettings settings = null)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var s = settings ?? app.GetInstance<JwtBearerAuthenticationSettings>();

            if (s == null)
            {
                throw new InvalidOperationException(Messages.Exception_UseJwtBearerAuthentication_NoSettingsProvided);
            }

            return app.UseJwtBearerAuthentication(s.ToJwtBearerAuthenticationOptions());
        }
    }
}
