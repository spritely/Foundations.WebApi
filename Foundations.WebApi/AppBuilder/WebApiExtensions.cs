// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebApiExtensions.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Web.Http;
    using Owin;
    using SimpleInjector.Integration.WebApi;

    /// <summary>
    /// Web api extensions for IAppBuilder
    /// </summary>
    public static class WebApiExtensions
    {
        /// <summary>
        /// Sets up the application to use web API with the specified HTTP configuration initializers.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="httpConfigurationInitializers">The HTTP configuration initializers.</param>
        /// <returns>The modified application.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Disposal will be controlled by Web Api.")]
        public static IAppBuilder UseWebApiWithHttpConfigurationInitializers(this IAppBuilder app, params InitializeHttpConfiguration[] httpConfigurationInitializers)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            var initializers = httpConfigurationInitializers ?? new InitializeHttpConfiguration[] { };
            var httpConfiguration = new HttpConfiguration();
            foreach (var initialize in initializers)
            {
                initialize(httpConfiguration);
            }

            app.UseWebApi(httpConfiguration);

            var container = app.GetContainer();
            httpConfiguration.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(container);

            return app;
        }
    }
}
