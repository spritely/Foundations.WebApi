// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContainerExtensions.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Collections.Generic;
    using Owin;
    using SimpleInjector;

    /// <summary>
    /// Dependency Injection container extensions for IAppBuilder.
    /// </summary>
    public static class ContainerExtensions
    {
        private const string ContainerInitializersKey = "Spritely.Foundations.WebApi.AppBuilder.ContainerIntializers";
        private const string ContainerKey = "Spritely.Foundations.WebApi.AppBuilder.Container";

        /// <summary>
        /// Sets up the application to use the specified container initializer.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="initializeContainer">The container initializer delegate.</param>
        /// <returns>The modified application.</returns>
        public static IAppBuilder UseContainerInitializer(this IAppBuilder app, InitializeContainer initializeContainer)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (initializeContainer == null)
            {
                throw new ArgumentNullException(nameof(initializeContainer));
            }

            var containerInitializers = app.GetContainerInitializers();
            containerInitializers.Add(initializeContainer);

            return app;
        }

        /// <summary>
        /// Gets an instance from the application's dependency injection container.
        /// </summary>
        /// <typeparam name="TService">The type of service instance to get.</typeparam>
        /// <param name="app">The application.</param>
        /// <returns>The specified service instance.</returns>
        public static TService GetInstance<TService>(this IAppBuilder app) where TService: class
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var container = app.GetContainer();
            var result = container.GetInstance<TService>();

            return result;
        }

        /// <summary>
        /// Gets an instance from the application's dependency injection container.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="serviceType">Type of the service instance to get.</param>
        /// <returns>The specified service instance.</returns>
        public static object GetInstance(this IAppBuilder app, Type serviceType)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            var container = app.GetContainer();
            var result = container.GetInstance(serviceType);

            return result;
        }

        private static ICollection<InitializeContainer> GetContainerInitializers(this IAppBuilder app)
        {
            if (!app.Properties.ContainsKey(ContainerInitializersKey))
            {
                app.Properties[ContainerInitializersKey] = new List<InitializeContainer>();
            }

            var containerInitializers = app.Properties[ContainerInitializersKey] as ICollection<InitializeContainer>;

            return containerInitializers;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "This instance is designed to live for the life of the application.")]
        internal static Container GetContainer(this IAppBuilder app)
        {
            if (!app.Properties.ContainsKey(ContainerKey))
            {
                var c = new Container();

                var containerInitializers = app.GetContainerInitializers();
                foreach (var initializeContainer in containerInitializers)
                {
                    initializeContainer(c);
                }

                c.Verify();

                app.Properties[ContainerKey] = c;
            }

            var container = app.Properties[ContainerKey] as Container;
            return container;
        }
    }
}
