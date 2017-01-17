// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JwtAuthenticationSettings.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Provides settings for a JWT authentication client.
    /// </summary>
    public class JwtAuthenticationSettings
    {
        /// <summary>
        /// Gets or sets the allowed clients.
        /// </summary>
        /// <value>The allowed clients.</value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is designed for serialization and deserialization to and from JSON which needs to be able to set this property.")]
        public ICollection<string> AllowedClients { get; set; } = new List<string>();

        /// <summary>
        /// Gets the allowed servers.
        /// </summary>
        /// <value>The allowed servers.</value>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "This is designed for serialization and deserialization to and from JSON which needs to be able to set this property.")]
        public ICollection<JwtAuthenticationServer> AllowedServers { get; set; } = new List<JwtAuthenticationServer>();

        /// <summary>
        /// Gets or sets the relative file certificate.
        /// </summary>
        /// <value>The relative file certificate.</value>
        public RelativeFileCertificate RelativeFileCertificate { get; set; }

        /// <summary>
        /// Gets or sets the store certificate.
        /// </summary>
        /// <value>The store certificate.</value>
        public StoreCertificate StoreCertificate { get; set; }

        /// <summary>
        /// Gets or sets the authorization key for form or query string values.
        /// </summary>
        /// <value>
        /// The authorization key for form or query string values.
        /// </value>
        public string AuthorizationKey { get; set; }

        /// <summary>
        /// Gets the authorization priority.
        /// </summary>
        /// <value>
        /// The authorization priority.
        /// </value>
        public ICollection<AuthorizationSource> AuthorizationPriority { get; } = new List<AuthorizationSource>(); 
    }
}
