// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultWebApiConfig.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Net.Http.Formatting;
    using System.Web.Http;
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
        public static InitializeHttpConfiguration InitializeHttpConfiguration { get; } = config => new DefaultWebApiConfig().Register(config);

        /// <summary>
        /// An implementation to initialize the HTTP configuration with a custom ExceptionLogger.
        /// </summary>
        public static Func<IExceptionLogger, InitializeHttpConfiguration> InitializeHttpConfigurationWith { get; } = exceptionLogger =>
           {
               var webApiConfig = new DefaultWebApiConfig
               {
                   ExceptionLogger = exceptionLogger
               };

               return config => webApiConfig.Register(config);
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
        public void Register(HttpConfiguration httpConfiguration)
        {
            if (httpConfiguration == null)
            {
                throw new ArgumentNullException(nameof(httpConfiguration));
            }

            var jsonFormatter = httpConfiguration.Formatters.JsonFormatter;
            jsonFormatter.MediaTypeMappings.Add(
                new RequestHeaderMapping("Accept", "text/html", StringComparison.OrdinalIgnoreCase, true, "application/json"));

            jsonFormatter.SerializerSettings = JsonConfiguration.CompactSerializerSettings;

            httpConfiguration.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "{controller}/{id}",
                defaults: new { id = RouteParameter.Optional, action = "Get" });

            if (ExceptionLogger != null)
            {
                httpConfiguration.Services.Add(typeof(IExceptionLogger), ExceptionLogger);
            }
        }
    }
}
