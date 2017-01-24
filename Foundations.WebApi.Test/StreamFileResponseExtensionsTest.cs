// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StreamFileResponseExtensionsTest.cs">
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
    using NUnit.Framework;

    [TestFixture]
    public class StreamFileResponseExtensionsTest
    {
        [Test]
        public void ToValidFileName_throws_on_invalid_argument()
        {
            Assert.Throws<ArgumentNullException>(() => (null as string).ToValidFileName());
            Assert.Throws<ArgumentNullException>(() => string.Empty.ToValidFileName());
        }

        [Test]
        public void ToValidFileName_returns_expected_value()
        {
            Assert.That("test/file\\name".ToValidFileName(), Is.EqualTo("test_file_name"));
            Assert.That("file*name-with:invalid*characters?in\"it|<>.ext".ToValidFileName(), Is.EqualTo("file_name-with_invalid_characters_in_it_.ext"));
        }

        [Test]
        public void ToUtf8MemoryStream_throws_on_invalid_argument()
        {
            Assert.Throws<ArgumentNullException>(() => (null as string).ToUtf8MemoryStream());
        }

        [Test]
        public void ToUtf8MemoryStream_returns_expected_value()
        {
            var expectedValue = "Some test data";
            using (var memoryStream = expectedValue.ToUtf8MemoryStream())
            using (var reader = new StreamReader(memoryStream))
            {
                var result = reader.ReadToEnd();
                Assert.That(result, Is.EqualTo(expectedValue));
            }
        }

        [Test]
        public void ToHttpContent_with_bytes_throws_on_invalid_argument()
        {
            Assert.Throws<ArgumentNullException>(() => (null as byte[]).ToHttpContent());
        }

        [Test]
        public async Task ToHttpContent_with_bytes_returns_expected_value()
        {
            var expectedValue = "A bunch of text";
            var bytes = Encoding.UTF8.GetBytes(expectedValue);

            using (var content = bytes.ToHttpContent())
            {
                var result = await content.ReadAsStringAsync();
                Assert.That(result, Is.EqualTo(expectedValue));
            }
        }

        [Test]
        public void ToHttpContent_with_Stream_throws_on_invalid_argument()
        {
            Assert.Throws<ArgumentNullException>(() => (null as Stream).ToHttpContent());
        }

        [Test]
        public async Task ToHttpContent_with_Stream_returns_expected_value()
        {
            var expectedValue = "Let's write some random stuff in a string.";

            using (var content = expectedValue.ToUtf8MemoryStream().ToHttpContent())
            {
                var result = await content.ReadAsStringAsync();
                Assert.That(result, Is.EqualTo(expectedValue));
            }
        }

        [Test]
        public void ToHttpContent_with_FileInfo_throws_on_invalid_argument()
        {
            Assert.Throws<ArgumentNullException>(() => (null as FileInfo).ToHttpContent());
        }

        [Test]
        public async Task ToHttpContent_with_FileInfo_returns_expected_value()
        {
            var expectedValue = @"{
    ""value"": ""Test""
}
";
            var fileName = Path.Combine(TestContext.CurrentContext.TestDirectory, ".config/Local/TestConfigSettings.json");

            var file = new FileInfo(fileName);
            using (var content = file.ToHttpContent())
            {
                var result = await content.ReadAsStringAsync();
                Assert.That(result, Is.EqualTo(expectedValue));
            }
        }

        [Test]
        public void ToOkWithFileResponse_with_FileInfo_throws_on_invalid_argument()
        {
            Assert.Throws<ArgumentNullException>(() => (null as FileInfo).ToOkWithFileResponse());
        }

        [Test]
        public async Task ToOkWithFileResponse_with_FileInfo_and_specified_media_type_returns_expected_response()
        {
            var expectedValue = @"{
    ""value"": ""Test""
}
";
            var fileName = Path.Combine(TestContext.CurrentContext.TestDirectory, ".config/Local/TestConfigSettings.json");

            var file = new FileInfo(fileName);
            var response = file.ToOkWithFileResponse("text/plain");
            var result = await response.Response.Content.ReadAsStringAsync();
            Assert.That(result, Is.EqualTo(expectedValue));
            Assert.That(response.Response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Response.Content.Headers.ContentDisposition.DispositionType, Is.EqualTo("attachment"));
            Assert.That(response.Response.Content.Headers.ContentDisposition.FileName, Is.EqualTo(file.Name));
            Assert.That(response.Response.Content.Headers.ContentType.MediaType, Is.EqualTo("text/plain"));
        }

        [Test]
        public async Task ToOkWithFileResponse_with_FileInfo_and_unspecified_media_type_returns_expected_response()
        {
            var expectedValue = @"{
    ""value"": ""Test""
}
";
            var fileName = Path.Combine(TestContext.CurrentContext.TestDirectory, ".config/Local/TestConfigSettings.json");

            var file = new FileInfo(fileName);
            var response = file.ToOkWithFileResponse();
            var result = await response.Response.Content.ReadAsStringAsync();
            Assert.That(result, Is.EqualTo(expectedValue));
            Assert.That(response.Response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Response.Content.Headers.ContentDisposition.DispositionType, Is.EqualTo("attachment"));
            Assert.That(response.Response.Content.Headers.ContentDisposition.FileName, Is.EqualTo(file.Name));
            Assert.That(response.Response.Content.Headers.ContentType.MediaType, Is.EqualTo("application/json"));
        }

        [Test]
        public void ToOkWithFileResponse_with_HttpContent_throws_on_invalid_argument()
        {
            Assert.Throws<ArgumentNullException>(() => (null as HttpContent).ToOkWithFileResponse("valid", "valid"));
            Assert.Throws<ArgumentNullException>(
                () =>
                {
                    using (var content = "Test".ToUtf8MemoryStream().ToHttpContent())
                    {
                        content.ToOkWithFileResponse(null, "valid");
                    }
                });
            Assert.Throws<ArgumentNullException>(
            () =>
            {
                using (var content = "Test".ToUtf8MemoryStream().ToHttpContent())
                {
                    content.ToOkWithFileResponse("  ", "valid");
                }
            });

            Assert.Throws<ArgumentNullException>(
            () =>
            {
                using (var content = "Test".ToUtf8MemoryStream().ToHttpContent())
                {
                    content.ToOkWithFileResponse("valid.txt", null);
                }
            });

            Assert.Throws<ArgumentNullException>(
            () =>
            {
                using (var content = "Test".ToUtf8MemoryStream().ToHttpContent())
                {
                    content.ToOkWithFileResponse("valid.txt", "   ");
                }
            });
        }

        [Test]
        public async Task ToOkWithFileResponse_with_HttpContent_returns_expected_response()
        {
            var expectedValue = @"Jack be nimble,
Jack be quick";

            var response = expectedValue.ToUtf8MemoryStream().ToHttpContent().ToOkWithFileResponse("JackFile.txt", "text/plain");
            var result = await response.Response.Content.ReadAsStringAsync();
            Assert.That(result, Is.EqualTo(expectedValue));
            Assert.That(response.Response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Response.Content.Headers.ContentDisposition.DispositionType, Is.EqualTo("attachment"));
            Assert.That(response.Response.Content.Headers.ContentDisposition.FileName, Is.EqualTo("JackFile.txt"));
            Assert.That(response.Response.Content.Headers.ContentType.MediaType, Is.EqualTo("text/plain"));
        }
    }
}
