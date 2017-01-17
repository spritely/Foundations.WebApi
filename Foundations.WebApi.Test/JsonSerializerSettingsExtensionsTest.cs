// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JsonSerializerSettingsExtensionsTest.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System.Linq;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]
    public class JsonSerializerExtensionsTest
    {
        [Test]
        public void WithPathStringConverter_adds_path_string_converter_to_settings()
        {
            var settings = new JsonSerializerSettings().WithPathStringConverter();

            Assert.That(settings.Converters.OfType<PathStringJsonConverter>().Count(), Is.EqualTo(1));
        }
    }
}
