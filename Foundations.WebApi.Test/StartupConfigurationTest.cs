// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StartupConfigurationTest.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using NUnit.Framework;
    using Spritely.Recipes;
    using SimpleInjector;
    using SimpleInjector.Integration.WebApi;

    [TestFixture]
    public class StartupConfigurationTest
    {
        [Test]
        public void DefaultJsonSettings_uses_value_if_provided()
        {
            var expected = JsonConfiguration.DefaultSerializerSettings;
            var configuration = new StartupConfiguration
            {
                DefaultJsonSettings = expected
            };

            Assert.That(configuration.DefaultJsonSettings, Is.SameAs(expected));
        }

        [Test]
        public void DeserializeConfigurationSettings_defaults_to_json_serialization()
        {
            var configuration = new StartupConfiguration();

            var result = configuration.DeserializeConfigurationSettings(typeof(TestType), @"{
    ""name"": ""Hello""
}") as TestType;

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("Hello"));
        }

        [Test]
        public void DeserializeConfigurationSettings_uses_value_if_provided()
        {
            Type type = null;
            string serialized = null;
            var expectedResult = new object();
            var configuration = new StartupConfiguration
            {
                DeserializeConfigurationSettings = (t, s) =>
                {
                    type = t;
                    serialized = s;

                    return expectedResult;
                }
            };

            var actualResult = configuration.DeserializeConfigurationSettings(typeof(TestType), "serialized");

            Assert.That(actualResult, Is.SameAs(expectedResult));
            Assert.That(serialized, Is.EqualTo("serialized"));
            Assert.That(type, Is.EqualTo(typeof(TestType)));
        }

        [Test]
        public void InitializeLogPolicy_defaults_to_BasicWebApiLogPolicy_Initialize()
        {
            var configuration = new StartupConfiguration();

            Assert.True(configuration.InitializeLogPolicy == BasicWebApiLogPolicy.Initialize);
        }

        [Test]
        public void InitializeLogPolicy_uses_value_if_provided()
        {
            bool executed = false;
            var configuration = new StartupConfiguration
            {
                InitializeLogPolicy = () =>
                {
                    executed = true;
                }
            };

            configuration.InitializeLogPolicy();

            Assert.That(executed, Is.True);
        }

        private class TestType
        {
            public string Name = null;
        }
    }
}
