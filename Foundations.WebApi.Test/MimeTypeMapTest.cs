// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MimeTypeMapTest.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    public class MimeTypeMapTest
    {
        [Test]
        public void GetMimeType_throws_on_null_argument()
        {
            Assert.Throws<ArgumentNullException>(() => MimeTypeMap.GetMimeType(null));
        }

        [Test]
        public void GetMimeType_returns_expected_matching_mime_type()
        {
            var zipMimeType = MimeTypeMap.GetMimeType(".zip");
            var pngMimeType = MimeTypeMap.GetMimeType("png");

            Assert.That(zipMimeType, Is.EqualTo("application/zip"));
            Assert.That(pngMimeType, Is.EqualTo("image/png"));
        }

        [Test]
        public void GetMimeType_returns_default_mime_type_when_none_found()
        {
            var mimeType = MimeTypeMap.GetMimeType(".a-weird-extension-that-should-not-match");

            Assert.That(mimeType, Is.EqualTo("application/octet-stream"));
        }

        [Test]
        public void GetExtension_throws_on_null_argument()
        {
            Assert.Throws<ArgumentNullException>(() => MimeTypeMap.GetExtension(null));
        }

        [Test]
        public void GetExtension_returns_expected_match()
        {
            var jpgExtension = MimeTypeMap.GetExtension("image/jpeg");
            var txtExtension = MimeTypeMap.GetExtension("text/plain");

            Assert.That(jpgExtension, Is.EqualTo(".jpg"));
            Assert.That(txtExtension, Is.EqualTo(".txt"));
        }

        [Test]
        public void GetExtension_throws_on_invalid_extension()
        {
            Assert.Throws<ArgumentException>(() => MimeTypeMap.GetExtension(string.Empty));
            Assert.Throws<ArgumentException>(() => MimeTypeMap.GetExtension(".txt"));
            Assert.Throws<ArgumentException>(() => MimeTypeMap.GetExtension("some-non-sense/mime-type"));
        }
    }
}
