// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JwtAuthenticationExtensions.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Microsoft.Owin.Security.DataHandler.Encoder;
    using Microsoft.Owin.Security.OAuth;
    using Owin;
    using Spritely.Recipes;

    /// <summary>
    /// JWT authentication extensions for IAppBuilder.
    /// </summary>
    public static class JwtAuthenticationExtensions
    {
        /// <summary>
        /// Adds JWT token middleware to your web application pipeline with client settings
        /// resolved from the container.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="settings">
        /// The JWT bearer authentication settings. If unspecified an attempt will be made to load
        /// from the container.
        /// </param>
        /// <returns>The modified application.</returns>
        public static IAppBuilder UseJwtAuthentication(
            this IAppBuilder app,
            JwtAuthenticationSettings settings = null)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var s = settings ?? app.GetInstance<JwtAuthenticationSettings>();

            if (s == null)
            {
                throw new InvalidOperationException(Messages.Exception_UseJwtAuthentication_NoSettingsProvided);
            }

            if (s.RelativeFileCertificate != null && s.StoreCertificate != null)
            {
                throw new InvalidOperationException(Messages.Exception_UseJwtAuthentication_MultipleOptionsProvided);
            }

            var privateKey = GetPrivateKey(s);

            // Try decoding each secret so it will throw an exception early if there is a configuration problem
            s.AllowedServers.ForEach(server => TextEncodings.Base64Url.Decode(server.Secret));

            var authenticationOptions = new OAuthBearerAuthenticationOptions
            {
                AccessTokenFormat = new JoseJwtFormat(s, privateKey),
                AuthenticationType = "Bearer",
                Provider = new OAuthBearerAuthenticationProvider()
                {
                    OnRequestToken = async context => await RequestToken(context, s)
                }
            };

            return app.UseOAuthBearerAuthentication(authenticationOptions);
        }

        private static RSACryptoServiceProvider GetPrivateKey(JwtAuthenticationSettings settings)
        {
            var certificateFetcher =
                settings.RelativeFileCertificate != null
                    ? new FileCertificateFetcher(settings.RelativeFileCertificate)
                    : settings.StoreCertificate != null
                        ? new StoreByThumbprintCertificateFetcher(settings.StoreCertificate)
                        : null as ICertificateFetcher;

            RSACryptoServiceProvider privateKey = null;
            if (certificateFetcher != null)
            {
                var certificate = certificateFetcher.Fetch();

                if (certificate == null)
                {
                    throw new InvalidOperationException(Messages.Exception_UseJwtAuthentication_CertificateNotFound);
                }

                privateKey = certificate.PrivateKey as RSACryptoServiceProvider;

                if (privateKey == null)
                {
                    throw new InvalidOperationException(Messages.Exception_UseJwtAuthentication_NoPrivateKey);
                }
            }
            return privateKey;
        }

        private static async Task RequestToken(OAuthRequestTokenContext requestTokenContext, JwtAuthenticationSettings settings)
        {
            var originalToken = requestTokenContext.Token;

            if (!string.IsNullOrWhiteSpace(settings.AuthorizationKey))
            {
                requestTokenContext.Token = null;

                // Late bound to capture originalToken
                Func<OAuthRequestTokenContext, string, Task> useHeader = (context, _) =>
                {
                    if (!string.IsNullOrWhiteSpace(originalToken))
                    {
                        context.Token = originalToken;
                    }

                    return Task.FromResult<object>(null);
                };

                var setTokenLookup = new Dictionary<AuthorizationSource, Func<OAuthRequestTokenContext, string, Task>>
                {
                    { AuthorizationSource.Header, useHeader },
                    { AuthorizationSource.Form, UseForm },
                    { AuthorizationSource.QueryString, UseQueryString }
                };

                var authorizationPriority = (settings.AuthorizationPriority.Count > 0)
                    ? settings.AuthorizationPriority
                    : new[] { AuthorizationSource.Header, AuthorizationSource.Form, AuthorizationSource.QueryString };

                // Apply them last to first so that first gets highest priority when setting value
                var setTokens = authorizationPriority.Reverse().Select(a => setTokenLookup[a]);

                foreach (var setToken in setTokens)
                {
                    await setToken(requestTokenContext, settings.AuthorizationKey);
                }
            }
        }

        private static async Task UseForm(OAuthRequestTokenContext context, string authorizationKey)
        {
            var form = await context.Request.ReadFormAsync();
            var formValue = form.Get(authorizationKey);
            context.Token = string.IsNullOrWhiteSpace(formValue) ? context.Token : formValue;
        }

        private static Task UseQueryString(OAuthRequestTokenContext context, string authorizationKey)
        {
            if (!string.IsNullOrWhiteSpace(context.Request.QueryString.Value))
            {
                var keyValues = context.Request.QueryString.Value.Split(
                    new[] { '&' },
                    StringSplitOptions.RemoveEmptyEntries);

                keyValues
                    .Select(kv => kv.Split(new [] { '=' }, StringSplitOptions.RemoveEmptyEntries))
                    .Where(kv => kv.Length >= 2 && string.Compare(kv[0].Trim(), authorizationKey, StringComparison.OrdinalIgnoreCase) == 0)
                    .ForEach(kv => context.Token = kv[1]);
            }

            return Task.FromResult<object>(null);
        }
    }
}
