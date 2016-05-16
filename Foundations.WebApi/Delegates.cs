// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Delegates.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System.Web.Http;
    using SimpleInjector;

    /// <summary>
    /// Delegate representing a container initialization function.
    /// </summary>
    /// <param name="container">The container.</param>
    public delegate void InitializeContainer(Container container);

    /// <summary>
    /// Delegate representing log policy initialization function.
    /// </summary>
    public delegate void InitializeLogPolicy();

    /// <summary>
    /// Delegate representing an HTTP configuration initialization function.
    /// </summary>
    /// <param name="httpConfiguration">The HTTP configuration.</param>
    public delegate void InitializeHttpConfiguration(HttpConfiguration httpConfiguration);
}
