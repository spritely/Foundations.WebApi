// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultWebApiConfigTest.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using System.Linq;
    using System.Web.Http;
    using System.Web.Http.ExceptionHandling;
    using Newtonsoft.Json.Serialization;
    using NSubstitute;
    using NUnit.Framework;
    using Spritely.Recipes;

    [TestFixture]
    public class DefaultWebApiConfigTest
    {
        [Test]
        public void InitializeHttpConfigurationWith_null_does_not_register_exception_logger()
        {
            var intialize = DefaultWebApiConfig.InitializeHttpConfigurationWith(null);
            var resolver = Substitute.For<IServiceResolver>();

            using (var httpConfiguration = new HttpConfiguration())
            {
                intialize(httpConfiguration, resolver);

                Assert.That(httpConfiguration.Services.GetExceptionLoggers().Count(), Is.EqualTo(0));
            }
        }

        [Test]
        public void InitializeHttpConfigurationWith_custom_exception_logger_registers_custom_exception_logger()
        {
            var expectedExceptionLogger = Substitute.For<IExceptionLogger>();
            var resolver = Substitute.For<IServiceResolver>();
            var intialize = DefaultWebApiConfig.InitializeHttpConfigurationWith(expectedExceptionLogger);

            using (var httpConfiguration = new HttpConfiguration())
            {
                intialize(httpConfiguration, resolver);

                Assert.That(httpConfiguration.Services.GetExceptionLoggers().Contains(expectedExceptionLogger));
            }
        }

        [Test]
        public void InitializeHttpConfiguration_uses_default_ItsLogExceptionLogger()
        {
            var resolver = Substitute.For<IServiceResolver>();

            using (var httpConfiguration = new HttpConfiguration())
            {
                DefaultWebApiConfig.InitializeHttpConfiguration(httpConfiguration, resolver);

                Assert.That(httpConfiguration.Services.GetExceptionLoggers().OfType<ItsLogExceptionLogger>().Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void InitializeHttpConfiguration_registers_default_route()
        {
            var resolver = Substitute.For<IServiceResolver>();

            using (var httpConfiguration = new HttpConfiguration())
            {
                DefaultWebApiConfig.InitializeHttpConfiguration(httpConfiguration, resolver);

                Assert.That(httpConfiguration.Routes.Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void Register_throws_on_null_arguments()
        {
            var defaultWebApiConfig = new DefaultWebApiConfig();
            var resolver = Substitute.For<IServiceResolver>();

            Assert.Throws<ArgumentNullException>(() => defaultWebApiConfig.Register(null, resolver));

            using (var configuration = new HttpConfiguration())
            {
                Assert.Throws<ArgumentNullException>(() => defaultWebApiConfig.Register(configuration, null));
            }
        }

        [Test]
        public void Register_configures_json_formatter()
        {
            var defaultWebApiConfig = new DefaultWebApiConfig();
            var resolver = Substitute.For<IServiceResolver>();

            using (var httpConfiguration = new HttpConfiguration())
            {
                defaultWebApiConfig.Register(httpConfiguration, resolver);

                var jsonFormatter = httpConfiguration.Formatters.JsonFormatter;
                
                Assert.That(jsonFormatter.SerializerSettings.ContractResolver, Is.TypeOf<CamelStrictConstructorContractResolver>());
            }
        }
    }
}
