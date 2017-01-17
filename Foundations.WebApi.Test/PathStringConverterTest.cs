// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PathStringConverterTest.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using Microsoft.Owin;
    using Newtonsoft.Json;
    using NUnit.Framework;

    [TestFixture]
    public class PathStringConverterTest
    {
        [Test]
        public void JsonConvert_can_serialize_PathString_value()
        {
            var serialized = JsonConvert.SerializeObject(new PathString("/test"), new PathStringJsonConverter());

            Assert.That(serialized, Is.EqualTo(@"""/test"""));
        }

        [Test]
        public void JsonConvert_can_deserialize_PathString_value()
        {
            var deserialized = JsonConvert.DeserializeObject<PathString>(@"""/test""", new PathStringJsonConverter());

            Assert.That(deserialized, Is.EqualTo(new PathString("/test")));
        }
    }
}
