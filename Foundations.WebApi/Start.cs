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
    using Owin;
    using Recipes;
    using SimpleInjector;
    using SimpleInjector.Integration.WebApi;
    using System;
    using System.Globalization;
    using System.Web.Http;

    /// <summary>
    /// An object used to Start a Web API service.
    /// </summary>
    public static class Start
    {
        private static void ConfigureJson(StartupConfiguration startupConfiguration)
        {
            JsonConvert.DefaultSettings = () => startupConfiguration.DefaultJsonSettings;
            Settings.Deserialize = startupConfiguration.DeserializeConfigurationSettings;
        }

        /// <summary>
        /// Call this from your Startup.Configuraion method to provide a default implementation with
        /// a specified startup configuration.
        /// </summary>
        /// <param name="startupConfiguration">The startup configuration.</param>
        /// <param name="appBuilder">The application builder.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is specifically designed to catch all exceptions.")]
        public static void Configuration(StartupConfiguration startupConfiguration, IAppBuilder appBuilder)
        {
            if (startupConfiguration == null)
            {
                throw new ArgumentNullException(nameof(startupConfiguration));
            }

            if (appBuilder == null)
            {
                throw new ArgumentNullException(nameof(appBuilder));
            }

            try
            {
                startupConfiguration.InitializeLogPolicy();
                Log.Write(Messages.Application_Starting);

                ConfigureJson(startupConfiguration);

                var container = new Container();
                container.Options.DefaultScopedLifestyle = new WebApiRequestLifestyle();
                startupConfiguration.ContainerInitializers.ForEach(initializeContainer => initializeContainer(container));

                var httpConfiguration = new HttpConfiguration();
                startupConfiguration.HttpConfigurationInitializers.ForEach(initializeHttpConfiguration => initializeHttpConfiguration(httpConfiguration));

                appBuilder.UseWebApi(httpConfiguration);

                container.RegisterWebApiControllers(httpConfiguration);
                container.Verify();

                httpConfiguration.DependencyResolver = new SimpleInjectorWebApiDependencyResolver(container);

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
        public static void Console<TStartup>(StartupConfiguration startupConfiguration)
        {
            try
            {
                ConfigureJson(startupConfiguration);
                var hostingSettings = Settings.Get<HostingSettings>();

                using (WebApp.Start<TStartup>(hostingSettings.Url))
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
