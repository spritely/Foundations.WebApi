﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JwtBearerAuthenticationSettings.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.Owin.Security.Jwt;
    using Microsoft.Owin.Security.DataHandler.Encoder;

    /// <summary>
    /// Provides settings for a JWT authentication client.
    /// </summary>
    public class JwtBearerAuthenticationSettings
    {
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets the allowed servers.
        /// </summary>
        /// <value>
        /// The allowed servers.
        /// </value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is designed for serialization and deserialization to and from JSON which needs to be able to set this property.")]
        public ICollection<JwtAuthenticationServer> AllowedServers { get; set; } = new List<JwtAuthenticationServer>();

        /// <summary>
        /// Performs an implicit conversion from <see cref="JwtBearerAuthenticationSettings"/> to <see cref="JwtBearerAuthenticationOptions"/>.
        /// </summary>
        /// <param name="settings">The JWT bearer authentication settings.</param>
        /// <returns>
        /// The result of the conversion.
        /// </returns>
        public static implicit operator JwtBearerAuthenticationOptions(JwtBearerAuthenticationSettings settings)
        {
            if (settings == null)
            {
                return null;
            }

            return new JwtBearerAuthenticationOptions
            {
                AllowedAudiences = new[] { settings.Id },
                IssuerSecurityTokenProviders = settings.AllowedServers.Select(s =>
                    new SymmetricKeyIssuerSecurityTokenProvider(s.Issuer, TextEncodings.Base64Url.Decode(s.Secret)))
            };
        }

        public JwtBearerAuthenticationOptions ToJwtBearerAuthenticationOptions()
        {
            return this;
        }
    }
}