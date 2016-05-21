// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultWebApiConfig.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Linq;
    using System.Net.Http.Formatting;
    using System.Web.Http;
    using System.Web.Http.Cors;
    using System.Web.Http.Dispatcher;
    using System.Web.Http.ExceptionHandling;
    using Spritely.Recipes;

    /// <summary>
    /// A default implementation for registering HTTP configuration information for hosting a Web API service.
    /// </summary>
    public class DefaultWebApiConfig
    {
        /// <summary>
        /// A default implementation to initialize the HTTP configuration with a new
        /// DefaultWebApiConfig project with an ItsLogExceptionHandler.
        /// </summary>
        public static InitializeHttpConfiguration InitializeHttpConfiguration { get; } = (config, resolver) => new DefaultWebApiConfig().Register(config, resolver);

        /// <summary>
        /// An implementation to initialize the HTTP configuration with a custom ExceptionLogger.
        /// </summary>
        public static Func<IExceptionLogger, InitializeHttpConfiguration> InitializeHttpConfigurationWith { get; } = exceptionLogger =>
           {
               var webApiConfig = new DefaultWebApiConfig
               {
                   ExceptionLogger = exceptionLogger
               };

               return (config, resolver) => webApiConfig.Register(config, resolver);
           };

        /// <summary>
        /// Gets or sets the exception logger.
        /// </summary>
        /// <value>The exception logger.</value>
        public IExceptionLogger ExceptionLogger { get; set; } = new ItsLogExceptionLogger();

        /// <summary>
        /// Registers some default values with the specified HTTP configuration.
        /// </summary>
        /// <param name="httpConfiguration">The HTTP configuration.</param>
        /// <param name="resolver">The resolver.</param>
        /// <exception cref="ArgumentNullException">If any arguments are null.</exception>
        public void Register(HttpConfiguration httpConfiguration, IServiceResolver resolver)
        {
            if (httpConfiguration == null)
            {
                throw new ArgumentNullException(nameof(httpConfiguration));
            }

            if (resolver == null)
            {
                throw new ArgumentNullException(nameof(resolver));
            }

            var jsonFormatter = httpConfiguration.Formatters.JsonFormatter;
            jsonFormatter.MediaTypeMappings.Add(
                new RequestHeaderMapping("Accept", "text/html", StringComparison.OrdinalIgnoreCase, true, "application/json"));

            jsonFormatter.SerializerSettings = JsonConfiguration.CompactSerializerSettings;

            httpConfiguration.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional });

            httpConfiguration.Services.Replace(typeof(IHttpControllerSelector), new ApiControllerSelector(httpConfiguration));

            if (ExceptionLogger != null)
            {
                httpConfiguration.Services.Add(typeof(IExceptionLogger), ExceptionLogger);
            }

            var hostingSettings = resolver.GetInstance<HostingSettings>();
            if (hostingSettings?.Cors != null && hostingSettings.Cors.Origins.Any())
            {
                var cors = hostingSettings.Cors;

                var corsPolicyProvider = new EnableCorsAttribute(
                            string.Join(",", cors.Origins),
                            string.Join(",", cors.Headers),
                            string.Join(",", cors.Methods),
                            string.Join(",", cors.ExposedHeaders))
                {
                    SupportsCredentials = cors.SupportsCredentials
                };

                if (cors.PreflightMaxAge.HasValue)
                {
                    corsPolicyProvider.PreflightMaxAge = cors.PreflightMaxAge.Value;
                }

                httpConfiguration.EnableCors(corsPolicyProvider);
            }
        }
    }
}
