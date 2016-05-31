// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Start.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using Its.Configuration;
    using Its.Log.Instrumentation;
    using Microsoft.Owin.Hosting;
    using Newtonsoft.Json;
    using System;
    using System.Globalization;
    using Microsoft.Owin.Hosting.Tracing;

    /// <summary>
    /// An object used to Start a Web API service.
    /// </summary>
    public static class Start
    {
        /// <summary>
        /// Initializes with the specified startup configuration.
        /// </summary>
        /// <param name="startupConfiguration">The startup configuration.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to catch all exceptions when an application first starts up.")]
        public static void Initialize(StartupConfiguration startupConfiguration = null)
        {
            try
            {
                var configuration = startupConfiguration ?? new StartupConfiguration();
                JsonConvert.DefaultSettings = () => configuration.DefaultJsonSettings;
                Settings.Deserialize = configuration.DeserializeConfigurationSettings;

                configuration.InitializeLogPolicy();
                Log.Write(Messages.Application_Started);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }

        /// <summary>
        /// Call this from your Startup.Main method to provide a default console implementation with
        /// a specified startup configuration.
        /// </summary>
        /// <typeparam name="TStartup">
        /// The type of the startup object (this is normally the Startup type).
        /// </typeparam>
        /// <param name="startupConfiguration">The startup configuration.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "This is designed to mimic the underlying WebApi interface.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to catch all exceptions.")]
        public static void Console<TStartup>(StartupConfiguration startupConfiguration = null)
        {
            try
            {
                var configuration = startupConfiguration ?? new StartupConfiguration();
                Settings.Deserialize = configuration.DeserializeConfigurationSettings;
                var hostingSettings = Settings.Get<HostingSettings>();
                var startOptions = new StartOptions(hostingSettings.Url);

                // Disable built-in owin tracing by using a null trace output
                // see: https://stackoverflow.com/questions/37527531/owin-testserver-logs-multiple-times-while-testing-how-can-i-fix-this/37548074#37548074
                // and: http://stackoverflow.com/questions/17948363/tracelistener-in-owin-self-hosting
                startOptions.Settings.Add(
                    typeof(ITraceOutputFactory).FullName,
                    typeof(NullTraceOutputFactory).AssemblyQualifiedName);

                using (WebApp.Start<TStartup>(startOptions))
                {
                    Log.Write(string.Format(CultureInfo.InvariantCulture, Messages.Web_Server_Running, hostingSettings.Url));
                    System.Console.WriteLine(Messages.Quit);
                    System.Console.ReadKey();
                    Log.Write(string.Format(CultureInfo.InvariantCulture, Messages.Web_Server_Terminated, hostingSettings.Url));
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
                System.Console.Write(ex);
            }
        }
    }
}
