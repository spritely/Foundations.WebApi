// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StoreCertificate.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Describes a certificate in a store.
    /// </summary>
    public class StoreCertificate
    {
        /// <summary>
        /// Gets or sets the name of the store. Defaults to StoreName.My.
        /// </summary>
        /// <value>
        /// The name of the store.
        /// </value>
        public StoreName StoreName { get; set; } = StoreName.My;

        /// <summary>
        /// Gets or sets the store location. Defaults to StoreLocation.LocalMachine.
        /// </summary>
        /// <value>
        /// The store location.
        /// </value>
        public StoreLocation StoreLocation { get; set; } = StoreLocation.LocalMachine;

        /// <summary>
        /// Gets or sets the certificate thumbprint.
        /// </summary>
        /// <value>
        /// The certificate thumbprint.
        /// </value>
        public string CertificateThumbprint { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether certificate validity is required. Defaults to true.
        /// </summary>
        /// <value>
        /// <c>true</c> if certificate validity is required; otherwise, <c>false</c>.
        /// </value>
        public bool CertificateValidityRequired { get; set; } = true;
    }
}