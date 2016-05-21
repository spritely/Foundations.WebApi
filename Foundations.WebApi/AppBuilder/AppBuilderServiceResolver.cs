// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppBuilderServiceResolver.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using Owin;

    /// <summary>
    /// A default implementation for resolving instances from the OWIN AppBuilder.
    /// </summary>
    public class AppBuilderServiceResolver : IServiceResolver
    {
        private readonly IAppBuilder app;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppBuilderServiceResolver"/> class.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <exception cref="ArgumentNullException">If app is null.</exception>
        public AppBuilderServiceResolver(IAppBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            this.app = app;
        }

        /// <summary>
        /// Gets an instance by its service type.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <returns>
        /// The service instance.
        /// </returns>
        public TService GetInstance<TService>() where TService : class
        {
            return app.GetInstance<TService>();
        }

        /// <summary>
        /// Gets the instance by its service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns>
        /// The service instance.
        /// </returns>
        public object GetInstance(Type serviceType)
        {
            return app.GetInstance(serviceType);
        }
    }
}
