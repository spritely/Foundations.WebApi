// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItsLogExtensions.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using Its.Log.Instrumentation;
    using Owin;
    
    /// <summary>
    /// Its.Log extensions for IAppBuilder.
    /// </summary>
    public static class ItsLogExtensions
    {
        public static IAppBuilder UseRequestAndResponseLogging(this IAppBuilder app)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.Use(async (context, next) =>
            {
                Log.Write(context.Request);

                await next.Invoke();

                Log.Write(context.Response);
            });

            return app;
        }
    }
}
