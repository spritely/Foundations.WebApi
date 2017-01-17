// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonSerializerSettingsExtensions.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// A set of extensions to JsonSerializerSettings.
    /// </summary>
    public static class JsonSerializerSettingsExtensions
    {
        /// <summary>
        /// Adds a path string converter to the given JSON serializer settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <returns>The modified settings.</returns>
        /// <exception cref="System.ArgumentNullException">If settings is null.</exception>
        public static JsonSerializerSettings WithPathStringConverter(this JsonSerializerSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            settings.Converters.Add(new PathStringJsonConverter());

            return settings;
        }
    }
}
