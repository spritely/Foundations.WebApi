// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StartupConfiguration.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System.Collections.Generic;
    using Its.Configuration;
    using Newtonsoft.Json;
    using SimpleInjector;
    using SimpleInjector.Integration.WebApi;
    using Spritely.Recipes;

    /// <summary>
    /// A configuration object providing the ability to override default behaviors when initializing
    /// a Web API service.
    /// </summary>
    public class StartupConfiguration
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Instance lives for the lifetime of the application.")]
        private static Container CreateContainer()
        {
            var c = new Container();
            c.Options.DefaultScopedLifestyle = new WebApiRequestLifestyle();

            return c;
        }

        private Container container = null;

        /// <summary>
        /// Gets or sets the dependency injection container.
        /// </summary>
        /// <value>The dependency injection container.</value>
        public Container Container
        {
            get
            {
                return container = container ?? CreateContainer();
            }
            set
            {
                container = value;
            }
        }

        private JsonSerializerSettings defaultJsonSettings = null;

        /// <summary>
        /// Gets or sets the default json settings.
        /// </summary>
        /// <value>The default json settings.</value>
        public JsonSerializerSettings DefaultJsonSettings
        {
            get
            {
                return (defaultJsonSettings = defaultJsonSettings ?? JsonConfiguration.CompactSerializerSettings);
            }
            set
            {
                defaultJsonSettings = value;
            }
        }

        private DeserializeSettings deserializeConfigurationSettings = null;

        /// <summary>
        /// Gets or sets a function to deserialize configuration settings.
        /// </summary>
        /// <value>The deserialize configuration settings function.</value>
        public DeserializeSettings DeserializeConfigurationSettings
        {
            get
            {
                return (deserializeConfigurationSettings = deserializeConfigurationSettings ?? ((type, serialized) => JsonConvert.DeserializeObject(serialized, type, JsonConfiguration.CompactSerializerSettings)));
            }
            set
            {
                deserializeConfigurationSettings = value;
            }
        }

        private InitializeLogPolicy initializeLogPolicy = null;

        /// <summary>
        /// Gets or sets a function to initialize the log policy.
        /// </summary>
        /// <value>The initialize log policy function.</value>
        public InitializeLogPolicy InitializeLogPolicy
        {
            get
            {
                return (initializeLogPolicy = initializeLogPolicy ?? BasicWebApiLogPolicy.Initialize);
            }
            set
            {
                initializeLogPolicy = value;
            }
        }

        /// <summary>
        /// The container initializers.
        /// </summary>
        public ICollection<InitializeContainer> ContainerInitializers { get; } = new List<InitializeContainer>();

        /// <summary>
        /// The HTTP configuration initializers.
        /// </summary>
        public ICollection<InitializeHttpConfiguration> HttpConfigurationInitializers { get; } = new List<InitializeHttpConfiguration>();
    }
}
