// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WebApiTest.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Jose;
    using Microsoft.Owin;
    using Microsoft.Owin.Security.DataHandler.Encoder;
    using Microsoft.Owin.Testing;
    using NSubstitute;
    using NUnit.Framework;
    using Owin;
    using Spritely.Recipes;

    /// <summary>
    /// Tests of the overall WebApi interface
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Testing the main surface area for assembly touches on many scenarios.")]
    [TestFixture]
    public class WebApiTest
    {
        private static Action<IAppBuilder> configure;

        private static readonly Action<IAppBuilder> forCompressionTest = app =>
            app.UseGzipDeflateCompression()
                .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

        private static Action<IAppBuilder> ForAuthenticationTestWith(JwtAuthenticationSettings settings)
        {
            return app =>
                app.UseContainerInitializer(c => c.Register(() => settings))
                    .UseJwtAuthentication()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);
        }

        private readonly JwtAuthenticationSettings jwtAuthenticationSettings = new JwtAuthenticationSettings
        {
            AllowedClients = { "my-identifier" },
            AllowedServers =
            {
                new JwtAuthenticationServer
                {
                    Issuer = "http://auth.localhost",
                    Secret = "pu6txARocfowC1b3eNZEYuNcnTBGwEGfupX9kShMc8U"
                }
            }
        };

        private JwtAuthenticationSettings JwtAuthenticationSettingsWithAuthorizationKey => new JwtAuthenticationSettings
        {
            AllowedClients = { "my-identifier" },
            AllowedServers =
            {
                new JwtAuthenticationServer
                {
                    Issuer = "http://auth.localhost",
                    Secret = "pu6txARocfowC1b3eNZEYuNcnTBGwEGfupX9kShMc8U"
                }
            },
            AuthorizationKey = "Authorization"
        };

        private readonly JwtAuthenticationSettings encryptedJwtAuthenticationSettings = new JwtAuthenticationSettings
        {
            AllowedClients = { "my-identifier" },
            AllowedServers =
            {
                new JwtAuthenticationServer
                {
                    Issuer = "http://auth.localhost",
                    Secret = "pu6txARocfowC1b3eNZEYuNcnTBGwEGfupX9kShMc8U"
                }
            },
            RelativeFileCertificate = new RelativeFileCertificate
            {
                BasePath = AppDomain.CurrentDomain.BaseDirectory,
                RelativeFilePath = "Certificates\\TestCertificate.pfx",
                Password = "Test".ToSecureString(),
                KeyStorageFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet
            }
        };

        [Test]
        public async Task Defaults_to_returning_compact_json()
        {
            configure = app =>
                app.UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var response = await WithTestServer(server => GetAsync(server, "/compact-json"));

            response.AssertOkWithContent(@"{""null"":null,""object"":{""name"":""Mr. Me""}}");
        }

        [Test]
        public async Task InitializeHttpConfiguration_can_read_items_from_container()
        {
            IDependency actualDependency = null;
            configure = app =>
                app.UseContainerInitializer(c => c.Register<IDependency, Dependency>())
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration, (config, resolver) =>
                    {
                        actualDependency = resolver.GetInstance<IDependency>();
                    });

            await WithTestServer(server => GetAsync(server, "/compact-json"));

            Assert.That(actualDependency, Is.Not.Null);
        }

        [Test]
        public async Task Controllers_can_reference_direct_types_defined_in_the_container()
        {
            configure = app =>
                app.UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var response = await WithTestServer(server => GetAsync(server, "/direct-dependency"));

            response.AssertOkWithContent(@"""Dependency""");
        }

        [Test]
        public async Task Controllers_can_reference_indirect_types_defined_in_the_container()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register<IDependency, Dependency>())
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var response = await WithTestServer(server => GetAsync(server, "/indirect-dependency"));

            response.AssertOkWithContent(@"""Dependency""");
        }

        [Test]
        public async Task Controllers_can_reference_settings()
        {
            configure = app =>
                app.UseSettingsContainerInitializer()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var response = await WithTestServer(server => GetAsync(server, "/settings-dependency"));

            response.AssertOkWithContent(@"{""value"":""Test""}");
        }

        [Test]
        public void UseCors_throws_when_settings_are_not_defined()
        {
            configure = app =>
                Assert.Throws<InvalidOperationException>(() => app.UseCors());

            using (TestServer.Create<Startup>())
            {
            }

            configure = app =>
            {
                app.UseContainerInitializer(c => c.Register(() => new HostingSettings()));
                Assert.Throws<InvalidOperationException>(() => app.UseCors());
            };

            using (TestServer.Create<Startup>())
            {
            }

            configure = app =>
            {
                app.UseContainerInitializer(c => c.Register(() => new HostingSettings
                {
                    Cors = new Cors()
                }));
                Assert.Throws<InvalidOperationException>(() => app.UseCors());
            };

            using (TestServer.Create<Startup>())
            {
            }
        }

        [Test]
        public async Task Cors_rejects_request_without_matching_origin()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => CreateHostingSettingsWith("http://notlocalhost", "*")))
                    .UseCors()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://invalidorigin");
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task Cors_accepts_request_with_matching_origin()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => CreateHostingSettingsWith("http://notlocalhost", "*")))
                    .UseCors()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://notlocalhost");
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task Cors_rejects_request_without_matching_method()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => CreateHostingSettingsWith("http://notlocalhost", "POST")))
                    .UseCors()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://notlocalhost");
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test]
        public async Task Cors_accepts_request_with_matching_method()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => CreateHostingSettingsWith("http://notlocalhost", "POST")))
                    .UseCors()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var request = new OptionsRequestMessage("POST", "http://localhost/compact-json", origin: "http://notlocalhost");
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public async Task Cors_with_SupportsCredentials_adds_expected_header_to_response()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() =>
                    {
                        var settings = CreateHostingSettingsWith("http://notlocalhost", "*");
                        settings.Cors.SupportsCredentials = true;
                        return settings;
                    }))
                    .UseCors()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://notlocalhost");
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var header = response.Headers.Single(h => h.Key == "Access-Control-Allow-Credentials");
            Assert.That(header.Value.Single(), Is.EqualTo("true"));
        }

        [Test]
        public async Task Cors_with_PreflightMaxAge_adds_expected_header_to_response()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() =>
                {
                    var settings = CreateHostingSettingsWith("http://notlocalhost", "*");
                    settings.Cors.PreflightMaxAge = 311;
                    return settings;
                }))
                    .UseCors()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://notlocalhost");
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var header = response.Headers.Single(h => h.Key == "Access-Control-Max-Age");
            Assert.That(header.Value.Single(), Is.EqualTo("311"));
        }

        [Test]
        public void UseJwtBearerAuthentication_throws_when_certificate_configuration_is_ambiguous()
        {
            var settings = new JwtAuthenticationSettings
            {
                RelativeFileCertificate = new RelativeFileCertificate(),
                StoreCertificate = new StoreCertificate()
            };

            var app = Substitute.For<IAppBuilder>();
            Assert.Throws<InvalidOperationException>(() => app.UseJwtAuthentication(settings));
        }

        [Test]
        public void UseJwtBearerAuthentication_throws_when_certificate_cannot_be_loaded()
        {
            var settings = new JwtAuthenticationSettings
            {
                StoreCertificate = new StoreCertificate
                {
                    CertificateThumbprint = "invalidthumbprint"
                }
            };

            var app = Substitute.For<IAppBuilder>();
            Assert.Throws<InvalidOperationException>(() => app.UseJwtAuthentication(settings));
        }

        [Test]
        public void UseJwtBearerAuthentication_throws_when_certificate_does_not_contain_private_key()
        {
            var settings = new JwtAuthenticationSettings
            {
                RelativeFileCertificate = new RelativeFileCertificate
                {
                    BasePath = AppDomain.CurrentDomain.BaseDirectory,
                    RelativeFilePath = "Certificates\\TestCertificate.cer",
                    Password = "Test".ToSecureString(),
                    KeyStorageFlags = X509KeyStorageFlags.Exportable | X509KeyStorageFlags.MachineKeySet
                }
            };

            var app = Substitute.For<IAppBuilder>();
            Assert.Throws<InvalidOperationException>(() => app.UseJwtAuthentication(settings));
        }

        [Test]
        public void UseJwtBearerAuthentication_throws_when_any_server_secret_is_not_Base64UrlEncoded()
        {
            var settings = new JwtAuthenticationSettings
            {
                AllowedServers =
                {
                    new JwtAuthenticationServer
                    {
                        Issuer = "http://auth.localhost",
                        Secret = "pu6txARocfowC1b3eNZEYuNcnTBGwEGfupX9kShMc8U" // first one is valid
                    },
                    new JwtAuthenticationServer
                    {
                        Issuer = "http://auth2.localhost",
                        Secret = "invalid secret"
                    }
                }
            };

            var app = Substitute.For<IAppBuilder>();
            Assert.Throws<FormatException>(() => app.UseJwtAuthentication(settings));
        }

        [Test]
        public async Task Jwt_request_with_valid_token_is_accepted()
        {
            var accessToken = CreateJwtAccessTokenFor(jwtAuthenticationSettings);
            configure = ForAuthenticationTestWith(jwtAuthenticationSettings);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => SendAsync(server, request));

            AssertOkAuthorizationRequiredResponse(response);
        }

        [Test]
        public async Task Jwt_request_with_invalid_token_is_rejected()
        {
            configure = ForAuthenticationTestWith(jwtAuthenticationSettings);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer invalidToken");

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_with_valid_token_in_form_is_accepted()
        {
            var accessToken = CreateJwtAccessTokenFor(JwtAuthenticationSettingsWithAuthorizationKey);
            configure = ForAuthenticationTestWith(JwtAuthenticationSettingsWithAuthorizationKey);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required")
            {
                Content = new StringContent("authorization=" + accessToken, Encoding.UTF8)
            };

            var response = await WithTestServer(server => SendAsync(server, request));

            AssertOkAuthorizationRequiredResponse(response);
        }

        [Test]
        public async Task Jwt_request_with_invalid_token_in_form_is_rejected()
        {
            configure = ForAuthenticationTestWith(JwtAuthenticationSettingsWithAuthorizationKey);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required")
            {
                Content = new StringContent("authorization=invalidToken", Encoding.UTF8)
            };

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_with_valid_token_in_uri_is_accepted()
        {
            var accessToken = CreateJwtAccessTokenFor(JwtAuthenticationSettingsWithAuthorizationKey);
            configure = ForAuthenticationTestWith(JwtAuthenticationSettingsWithAuthorizationKey);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required?authorization=" + accessToken);
            var response = await WithTestServer(server => SendAsync(server, request));

            AssertOkAuthorizationRequiredResponse(response);
        }

        [Test]
        public async Task Jwt_request_with_invalid_token_in_uri_is_rejected()
        {
            configure = ForAuthenticationTestWith(JwtAuthenticationSettingsWithAuthorizationKey);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required?authorization=invalidToken");

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_prioritizes_authorization_from_form_over_uri_by_default()
        {
            var validToken = CreateJwtAccessTokenFor(JwtAuthenticationSettingsWithAuthorizationKey);
            configure = ForAuthenticationTestWith(JwtAuthenticationSettingsWithAuthorizationKey);

            var validFormRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required?authorization=invalidtoken")
            {
                Content = new StringContent("authorization=" + validToken, Encoding.UTF8)
            };

            var validUriRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required?authorization=" + validToken)
            {
                Content = new StringContent("authorization=invalidToken", Encoding.UTF8)
            };

            var responses = await SendRequests(goodRequest: validFormRequest, badRequest: validUriRequest);

            AssertOkAuthorizationRequiredResponse(responses.Good);
            Assert.That(responses.Bad.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_prioritizes_authorization_from_header_over_uri_by_default()
        {
            var validToken = CreateJwtAccessTokenFor(JwtAuthenticationSettingsWithAuthorizationKey);
            configure = ForAuthenticationTestWith(JwtAuthenticationSettingsWithAuthorizationKey);

            var validHeaderRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required?authorization=invalidtoken");
            validHeaderRequest.Headers.Add("Authorization", "bearer " + validToken);

            var validUriRequest = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required?authorization=" + validToken);
            validUriRequest.Headers.Add("Authorization", "bearer invalidToken");

            var responses = await SendRequests(goodRequest: validHeaderRequest, badRequest: validUriRequest);

            AssertOkAuthorizationRequiredResponse(responses.Good);
            Assert.That(responses.Bad.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "formby", Justification = "This word does not appear in the name unless _ is removed; it does not represent hungarian notation.")]
        [Test]
        public async Task Jwt_request_prioritizes_authorization_from_header_over_form_by_default()
        {
            var validToken = CreateJwtAccessTokenFor(JwtAuthenticationSettingsWithAuthorizationKey);
            configure = ForAuthenticationTestWith(JwtAuthenticationSettingsWithAuthorizationKey);

            var validHeaderRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required")
            {
                Content = new StringContent("authorization=invalidToken", Encoding.UTF8)
            };
            validHeaderRequest.Headers.Add("Authorization", "bearer " + validToken);

            var validFormRequest = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required")
            {
                Content = new StringContent("authorization=" + validToken, Encoding.UTF8)
            };
            validFormRequest.Headers.Add("Authorization", "bearer invalidToken");

            var responses = await SendRequests(goodRequest: validHeaderRequest, badRequest: validFormRequest);

            AssertOkAuthorizationRequiredResponse(responses.Good);
            Assert.That(responses.Bad.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_missing_header_authorization_priority_is_rejected()
        {
            var settings = JwtAuthenticationSettingsWithAuthorizationKey;
            settings.AuthorizationPriority.Add(AuthorizationSource.Form);
            settings.AuthorizationPriority.Add(AuthorizationSource.QueryString);
            var validToken = CreateJwtAccessTokenFor(settings);
            configure = ForAuthenticationTestWith(settings);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + validToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_missing_form_authorization_priority_is_rejected()
        {
            var settings = JwtAuthenticationSettingsWithAuthorizationKey;
            settings.AuthorizationPriority.Add(AuthorizationSource.Header);
            settings.AuthorizationPriority.Add(AuthorizationSource.QueryString);
            var validToken = CreateJwtAccessTokenFor(settings);
            configure = ForAuthenticationTestWith(settings);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required")
            {
                Content = new StringContent("authorization=" + validToken, Encoding.UTF8)
            };

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_missing_query_string_authorization_priority_is_rejected()
        {
            var settings = JwtAuthenticationSettingsWithAuthorizationKey;
            settings.AuthorizationPriority.Add(AuthorizationSource.Header);
            settings.AuthorizationPriority.Add(AuthorizationSource.Form);
            var validToken = CreateJwtAccessTokenFor(settings);
            configure = ForAuthenticationTestWith(settings);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required?authorization=" + validToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_respects_authorization_priority_of_Header()
        {
            var settings = JwtAuthenticationSettingsWithAuthorizationKey;
            settings.AuthorizationPriority.Add(AuthorizationSource.Header);
            settings.AuthorizationPriority.Add(AuthorizationSource.Form);
            settings.AuthorizationPriority.Add(AuthorizationSource.QueryString);
            var validToken = CreateJwtAccessTokenFor(settings);
            configure = ForAuthenticationTestWith(settings);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required?authorization=invalidToken1")
            {
                Content = new StringContent("authorization=invalidToken2", Encoding.UTF8)
            };
            request.Headers.Add("Authorization", "bearer " + validToken);

            var response = await WithTestServer(server => SendAsync(server, request));

            AssertOkAuthorizationRequiredResponse(response);
        }

        [Test]
        public async Task Jwt_request_respects_authorization_priority_of_Form()
        {
            var settings = JwtAuthenticationSettingsWithAuthorizationKey;
            settings.AuthorizationPriority.Add(AuthorizationSource.Form);
            settings.AuthorizationPriority.Add(AuthorizationSource.Header);
            settings.AuthorizationPriority.Add(AuthorizationSource.QueryString);
            var validToken = CreateJwtAccessTokenFor(settings);
            configure = ForAuthenticationTestWith(settings);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required?authorization=invalidToken1")
            {
                Content = new StringContent("authorization=" + validToken, Encoding.UTF8)
            };
            request.Headers.Add("Authorization", "bearer invalidToken2");

            var response = await WithTestServer(server => SendAsync(server, request));

            AssertOkAuthorizationRequiredResponse(response);
        }

        [Test]
        public async Task Jwt_request_respects_authorization_priority_of_QueryString()
        {
            var settings = JwtAuthenticationSettingsWithAuthorizationKey;
            settings.AuthorizationPriority.Add(AuthorizationSource.QueryString);
            settings.AuthorizationPriority.Add(AuthorizationSource.Header);
            settings.AuthorizationPriority.Add(AuthorizationSource.Form);
            var validToken = CreateJwtAccessTokenFor(settings);
            configure = ForAuthenticationTestWith(settings);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/authorization-required?authorization=" + validToken)
            {
                Content = new StringContent("authorization=invalidToken1", Encoding.UTF8)
            };
            request.Headers.Add("Authorization", "bearer invalidToken2");

            var response = await WithTestServer(server => SendAsync(server, request));

            AssertOkAuthorizationRequiredResponse(response);
        }

        [Test]
        public async Task Jwt_request_with_invalid_secret_is_rejected()
        {
            configure = ForAuthenticationTestWith(jwtAuthenticationSettings);

            var accessToken = CreateJwtAccessTokenFor(
                jwtAuthenticationSettings.AllowedServers.First().Issuer,
                jwtAuthenticationSettings.AllowedClients.First(),
                secret: "HayCkqRlBqeILBmvywxwzWsANzQ5YQQaJdjnDPR5CW0");

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_with_invalid_issuer_is_rejected()
        {
            configure = ForAuthenticationTestWith(jwtAuthenticationSettings);

            var accessToken = CreateJwtAccessTokenFor(
                "http://invalid-issuer.localhost",
                jwtAuthenticationSettings.AllowedClients.First(),
                secret: jwtAuthenticationSettings.AllowedServers.First().Secret);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_with_invalid_client_id_is_rejected()
        {
            configure = ForAuthenticationTestWith(jwtAuthenticationSettings);

            var accessToken = CreateJwtAccessTokenFor(
                jwtAuthenticationSettings.AllowedServers.First().Issuer,
                "invalid-client-id",
                secret: jwtAuthenticationSettings.AllowedServers.First().Secret);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Jwt_request_with_expired_token_is_rejected()
        {
            configure = ForAuthenticationTestWith(jwtAuthenticationSettings);

            var accessToken = CreateJwtAccessTokenFor(
                jwtAuthenticationSettings.AllowedServers.First().Issuer,
                jwtAuthenticationSettings.AllowedClients.First(),
                expires: DateTime.UtcNow.AddSeconds(1),
                secret: jwtAuthenticationSettings.AllowedServers.First().Secret);

            await Task.Delay(TimeSpan.FromSeconds(1.1));

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Encrypted_jwt_request_with_valid_token_is_accepted()
        {
            var certificate = new FileCertificateFetcher(encryptedJwtAuthenticationSettings.RelativeFileCertificate).Fetch();
            configure = ForAuthenticationTestWith(encryptedJwtAuthenticationSettings);

            var accessToken = CreateJwtAccessTokenFor(
                jwtAuthenticationSettings.AllowedServers.First().Issuer,
                jwtAuthenticationSettings.AllowedClients.First(),
                claims: new[] { new Claim("user", "test") },
                secret: jwtAuthenticationSettings.AllowedServers.First().Secret,
                certificate: certificate);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => SendAsync(server, request));

            AssertOkAuthorizationRequiredResponse(response);
        }

        [Test]
        public async Task Encrypted_jwt_request_with_invalid_secret_is_rejected()
        {
            var certificate = new FileCertificateFetcher(encryptedJwtAuthenticationSettings.RelativeFileCertificate).Fetch();
            configure = ForAuthenticationTestWith(encryptedJwtAuthenticationSettings);

            var accessToken = CreateJwtAccessTokenFor(
                jwtAuthenticationSettings.AllowedServers.First().Issuer,
                jwtAuthenticationSettings.AllowedClients.First(),
                secret: "HayCkqRlBqeILBmvywxwzWsANzQ5YQQaJdjnDPR5CW0",
                certificate: certificate);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Encrypted_jwt_request_with_invalid_issuer_is_rejected()
        {
            var certificate = new FileCertificateFetcher(encryptedJwtAuthenticationSettings.RelativeFileCertificate).Fetch();
            configure = ForAuthenticationTestWith(encryptedJwtAuthenticationSettings);

            var accessToken = CreateJwtAccessTokenFor(
                "http://invalid-issuer.localhost",
                jwtAuthenticationSettings.AllowedClients.First(),
                secret: jwtAuthenticationSettings.AllowedServers.First().Secret,
                certificate: certificate);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Encrypted_jwt_request_with_invalid_client_id_is_rejected()
        {
            var certificate = new FileCertificateFetcher(encryptedJwtAuthenticationSettings.RelativeFileCertificate).Fetch();
            configure = ForAuthenticationTestWith(encryptedJwtAuthenticationSettings);

            var accessToken = CreateJwtAccessTokenFor(
                jwtAuthenticationSettings.AllowedServers.First().Issuer,
                "invalid-client-id",
                secret: jwtAuthenticationSettings.AllowedServers.First().Secret,
                certificate: certificate);

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Encrypted_jwt_request_with_expired_token_is_rejected()
        {
            var certificate = new FileCertificateFetcher(encryptedJwtAuthenticationSettings.RelativeFileCertificate).Fetch();
            configure = ForAuthenticationTestWith(encryptedJwtAuthenticationSettings);

            var accessToken = CreateJwtAccessTokenFor(
                jwtAuthenticationSettings.AllowedServers.First().Issuer,
                jwtAuthenticationSettings.AllowedClients.First(),
                expires: DateTime.UtcNow.AddSeconds(1),
                secret: jwtAuthenticationSettings.AllowedServers.First().Secret,
                certificate: certificate);

            await Task.Delay(TimeSpan.FromSeconds(1.1));

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
            request.Headers.Add("Authorization", "bearer " + accessToken);

            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
        }

        [Test]
        public async Task Compression_request_with_short_response_is_uncompressed()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(ShortResponseController.Response);
            configure = forCompressionTest;

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/short-response");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");

            var response = await WithTestServer(server => SendAsync(server, request));

            response.AssertOkWithContent(expectedResponse);
        }

        [Test]
        public async Task Compression_request_without_compression_support_is_uncompressed()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(LongResponseController.Response);
            configure = forCompressionTest;

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/long-response");
            var response = await WithTestServer(server => SendAsync(server, request));

            response.AssertOkWithContent(expectedResponse);
        }

        [Test]
        public async Task Compression_request_with_deflate_support_is_deflated()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(LongResponseController.Response);
            var expressedCompressedResponse = await Compress(expectedResponse, acceptedEncoding: "deflate");
            configure = forCompressionTest;

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/long-response");
            request.Headers.Add("Accept-Encoding", "deflate");

            var response = await WithTestServer(server => SendAsync(server, request));

            response.AssertOkWithContent(expressedCompressedResponse);
        }

        [Test]
        public async Task Compression_request_with_gzip_support_is_zipped()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(LongResponseController.Response);
            var expressedCompressedResponse = await Compress(expectedResponse, acceptedEncoding: "gzip");
            configure = forCompressionTest;

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/long-response");
            request.Headers.Add("Accept-Encoding", "gzip");

            var response = await WithTestServer(server => SendAsync(server, request));

            response.AssertOkWithContent(expressedCompressedResponse);
        }

        [Test]
        public async Task Compression_request_with_gzip_and_deflate_support_is_zipped()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(LongResponseController.Response);
            var expressedCompressedResponse = await Compress(expectedResponse, acceptedEncoding: "gzip");
            configure = forCompressionTest;

            var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/long-response");
            request.Headers.Add("Accept-Encoding", "gzip, deflate");

            var response = await WithTestServer(server => SendAsync(server, request));

            response.AssertOkWithContent(expressedCompressedResponse);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated through reflection and code analysis cannot detect this.")]
        private class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                configure(app);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class TestConfigSettings
        {
            public string Value { get; set; }
        }

        private class Response
        {
            public string Content { get; set; }
            public HttpStatusCode StatusCode { get; set; }

            public void AssertOkWithContent(string content)
            {
                Assert.That(StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(Content, Is.EqualTo(content));
            }
        }

        private class GoodBadResponses
        {
            public Response Good { get; set; }
            public HttpResponseMessage Bad { get; set; }
        }

        private static async Task<TResult> WithTestServer<TResult>(Func<TestServer, Task<TResult>> run)
        {
            using (var server = TestServer.Create<Startup>())
            {
                var capturedResult = await run(server);

                return capturedResult;
            }
        }

        private static async Task<Response> GetAsync(TestServer server, string uri)
        {
            var response = await server.HttpClient.GetAsync(uri);
            var content = await response.Content.ReadAsStringAsync();

            return new Response
            {
                Content = content,
                StatusCode = response.StatusCode
            };
        }

        private static async Task<Response> SendAsync(TestServer server, HttpRequestMessage request)
        {
            var response = await server.HttpClient.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            return new Response
            {
                Content = content,
                StatusCode = response.StatusCode
            };
        }

        private static async Task<GoodBadResponses> SendRequests(HttpRequestMessage goodRequest, HttpRequestMessage badRequest)
        {
            var responses = await WithTestServer(
                async server =>
                {
                    var goodResponse = await SendAsync(server, goodRequest);
                    var badResponse = await server.HttpClient.SendAsync(badRequest);

                    return new GoodBadResponses { Good = goodResponse, Bad = badResponse };
                });

            return responses;
        }

        private static void AssertOkAuthorizationRequiredResponse(Response response)
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.StartWith(@"[{""type"":""user"",""value"":""test""},{""type"":""iss"",""value"":""http://auth.localhost""},{""type"":""aud"",""value"":""my-identifier""}"));
        }

        private static HostingSettings CreateHostingSettingsWith(string origin, string method)
        {
            var settings = new HostingSettings
            {
                Cors = new Cors
                {
                    Origins = { origin },
                    Methods = { method },
                    Headers = { "*" }
                }
            };

            return settings;
        }

        private static string CreateJwtAccessTokenFor(JwtAuthenticationSettings settings)
        {
            var accessToken = CreateJwtAccessTokenFor(
                settings.AllowedServers.First().Issuer,
                settings.AllowedClients.First(),
                claims: new[] { new Claim("user", "test") },
                secret: settings.AllowedServers.First().Secret);

            return accessToken;
        }

        private static string CreateJwtAccessTokenFor(string issuer = null, string clientId = null, IEnumerable<Claim> claims = null, DateTime? issued = null, DateTime? expires = null, string secret = null, X509Certificate2 certificate = null)
        {
            var securityKey = TextEncodings.Base64Url.Decode(secret);

            var signingCredentials = new SigningCredentials(
                new InMemorySymmetricSecurityKey(securityKey),
                "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512",
                "http://www.w3.org/2001/04/xmlenc#sha512");

            var token = new JwtSecurityToken(issuer, clientId, claims, issued ?? DateTime.UtcNow, expires ?? DateTime.UtcNow.AddHours(1), signingCredentials);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.WriteToken(token);

            var publicKey = certificate?.PublicKey?.Key as RSACryptoServiceProvider;

            return (publicKey != null) ? JWT.Encode(jwt, publicKey, JweAlgorithm.RSA_OAEP_256, JweEncryption.A256GCM, JweCompression.DEF) : jwt;
        }

        private static async Task<string> Compress(string source, string acceptedEncoding)
        {
            var context = Substitute.For<IOwinContext>();

            var sourceStream = new MemoryStream(Encoding.UTF8.GetBytes(source));
            var compressedStream = new MemoryStream();

            await sourceStream.CompressTo(
                compressedStream,
                context,
                CompressionExtensions.CreateCompressionStream,
                acceptedEncoding,
                bufferSize: 8096);

            compressedStream.Seek(0, SeekOrigin.Begin);
            var result = await new StreamReader(compressedStream).ReadToEndAsync();

            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class OptionsRequestMessage : HttpRequestMessage
        {
            internal OptionsRequestMessage(string method, string url, string origin)
            {
                Method = HttpMethod.Options;
                RequestUri = new Uri(url);
                Headers.Add("Origin", origin);
                Headers.Add("Access-Control-Request-Method", method);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public interface IDependency
        {
            string Value { get; }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class Dependency : IDependency
        {
            public string Value { get; } = "Dependency";
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class CompactJsonController : ApiController
        {
            public IHttpActionResult Get()
            {
                return Ok(new { Null = null as string, Object = new { Name = "Mr. Me" } });
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class DirectDependencyController : ApiController
        {
            private readonly Dependency dependency;

            public DirectDependencyController(Dependency dependency)
            {
                this.dependency = dependency;
            }

            public IHttpActionResult Get()
            {
                return Ok(dependency.Value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class IndirectDependencyController : ApiController
        {
            private readonly IDependency dependency;

            public IndirectDependencyController(IDependency dependency)
            {
                this.dependency = dependency;
            }

            public IHttpActionResult Get()
            {
                return Ok(dependency.Value);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class SettingsDependencyController : ApiController
        {
            private readonly TestConfigSettings settings;

            public SettingsDependencyController(TestConfigSettings settings)
            {
                this.settings = settings;
            }

            public IHttpActionResult Get()
            {
                return Ok(settings);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        [Authorize]
        public class AuthorizationRequiredController : ApiController
        {
            public IHttpActionResult Get()
            {
                var identity = User.Identity as ClaimsIdentity;

                return Ok(identity.Claims.Select(c => new
                {
                    c.Type,
                    c.Value
                }));
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "authorization", Justification = "This is used by the tests, but usage cannot be detected by code analysis because it is called indirectly through web api.")]
            public IHttpActionResult Get(string authorization)
            {
                var identity = User.Identity as ClaimsIdentity;

                return Ok(identity.Claims.Select(c => new
                {
                    c.Type,
                    c.Value
                }));
            }

            public IHttpActionResult Post()
            {
                var identity = User.Identity as ClaimsIdentity;

                return Ok(identity.Claims.Select(c => new
                {
                    c.Type,
                    c.Value
                }));
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class ShortResponseController : ApiController
        {
            internal static string Response = "Short response";

            public IHttpActionResult Get()
            {
                return Ok(Response);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class LongResponseController : ApiController
        {
            internal static string Response = @"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Etiam aliquet rutrum enim a sollicitudin. Etiam ac aliquam sapien. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Nulla egestas, tellus non dictum vulputate, lorem ante viverra metus, ac finibus erat felis eu urna. Mauris ut lacinia enim, vel accumsan neque. Praesent laoreet nunc vitae sapien fringilla, ac ornare magna pharetra. Vivamus vitae odio vulputate, hendrerit sapien nec, lobortis sapien.
In ut ligula sit amet magna maximus ullamcorper sed ac velit. Pellentesque hendrerit sit amet magna in egestas. Quisque enim lectus, vehicula non mollis ac, pretium non justo. Nullam efficitur vulputate nunc. Pellentesque non elit nec erat vehicula euismod sed ut urna. Cras consequat, massa a commodo laoreet, elit odio euismod mauris, sit amet viverra lorem magna mattis enim. Morbi ante ex, blandit eu vulputate sed, consectetur at metus.
Sed purus lorem, consequat in orci vel, ullamcorper maximus tortor. Aenean commodo augue in est malesuada finibus. Cras maximus lorem nec dignissim gravida. Sed ornare, felis non malesuada dictum, felis diam vestibulum risus, interdum efficitur sem enim at risus. Maecenas at tincidunt neque, ut porta magna. Sed tincidunt hendrerit justo ac placerat. Sed ante lorem, dictum quis lorem nec, volutpat semper elit. Suspendisse volutpat rutrum enim, vel porttitor ante accumsan vel. Nulla accumsan elit orci, iaculis fringilla leo sollicitudin in. Pellentesque egestas id urna sed ornare. Curabitur sit amet congue risus. Maecenas ut quam sed ligula vestibulum viverra et eget dolor. Ut mi felis, sollicitudin ut elit vitae, pretium fringilla sapien.
Ut non posuere tortor. In suscipit dolor et auctor facilisis. Integer luctus orci nisl, eu malesuada metus porttitor a. Praesent blandit scelerisque leo, ut tempor elit commodo ac. Integer nec sapien quis mauris sollicitudin dictum quis eget odio. Aenean enim turpis, dignissim ut mauris ac, congue tincidunt orci. Cras sed feugiat mi, id ultrices libero. Donec et dolor vestibulum, fringilla ante vel, vestibulum arcu. Duis vulputate varius enim vel consequat. Quisque ut urna id lorem ornare volutpat. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Phasellus dignissim interdum nisi a euismod.
Curabitur congue ex eros, quis mollis tellus varius vel. Aliquam vestibulum tortor et enim porta porta. Quisque rutrum tellus sit amet justo porttitor pretium. Nunc elit enim, luctus vitae hendrerit ac, finibus ac lectus. Quisque ac viverra lorem. Duis cursus lobortis ultrices. Pellentesque maximus elit sed ligula vehicula congue.
Phasellus mauris lacus, pulvinar aliquet arcu ut, sagittis finibus neque. Proin pretium, libero vel lobortis dictum, augue dolor rutrum justo, sit amet dictum eros mi ac tellus. Suspendisse bibendum leo eget quam vehicula aliquet. In venenatis felis at arcu feugiat iaculis. Ut dapibus felis ornare, fringilla elit quis, suscipit lectus. Donec ipsum nulla, iaculis vitae ipsum nec, accumsan posuere urna. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Sed vitae nibh quis nunc rhoncus tempor. Morbi non mi interdum, lobortis nulla ut, posuere lacus. Duis vitae enim eget nulla ullamcorper ullamcorper ac vel sapien. Quisque eget arcu non dui finibus sollicitudin quis quis nulla. In id nunc et sapien egestas venenatis. Vivamus in euismod mauris. Sed iaculis efficitur elementum.
Maecenas est odio, efficitur eget enim et, blandit convallis lorem. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Nam ullamcorper rhoncus enim vel vehicula. Cras a sagittis dui. Cras sit amet bibendum ligula. Fusce libero neque, ultrices a velit et, accumsan commodo libero. Aliquam lobortis ut sapien quis scelerisque. Vestibulum nec dui a felis dapibus euismod sit amet non est. Nunc tincidunt sem eget lorem commodo, vel scelerisque mauris blandit. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nunc ut ullamcorper est, at tempus velit.
Maecenas nec urna at purus dignissim efficitur. Nullam sollicitudin dui id tellus varius, non faucibus sem pretium. Donec consequat tellus nec laoreet dignissim. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Nulla facilisi. Pellentesque convallis purus eget augue condimentum, a euismod ligula semper. Vivamus id faucibus ligula. In faucibus, felis eget auctor ornare, sem leo scelerisque velit, sit amet finibus ipsum ipsum eget tortor.";

            public IHttpActionResult Get()
            {
                return Ok(Response);
            }
        }
    }
}
