// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IServiceResolver.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;

    /// <summary>
    /// Represents an object capable of resolving service instances.
    /// </summary>
    public interface IServiceResolver
    {
        /// <summary>
        /// Gets an instance by its service type.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <returns>The service instance.</returns>
        TService GetInstance<TService>() where TService : class;

        /// <summary>
        /// Gets the instance by its service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns>The service instance.</returns>
        object GetInstance(Type serviceType);
    }
}
