// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultWebApiConfigTest.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using Its.Log.Instrumentation;
    using NSubstitute;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web.Http;
    using System.Web.Http.ExceptionHandling;

    [TestFixture]
    public class DefaultWebApiConfigTest
    {
        [Test]
        public void InitializeHttpConfigurationWith_null_does_not_register_exception_logger()
        {
            var intialize = DefaultWebApiConfig.InitializeHttpConfigurationWith(null);

            using (var httpConfiguration = new HttpConfiguration())
            {
                intialize(httpConfiguration);

                Assert.That(httpConfiguration.Services.GetExceptionLoggers().Count(), Is.EqualTo(0));
            }
        }

        [Test]
        public void InitializeHttpConfigurationWith_custom_exception_logger_registers_custom_exception_logger()
        {
            var expectedExceptionLogger = Substitute.For<IExceptionLogger>();
            var intialize = DefaultWebApiConfig.InitializeHttpConfigurationWith(expectedExceptionLogger);

            using (var httpConfiguration = new HttpConfiguration())
            {
                intialize(httpConfiguration);

                Assert.That(httpConfiguration.Services.GetExceptionLoggers().Contains(expectedExceptionLogger));
            }
        }

        [Test]
        public void InitializeHttpConfiguration_uses_default_ItsLogExceptionLogger()
        {
            using (var httpConfiguration = new HttpConfiguration())
            {
                DefaultWebApiConfig.InitializeHttpConfiguration(httpConfiguration);

                Assert.That(httpConfiguration.Services.GetExceptionLoggers().OfType<ItsLogExceptionLogger>().Count(), Is.EqualTo(1));
            }
        }

        [Test]
        public void InitializeHttpConfiguration_registers_default_route()
        {
            using (var httpConfiguration = new HttpConfiguration())
            {
                DefaultWebApiConfig.InitializeHttpConfiguration(httpConfiguration);

                Assert.That(httpConfiguration.Routes.Count(), Is.EqualTo(1));
            }
        }
    }
}
