// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MultipartFormApiTest.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using Microsoft.Owin.Testing;
    using NUnit.Framework;
    using Owin;

    /// <summary>
    /// Multi-part form api tests.
    /// </summary>
    [TestFixture]
    public class MultipartFormApiTest
    {
        private static Action<IAppBuilder> configure;

        [Test]
        public async Task When_request_is_not_multipart_then_Unsupported_Media_Type_is_returned()
        {
            CreateWriteStream createWriteStream = (fileName, formData, headers) => new MemoryStream();

            configure = app =>
                app.UseContainerInitializer(c => c.RegisterSingleton(createWriteStream))
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/simple-multi-part-form")
            {
                Content = new StringContent("Not a multi-part-form")
            };
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
        }

        [Test]
        public async Task Create_write_stream_is_called_once_for_each_file_in_request_and_that_file_is_written_into_stream()
        {
            var calledTimes = 0;
            var streams = new Stream[] { new MemoryStream(), new MemoryStream() };
            CreateWriteStream createWriteStream =
                (fileName, formData, headers) => streams[calledTimes++];

            configure = app =>
                app.UseContainerInitializer(c => c.RegisterSingleton(createWriteStream))
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var file1Contents = Encoding.UTF8.GetBytes("Some random file");
            var file2Contents = Encoding.UTF8.GetBytes("More random content");

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/simple-multi-part-form")
            {
                Content = new MultipartFormDataContent
                {
                    { new StreamContent(new MemoryStream(file1Contents)), "files", "firstFile.txt" },
                    { new StreamContent(new MemoryStream(file2Contents)), "files", "secondFile.txt" }
                }
            };
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(calledTimes, Is.EqualTo(2));
            var stream1Contents = new StreamReader(streams[0]).ReadToEnd();
            var stream2Contents = new StreamReader(streams[1]).ReadToEnd();
            Assert.That(stream1Contents, Is.EqualTo("Some random file"));
            Assert.That(stream2Contents, Is.EqualTo("More random content"));
        }

        [Test]
        public async Task Create_write_stream_receives_correct_file_name_for_each_file()
        {
            var fileNames = new List<string>();
            CreateWriteStream createWriteStream =
                (fileName, formData, headers) =>
                {
                    fileNames.Add(fileName);
                    return new MemoryStream();
                };

            configure = app =>
                app.UseContainerInitializer(c => c.RegisterSingleton(createWriteStream))
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var file1Contents = Encoding.UTF8.GetBytes("Some random file");
            var file2Contents = Encoding.UTF8.GetBytes("More random content");

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/simple-multi-part-form")
            {
                Content = new MultipartFormDataContent
                {
                    { new StreamContent(new MemoryStream(file1Contents)), "files", "firstFile.txt" },
                    { new StreamContent(new MemoryStream(file2Contents)), "files", "secondFile.txt" }
                }
            };
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(fileNames.Count, Is.EqualTo(2));
            Assert.That(fileNames.First(), Is.EqualTo("firstFile.txt"));
            Assert.That(fileNames.Skip(1).First(), Is.EqualTo("secondFile.txt"));
        }

        [Test]
        public async Task FormData_is_available_for_create_write_stream_as_far_as_stream_has_been_currently_processed()
        {
            var formDataResults = new List<List<KeyValuePair<string, string>>>();
            CreateWriteStream createWriteStream =
                (fileName, formData, headers) =>
                {
                    var results = new List<KeyValuePair<string, string>>();
                    foreach (var keyValue in formData)
                    {
                        var value = Task.Run(() => keyValue.Value()).Result;
                        results.Add(new KeyValuePair<string, string>(keyValue.Key, value));
                    }
                    formDataResults.Add(results);

                    return new MemoryStream();
                };

            configure = app =>
                app.UseContainerInitializer(c => c.RegisterSingleton(createWriteStream))
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var file1Contents = Encoding.UTF8.GetBytes("Some random file");
            var file2Contents = Encoding.UTF8.GetBytes("More random content");

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/simple-multi-part-form")
            {
                Content = new MultipartFormDataContent
                {
                    { new StringContent("Field 1 Value"), "field1" },
                    { new StreamContent(new MemoryStream(file1Contents)), "files", "firstFile.txt" },
                    { new StringContent("Field 2 Value"), "field2" },
                    { new StreamContent(new MemoryStream(file2Contents)), "files", "secondFile.txt" },
                    { new StringContent("Field 3 Value"), "field3" }
                }
            };
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(formDataResults.Count, Is.EqualTo(2));
            Assert.That(formDataResults.First().Count, Is.EqualTo(1));
            Assert.That(formDataResults.First().First().Key, Is.EqualTo("field1"));
            Assert.That(formDataResults.First().First().Value, Is.EqualTo("Field 1 Value"));
            Assert.That(formDataResults.Skip(1).First().Count, Is.EqualTo(2));
            Assert.That(formDataResults.Skip(1).First().First().Key, Is.EqualTo("field1"));
            Assert.That(formDataResults.Skip(1).First().First().Value, Is.EqualTo("Field 1 Value"));
            Assert.That(formDataResults.Skip(1).First().Skip(1).First().Key, Is.EqualTo("field2"));
            Assert.That(formDataResults.Skip(1).First().Skip(1).First().Value, Is.EqualTo("Field 2 Value"));
        }

        [Test]
        public async Task All_FormData_is_available_after_all_streams_have_been_processed()
        {
            CreateWriteStream createWriteStream =
                (fileName, formData, headers) => new MemoryStream();

            configure = app =>
                app.UseContainerInitializer(c => c.RegisterSingleton(createWriteStream))
                    .UseWebApiWithHttpConfigurationInitializers(DefaultWebApiConfig.InitializeHttpConfiguration);

            var file1Contents = Encoding.UTF8.GetBytes("Some random file");
            var file2Contents = Encoding.UTF8.GetBytes("More random content");

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost/simple-multi-part-form")
            {
                Content = new MultipartFormDataContent
                {
                    { new StringContent("Field 1 Value"), "field1" },
                    { new StreamContent(new MemoryStream(file1Contents)), "files", "firstFile.txt" },
                    { new StringContent("Field 2 Value"), "field2" },
                    { new StreamContent(new MemoryStream(file2Contents)), "files", "secondFile.txt" },
                    { new StringContent("Field 3 Value"), "field3" }
                }
            };
            var response = await WithTestServer(server => server.HttpClient.SendAsync(request));
            
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            var content = await response.Content.ReadAsStringAsync();
            Assert.That(content, Is.EqualTo(@"[{""key"":""field1"",""value"":""Field 1 Value""},{""key"":""field2"",""value"":""Field 2 Value""},{""key"":""field3"",""value"":""Field 3 Value""}]"));
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Justification = "Class is instantiated through reflection and code analysis cannot detect this.")]
        private class Startup
        {
            public void Configuration(IAppBuilder app)
            {
                configure(app);
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Justification = "This must be public for Web Api to use it and keeping it with its associated tests.")]
        public class SimpleMultipartFormController : ApiController
        {
            private const int BufferSize = 0x1000;
            private readonly CreateWriteStream createWriteStream;

            public SimpleMultipartFormController(CreateWriteStream createWriteStream)
            {
                if (createWriteStream == null)
                {
                    throw new ArgumentNullException(nameof(createWriteStream));
                }
                
                this.createWriteStream = createWriteStream;
            }

            public async Task<IHttpActionResult> Post()
            {
                if (!Request.Content.IsMimeMultipartContent())
                {
                    return StatusCode(HttpStatusCode.UnsupportedMediaType);
                }

                var streamProvider = new MultipartFormDataCustomStreamProvider(createWriteStream, BufferSize);

                var results = await Request.Content.ReadAsMultipartAsync(streamProvider);

                var formData = new List<KeyValuePair<string, string>>();
                foreach (var keyValue in results.FormData)
                {
                    var value = Task.Run(() => keyValue.Value()).Result;
                    formData.Add(new KeyValuePair<string, string>(keyValue.Key, value));
                }

                return Ok(formData);
            }
        }
    }
}
