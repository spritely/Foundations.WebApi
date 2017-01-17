// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompressionApiTest.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Owin;
    using Microsoft.Owin.Testing;
    using NSubstitute;
    using NUnit.Framework;
    using Owin;
    using Spritely.Recipes;

    /// <summary>
    /// Compression api tests.
    /// </summary>
    [TestFixture]
    public class CompressionApiTest
    {
        private static Action<IAppBuilder> configure;

        private static readonly Action<IAppBuilder> forCompressionTest = app =>
            app.UseGzipDeflateCompression()
                .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

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
