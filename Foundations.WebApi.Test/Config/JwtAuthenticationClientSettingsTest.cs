// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JwtAuthenticationClientSettingsTest.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using System.Linq;
    using System.Security.Cryptography;
    using Microsoft.Owin.Security.Jwt;
    using Microsoft.Owin.Security.DataHandler.Encoder;
    using NUnit.Framework;

    [TestFixture]
    public class JwtAuthenticationClientSettingsTest
    {
        [Test]
        public void Implicit_conversion_to_JwtBearerAuthenticationOptions_produces_expected_result()
        {
            var settings = new JwtAuthenticationClientSettings
            {
                Id = "Implicit_conversion_to_JwtBearerAuthenticationOptions_produces_expected_result",
                AllowedServers =
                {
                    new JwtAuthenticationServer { Issuer = "test1", Secret = CreateSecret() },
                    new JwtAuthenticationServer { Issuer = "test2", Secret = CreateSecret() }
                }
            };

            Func<JwtBearerAuthenticationOptions, JwtBearerAuthenticationOptions> implicitConversion = o => o;
            var options = implicitConversion(settings);

            Assert.That(options.AllowedAudiences.Count(), Is.EqualTo(1));
            Assert.That(options.AllowedAudiences.First(), Is.EqualTo(settings.Id));
            Assert.That(options.IssuerSecurityTokenProviders.Count(), Is.EqualTo(2));
            Assert.That(options.IssuerSecurityTokenProviders.First().Issuer, Is.EqualTo(settings.AllowedServers.First().Issuer));
            Assert.That(options.IssuerSecurityTokenProviders.Skip(1).First().Issuer, Is.EqualTo(settings.AllowedServers.Skip(1).First().Issuer));
        }

        [Test]
        public void ToJwtBearerAuthenticationOptions_produces_expected_result()
        {
            var settings = new JwtAuthenticationClientSettings
            {
                Id = "ToJwtBearerAuthenticationOptions_produces_expected_result",
                AllowedServers =
                {
                    new JwtAuthenticationServer { Issuer = "1test", Secret = CreateSecret() },
                    new JwtAuthenticationServer { Issuer = "2test", Secret = CreateSecret() }
                }
            };

            var options = settings.ToJwtBearerAuthenticationOptions();

            Assert.That(options.AllowedAudiences.Count(), Is.EqualTo(1));
            Assert.That(options.AllowedAudiences.First(), Is.EqualTo(settings.Id));
            Assert.That(options.IssuerSecurityTokenProviders.Count(), Is.EqualTo(2));
            Assert.That(options.IssuerSecurityTokenProviders.First().Issuer, Is.EqualTo(settings.AllowedServers.First().Issuer));
            Assert.That(options.IssuerSecurityTokenProviders.Skip(1).First().Issuer, Is.EqualTo(settings.AllowedServers.Skip(1).First().Issuer));
        }

        private string CreateSecret()
        {
            var key = new byte[32];
            using (var generator = RandomNumberGenerator.Create())
            {
                generator.GetBytes(key);
            }
            var base64Secret = TextEncodings.Base64Url.Encode(key);

            return base64Secret;
        }
    }
}