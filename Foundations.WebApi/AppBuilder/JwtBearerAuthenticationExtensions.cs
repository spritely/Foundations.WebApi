// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JwtBearerAuthenticationExtensions.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Security.Cryptography;
    using Microsoft.Owin.Security.DataHandler.Encoder;
    using Microsoft.Owin.Security.OAuth;
    using Owin;
    using Spritely.Recipes;

    /// <summary>
    /// JWT bearer authentication extensions for IAppBuilder.
    /// </summary>
    public static class JwtBearerAuthenticationExtensions
    {
        /// <summary>
        /// Adds JWT bearer token middleware to your web application pipeline with client settings
        /// resolved from the container.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="settings">
        /// The JWT bearer authentication settings. If unspecified an attempt will be made to load
        /// from the container.
        /// </param>
        /// <returns>The modified application.</returns>
        public static IAppBuilder UseJwtBearerAuthentication(
            this IAppBuilder app,
            JwtBearerAuthenticationSettings settings = null)
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

            if (s.RelativeFileCertificate != null && s.StoreCertificate != null)
            {
                throw new InvalidOperationException(Messages.Exception_UseJwtBearerAuthentication_MultipleOptionsProvided);
            }

            var certificateFetcher =
                s.RelativeFileCertificate != null
                    ? new FileCertificateFetcher(s.RelativeFileCertificate)
                    : s.StoreCertificate != null
                        ? new StoreByThumbprintCertificateFetcher(s.StoreCertificate)
                        : null as ICertificateFetcher;

            RSACryptoServiceProvider privateKey = null;
            if (certificateFetcher != null)
            {
                var certificate = certificateFetcher.Fetch();

                if (certificate == null)
                {
                    throw new InvalidOperationException(Messages.Exception_UseJwtBearerAuthentication_CertificateNotFound);
                }

                privateKey = certificate.PrivateKey as RSACryptoServiceProvider;

                if (privateKey == null)
                {
                    throw new InvalidOperationException(Messages.Exception_UseJwtBearerAuthentication_NoPrivateKey);
                }
            }

            // Try decoding each secret so it will throw an exception early if there is a configuration problem
            s.AllowedServers.ForEach(server => TextEncodings.Base64Url.Decode(server.Secret));

            var bearerAuthenticationOptions = new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = new JoseJwtFormat(s, privateKey),
                AuthenticationType = "Bearer"
            };

            return app.UseOAuthBearerAuthentication(bearerAuthenticationOptions);
        }
    }
}
