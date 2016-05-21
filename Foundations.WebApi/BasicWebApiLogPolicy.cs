// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BasicWebApiLogPolicy.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Web.Http.ExceptionHandling;
    using Its.Log.Instrumentation;
    using Microsoft.Owin;

    /// <summary>
    /// Container for static log policy initialization logic.
    /// </summary>
    public static class BasicWebApiLogPolicy
    {
        /// <summary>
        /// Initializes the log policy to write output to Tracing and registers WebApi's
        /// ExceptionLoggerContext object for additional output
        /// </summary>
        public static void Initialize()
        {
            Formatter<OwinRequest>.RegisterForMembers(
                o => o.Method,
                o => o.Uri,
                o => o.Headers);

            Formatter<OwinResponse>.RegisterForMembers(
                o => o.StatusCode,
                o => o.ReasonPhrase,
                o => o.Headers);

            Formatter<HeaderDictionary>.Register(d => string.Join("; ", d.Keys.Select(k => k + ": " + string.Join(", ", d.GetValues(k)))));

            Formatter<ExceptionLoggerContext>.RegisterForAllMembers();

            Log.EntryPosted += (sender, args) =>
            {
                var subject = args.LogEntry.Subject ?? string.Empty;
                var message = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture) + ": " + subject.ToLogString();

                Trace.WriteLine(message);
            };
        }
    }
}
