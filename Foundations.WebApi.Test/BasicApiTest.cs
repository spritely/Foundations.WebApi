// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BasicApiTest.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Owin.Testing;
    using NUnit.Framework;
    using Owin;

    /// <summary>
    /// Basic api tests.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "Testing the main surface area for assembly touches on many scenarios.")]
    [TestFixture]
    public class BasicApiTest
    {
        private static Action<IAppBuilder> configure;

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
    }
}
