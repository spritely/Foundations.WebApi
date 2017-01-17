// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StoreByThumbprintCertificateFetcher.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Globalization;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Fetches certificates from the X509 certificate store by thumbprint.
    /// </summary>
    /// <seealso cref="ICertificateFetcher"/>
    public class StoreByThumbprintCertificateFetcher : ICertificateFetcher
    {
        private readonly StoreCertificate storeCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="StoreByThumbprintCertificateFetcher"/> class.
        /// </summary>
        /// <param name="storeCertificate">The store certificate.</param>
        /// <exception cref="System.ArgumentNullException">If any arguments are null.</exception>
        public StoreByThumbprintCertificateFetcher(StoreCertificate storeCertificate)
        {
            if (storeCertificate == null)
            {
                throw new ArgumentNullException(nameof(storeCertificate));
            }

            this.storeCertificate = storeCertificate;
        }

        /// <summary>
        /// Fetches a certificate.
        /// </summary>
        /// <returns>The certificate.</returns>
        public X509Certificate2 Fetch()
        {
            var certificateStore = new X509Store(storeCertificate.StoreName, storeCertificate.StoreLocation);

            try
            {
                certificateStore.Open(OpenFlags.OpenExistingOnly);

                var thumbprint =
                    Regex.Replace(storeCertificate.CertificateThumbprint, @"[^\da-zA-z]", string.Empty)
                        .ToUpper(CultureInfo.InvariantCulture);

                var certificates = certificateStore.Certificates.Find(
                    X509FindType.FindByThumbprint,
                    thumbprint,
                    storeCertificate.CertificateValidityRequired);

                var certificate = certificates.Count > 0 ? certificates[0] : null;

                return certificate;
            }
            finally
            {
                certificateStore.Close();
            }
        }
    }
}
