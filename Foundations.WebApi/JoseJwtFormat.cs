// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JoseJwtFormat.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using Jose;
    using Microsoft.Owin.Security;
    using Microsoft.Owin.Security.DataHandler.Encoder;
    using Microsoft.Owin.Security.Jwt;

    /// <summary>
    /// Responsible for decrypting jose encrypted jwt tokens.
    /// </summary>
    /// <seealso cref="Microsoft.Owin.Security.Jwt.JwtFormat"/>
    public class JoseJwtFormat : ISecureDataFormat<AuthenticationTicket>
    {
        private readonly JwtBearerAuthenticationSettings settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="JoseJwtFormat"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <exception cref="System.ArgumentNullException">If any arguments are null.</exception>
        public JoseJwtFormat(JwtBearerAuthenticationSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            this.settings = settings;
        }

        /// <summary>
        /// Transforms the specified authentication ticket into a JWT.
        /// </summary>
        /// <param name="data">The authentication ticket to transform into a JWT.</param>
        /// <returns>Always throws NotSupportedException.</returns>
        /// <exception cref="System.NotSupportedException">JoseJwtFormat can only Unprotect.</exception>
        public string Protect(AuthenticationTicket data)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Validates the specified JWT and builds an AuthenticationTicket from it.
        /// </summary>
        /// <param name="protectedText">The JWT to validate.</param>
        /// <returns>An AuthenticationTicket built from the <paramref name="protectedText"/></returns>
        /// <exception cref="T:System.ArgumentNullException">
        /// Thrown if the <paramref name="protectedText"/> is null or empty.
        /// </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// Thrown if the <paramref name="protectedText"/> is not a JWT.
        /// </exception>
        public AuthenticationTicket Unprotect(string protectedText)
        {
            if (string.IsNullOrWhiteSpace(protectedText))
            {
                throw new ArgumentNullException(nameof(protectedText));
            }

            if (settings.RelativeFileCertificate != null && settings.StoreCertificate != null)
            {
                throw new InvalidOperationException(Messages.Exception_UnprotectJoseJwt_MultipleOptionsProvided);
            }

            var certificateFetcher =
                settings.RelativeFileCertificate != null
                    ? new FileCertificateFetcher(settings.RelativeFileCertificate)
                    : settings.StoreCertificate != null
                        ? new StoreByThumbprintCertificateFetcher(settings.StoreCertificate)
                        : null as ICertificateFetcher;

            var privateKey = certificateFetcher?.Fetch()?.PrivateKey as RSACryptoServiceProvider;
            var jwt = privateKey != null ? JWT.Decode(protectedText, privateKey) : protectedText;

            var securityTokenProviders = settings.AllowedServers.Select(
                    server => new SymmetricKeyIssuerSecurityTokenProvider(
                        server.Issuer,
                        TextEncodings.Base64Url.Decode(server.Secret)));

            var jwtFormat = new JwtFormat(settings.AllowedClients, securityTokenProviders);

            var ticket = jwtFormat.Unprotect(jwt);

            return ticket;
        }
    }
}
