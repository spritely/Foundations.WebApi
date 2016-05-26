// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContainerExtensionsTest.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using System.Linq;
    using Microsoft.Owin.Builder;
    using NUnit.Framework;
    using Owin;

    [TestFixture]
    public class ItsConfigurationExtensionsTest
    {
        [Test]
        public void UseSettingsContainerInitializer_throws_on_null_arguments()
        {
            Assert.Throws<ArgumentNullException>(() => (null as IAppBuilder).UseSettingsContainerInitializer());
        }

        [Test]
        public void UseSettingsContainerInitializer_sets_up_container_with_instances_for_settings_files()
        {
            var app = new AppBuilder();

            app.UseSettingsContainerInitializer();

            var settings = app.GetInstance<JwtBearerAuthenticationSettings>();

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.AllowedClients.Count, Is.EqualTo(1));
            Assert.That(settings.AllowedClients.First(), Is.EqualTo("test.client"));
            Assert.That(settings.AllowedServers.Count, Is.EqualTo(1));
            Assert.That(settings.AllowedServers.First().Issuer, Is.EqualTo("localhost"));
            Assert.That(settings.AllowedServers.First().Secret, Is.EqualTo("pu6txARocfowC1b3eNZEYuNcnTBGwEGfupX9kShMc8U"));
        }
    }
}
