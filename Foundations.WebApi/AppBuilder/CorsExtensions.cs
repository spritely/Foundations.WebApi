// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CorsExtensions.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Web.Cors;
    using Microsoft.Owin.Cors;
    using Owin;
    using Spritely.Recipes;

    /// <summary>
    /// Its.Configuration extensions for IAppBuilder.
    /// </summary>
    public static class CorsExtensions
    {
        public static IAppBuilder UseCors(this IAppBuilder app)
        {
            var hostingSettings = app.GetInstance<HostingSettings>();
            var cors = hostingSettings.Cors;

            if (cors == null || !cors.Origins.Any())
            {
                throw new InvalidOperationException(Messages.Exception_UseCors_NoSettingsProvided);
            }

            var corsPolicy = new CorsPolicy
            {
                SupportsCredentials = cors.SupportsCredentials,
                PreflightMaxAge = cors.PreflightMaxAge
            };

            cors.Origins.ForEach(corsPolicy.Origins.Add);
            cors.Headers.ForEach(corsPolicy.Headers.Add);
            cors.Methods.ForEach(corsPolicy.Methods.Add);
            cors.ExposedHeaders.ForEach(corsPolicy.ExposedHeaders.Add);

            if (cors.Origins.FirstOrDefault(o => o == "*") != null)
            {
                corsPolicy.AllowAnyOrigin = true;
            }

            if (cors.Headers.FirstOrDefault(h => h == "*") != null)
            {
                corsPolicy.AllowAnyHeader = true;
            }

            if (cors.Methods.FirstOrDefault(m => m == "*") != null)
            {
                corsPolicy.AllowAnyMethod = true;
            }

            app.UseCors(new CorsOptions
            {
                PolicyProvider = new CorsPolicyProvider
                {
                    PolicyResolver = request => Task.FromResult(corsPolicy)
                }
            });

            return app;
        }
    }
}
