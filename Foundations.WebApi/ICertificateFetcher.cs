// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICertificateFetcher.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Represents an object capable of fetching certificates.
    /// </summary>
    public interface ICertificateFetcher
    {
        /// <summary>
        /// Fetches a certificate.
        /// </summary>
        /// <returns>The certificate.</returns>
        X509Certificate2 Fetch();
    }
}
