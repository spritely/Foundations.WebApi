// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FileCertificateFetcher.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Fetches certificates from files.
    /// </summary>
    /// <seealso cref="ICertificateFetcher"/>
    public class FileCertificateFetcher : ICertificateFetcher
    {
        private readonly RelativeFileCertificate fileCertificate;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileCertificateFetcher"/> class.
        /// </summary>
        /// <param name="fileCertificate">The file certificate.</param>
        /// <exception cref="System.ArgumentNullException">If any arguments are null.</exception>
        public FileCertificateFetcher(RelativeFileCertificate fileCertificate)
        {
            if (fileCertificate == null)
            {
                throw new ArgumentNullException(nameof(fileCertificate));
            }

            this.fileCertificate = fileCertificate;
        }

        /// <summary>
        /// Fetches a certificate.
        /// </summary>
        /// <returns>The certificate.</returns>
        public X509Certificate2 Fetch()
        {
            var certificate = new X509Certificate2(
                fileCertificate.FilePath,
                fileCertificate.Password,
                fileCertificate.KeyStorageFlags);

            return certificate;
        }
    }
}
