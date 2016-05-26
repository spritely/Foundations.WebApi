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
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Owin.Security.DataHandler.Encoder;
    using Microsoft.Owin.Testing;
    using NUnit.Framework;
    using Owin;
    
    /// <summary>
    /// Tests of the overall WebApi interface
    /// </summary>
    [TestFixture]
    public class WebApiTest
    {
        private static Action<IAppBuilder> configure;

        private readonly JwtBearerAuthenticationSettings jwtBearerAuthenticationSettings = new JwtBearerAuthenticationSettings
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

        [Test]
        public async Task Defaults_to_returning_compact_json()
        {
            configure = app =>
                app.UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var response = await server.HttpClient.GetAsync("/compact-json");
                var result = await response.Content.ReadAsStringAsync();

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result, Is.EqualTo(@"{""object"":{""name"":""Mr. Me""}}"));
            }
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

            using (var server = TestServer.Create<Startup>())
            {
                var response = await server.HttpClient.GetAsync("/compact-json");
                await response.Content.ReadAsStringAsync();
            }

            Assert.That(actualDependency, Is.Not.Null);
        }

        [Test]
        public async Task Controllers_can_reference_direct_types_defined_in_the_container()
        {
            configure = app =>
                app.UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var response = await server.HttpClient.GetAsync("/direct-dependency");
                var result = await response.Content.ReadAsStringAsync();

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result, Is.EqualTo(@"""Dependency"""));
            }
        }

        [Test]
        public async Task Controllers_can_reference_indirect_types_defined_in_the_container()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register<IDependency, Dependency>())
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var response = await server.HttpClient.GetAsync("/indirect-dependency");
                var result = await response.Content.ReadAsStringAsync();

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result, Is.EqualTo(@"""Dependency"""));
            }
        }

        [Test]
        public async Task Controllers_can_reference_settings()
        {
            configure = app =>
                app.UseSettingsContainerInitializer()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var response = await server.HttpClient.GetAsync("/settings-dependency");
                var result = await response.Content.ReadAsStringAsync();

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result, Is.EqualTo(@"{""value"":""Test""}"));
            }
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

            using (var server = TestServer.Create<Startup>())
            {
                var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://invalidorigin");
                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task Cors_accepts_request_with_matching_origin()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => CreateHostingSettingsWith("http://notlocalhost", "*")))
                    .UseCors()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://notlocalhost");
                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
        }

        [Test]
        public async Task Cors_rejects_request_without_matching_method()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => CreateHostingSettingsWith("http://notlocalhost", "POST")))
                    .UseCors()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://notlocalhost");
                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            }
        }

        [Test]
        public async Task Cors_accepts_request_with_matching_method()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => CreateHostingSettingsWith("http://notlocalhost", "POST")))
                    .UseCors()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new OptionsRequestMessage("POST", "http://localhost/compact-json", origin: "http://notlocalhost");
                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            }
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

            using (var server = TestServer.Create<Startup>())
            {
                var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://notlocalhost");
                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var header = response.Headers.Single(h => h.Key == "Access-Control-Allow-Credentials");
                Assert.That(header.Value.Single(), Is.EqualTo("true"));
            }
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

            using (var server = TestServer.Create<Startup>())
            {
                var request = new OptionsRequestMessage("GET", "http://localhost/compact-json", origin: "http://notlocalhost");
                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                var header = response.Headers.Single(h => h.Key == "Access-Control-Max-Age");
                Assert.That(header.Value.Single(), Is.EqualTo("311"));
            }
        }

        [Test]
        public async Task Jwt_request_with_valid_token_is_accepted()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => jwtBearerAuthenticationSettings))
                    .UseJwtBearerAuthentication()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
                var accessToken = CreateJwtAccessTokenFor(
                    jwtBearerAuthenticationSettings.AllowedServers.First().Issuer,
                    jwtBearerAuthenticationSettings.AllowedClients.First(),
                    claims: new [] { new Claim("user", "test") },
                    secret: jwtBearerAuthenticationSettings.AllowedServers.First().Secret);
                request.Headers.Add("Authorization", "bearer " + accessToken);

                var response = await server.HttpClient.SendAsync(request);
                var result = await response.Content.ReadAsStringAsync();

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result, Does.StartWith(@"[{""type"":""user"",""value"":""test""},{""type"":""iss"",""value"":""http://auth.localhost""},{""type"":""aud"",""value"":""my-identifier""}"));
            }
        }

        [Test]
        public async Task Jwt_request_with_invalid_secret_is_rejected()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => jwtBearerAuthenticationSettings))
                    .UseJwtBearerAuthentication()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
                var accessToken = CreateJwtAccessTokenFor(
                    jwtBearerAuthenticationSettings.AllowedServers.First().Issuer,
                    jwtBearerAuthenticationSettings.AllowedClients.First(),
                    secret: "HayCkqRlBqeILBmvywxwzWsANzQ5YQQaJdjnDPR5CW0");

                request.Headers.Add("Authorization", "bearer " + accessToken);

                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public async Task Jwt_request_with_invalid_issuer_is_rejected()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => jwtBearerAuthenticationSettings))
                    .UseJwtBearerAuthentication()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
                var accessToken = CreateJwtAccessTokenFor(
                    "http://invalid-issuer.localhost",
                    jwtBearerAuthenticationSettings.AllowedClients.First(),
                    secret: jwtBearerAuthenticationSettings.AllowedServers.First().Secret);

                request.Headers.Add("Authorization", "bearer " + accessToken);

                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public async Task Jwt_request_with_invalid_client_id_is_rejected()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => jwtBearerAuthenticationSettings))
                    .UseJwtBearerAuthentication()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
                var accessToken = CreateJwtAccessTokenFor(
                    jwtBearerAuthenticationSettings.AllowedServers.First().Issuer,
                    "invalid-client-id",
                    secret: jwtBearerAuthenticationSettings.AllowedServers.First().Secret);

                request.Headers.Add("Authorization", "bearer " + accessToken);

                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
        }

        [Test]
        public async Task Jwt_request_with_expired_token_is_rejected()
        {
            configure = app =>
                app.UseContainerInitializer(c => c.Register(() => jwtBearerAuthenticationSettings))
                    .UseJwtBearerAuthentication()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/authorization-required");
                var accessToken = CreateJwtAccessTokenFor(
                    jwtBearerAuthenticationSettings.AllowedServers.First().Issuer,
                    jwtBearerAuthenticationSettings.AllowedClients.First(),
                    expires: DateTime.UtcNow.AddSeconds(1),
                    secret: jwtBearerAuthenticationSettings.AllowedServers.First().Secret);

                await Task.Delay(TimeSpan.FromSeconds(1.1));

                request.Headers.Add("Authorization", "bearer " + accessToken);

                var response = await server.HttpClient.SendAsync(request);

                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            }
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

        private static string CreateJwtAccessTokenFor(string issuer = null, string clientId = null, IEnumerable<Claim> claims = null, DateTime? issued = null, DateTime? expires = null, string secret = null)
        {
            var securityKey = TextEncodings.Base64Url.Decode(secret);

            var signingCredentials = new SigningCredentials(
                new InMemorySymmetricSecurityKey(securityKey),
                "http://www.w3.org/2001/04/xmldsig-more#hmac-sha512",
                "http://www.w3.org/2001/04/xmlenc#sha512");

            var token = new JwtSecurityToken(issuer, clientId, claims, issued ?? DateTime.UtcNow, expires ?? DateTime.UtcNow.AddHours(1), signingCredentials);
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.WriteToken(token);

            return jwt;
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
        }
    }
}
