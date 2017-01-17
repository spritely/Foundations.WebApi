// --------------------------------------------------------------------------------------------------------------------
// <copyright file="JwtApiTest.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
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
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Jose;
    using Microsoft.Owin.Security.DataHandler.Encoder;
    using Microsoft.Owin.Testing;
    using NSubstitute;
    using NUnit.Framework;
    using Owin;
    using Spritely.Recipes;

    /// <summary>
    /// Jwt api tests.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Testing the main surface area for assembly touches on many scenarios.")]
    [TestFixture]
    public class JwtApiTest
    {
        private static Action<IAppBuilder> configure;

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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated through reflection and code analysis cannot detect this.")]
        private class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                configure(app);
            }
        }

        private class Response
        {
            public string Content { get; set; }
            public HttpStatusCode StatusCode { get; set; }
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
    }
}
