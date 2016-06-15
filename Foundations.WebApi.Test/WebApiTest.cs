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
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
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
                Assert.That(result, Is.EqualTo(@"{""null"":null,""object"":{""name"":""Mr. Me""}}"));
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

        [Test]
        public async Task Compression_request_with_short_response_is_uncompressed()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(ShortResponseController.Response);

            configure = app =>
                app.UseGzipDeflateCompression()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/short-response");
                request.Headers.Add("Accept-Encoding", "gzip, deflate");

                var response = await server.HttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                Assert.That(content, Is.EqualTo(expectedResponse));
            }
        }

        [Test]
        public async Task Compression_request_without_compression_support_is_uncompressed()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(LongResponseController.Response);

            configure = app =>
                app.UseGzipDeflateCompression()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/long-response");

                var response = await server.HttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                Assert.That(content, Is.EqualTo(expectedResponse));
            }
        }

        [Test]
        public async Task Compression_request_with_deflate_support_is_deflated()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(LongResponseController.Response);
            var expressedCompressedResponse = await Compress(expectedResponse, acceptedEncoding: "deflate");

            configure = app =>
                app.UseGzipDeflateCompression()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/long-response");
                request.Headers.Add("Accept-Encoding", "deflate");

                var response = await server.HttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                Assert.That(content, Is.EqualTo(expressedCompressedResponse));
            }
        }

        [Test]
        public async Task Compression_request_with_gzip_support_is_zipped()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(LongResponseController.Response);
            var expressedCompressedResponse = await Compress(expectedResponse, acceptedEncoding: "gzip");

            configure = app =>
                app.UseGzipDeflateCompression()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/long-response");
                request.Headers.Add("Accept-Encoding", "gzip");

                var response = await server.HttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                Assert.That(content, Is.EqualTo(expressedCompressedResponse));
            }
        }

        [Test]
        public async Task Compression_request_with_gzip_and_deflate_support_is_zipped()
        {
            var expectedResponse = CompactJsonSerializer.SerializeObject(LongResponseController.Response);
            var expressedCompressedResponse = await Compress(expectedResponse, acceptedEncoding: "gzip");

            configure = app =>
                app.UseGzipDeflateCompression()
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            using (var server = TestServer.Create<Startup>())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost/long-response");
                request.Headers.Add("Accept-Encoding", "gzip, deflate");

                var response = await server.HttpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                Assert.That(content, Is.EqualTo(expressedCompressedResponse));
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
