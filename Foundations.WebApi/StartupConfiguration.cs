// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StartupConfiguration.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using Its.Configuration;
    using Newtonsoft.Json;
    using Spritely.Recipes;

    /// <summary>
    /// A configuration object providing the ability to override default behaviors when initializing
    /// a Web API service.
    /// </summary>
    public class StartupConfiguration
    {
        private JsonSerializerSettings defaultJsonSettings = null;

        /// <summary>
        /// Gets or sets the default json settings.
        /// </summary>
        /// <value>The default json settings.</value>
        public JsonSerializerSettings DefaultJsonSettings
        {
            get
            {
                return defaultJsonSettings = defaultJsonSettings ?? JsonConfiguration.CompactSerializerSettings.WithPathStringConverter();
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
                return deserializeConfigurationSettings = deserializeConfigurationSettings ?? ((type, serialized) => JsonConvert.DeserializeObject(serialized, type, DefaultJsonSettings));
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
    }
}
