// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StreamFileResponseExtensions.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web.Http.Results;

    public static class StreamFileResponseExtensions
    {
        /// <summary>
        /// Converts the value into a UTF8 memory stream. Caller is expected to dispose of result.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The memory stream.</returns>
        /// <exception cref="System.ArgumentNullException">If value is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "User is responsible for disposing instance.")]
        public static MemoryStream ToUtf8MemoryStream(this string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var byteArray = Encoding.UTF8.GetBytes(value);
            var stream = new MemoryStream(byteArray);
            
            return stream;
        }

        /// <summary>
        /// Converts the byte array content into an HttpContent instance. Caller is expected to dispose of result.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns>An HttpContent result.</returns>
        /// <exception cref="System.ArgumentNullException">If bytes is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "User is responsible for disposing instance.")]
        public static HttpContent ToHttpContent(this byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var content = new ByteArrayContent(bytes, 0, bytes.Length);

            return content;
        }

        /// <summary>
        /// Converts the stream content into an HttpContent instance. Caller is expected to dispose of result.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>An HttpContent result.</returns>
        /// <exception cref="System.ArgumentNullException">If stream is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "User is responsible for disposing instance.")]
        public static HttpContent ToHttpContent(this Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var content = new StreamContent(stream);

            return content;
        }

        /// <summary>
        /// Converts the specified file's content into an HttpContent instance. Caller is expected to dispose of result.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns>An HttpContent result.</returns>
        /// <exception cref="System.ArgumentNullException">If file is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "User is responsible for disposing instance.")]
        public static HttpContent ToHttpContent(this FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var fileStream = File.OpenRead(file.FullName);
            var content = new StreamContent(fileStream);

            return content;
        }

        /// <summary>
        /// Converts the file content into a ResponseMessageResult instance including providing appropriate content headers
        /// specifying the content type and file name. Caller is expected to dispose of result.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="mediaType">The media type for the file. If null, then the media type will be determined from the file extension.</param>
        /// <returns>A ResponseMessageResult ready for Web Api to stream the file content back to a client.</returns>
        /// <exception cref="System.ArgumentNullException">If file is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Web Api is responsible for disposing instance.")]
        public static ResponseMessageResult ToOkWithFileResponse(this FileInfo file, string mediaType = null)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var contentType = !string.IsNullOrWhiteSpace(mediaType) ? mediaType : MimeTypeMap.GetMimeType(file.Extension);

            var fileStream = File.OpenRead(file.FullName);
            var content = new StreamContent(fileStream);

            return ToOkWithFileResponse(content, file.Name, contentType);
        }

        /// <summary>
        /// Converts the content into a ResponseMessageResult instance including providing appropriate content headers
        /// specifying the content type and file name. Caller is expected to dispose of result.
        /// </summary>
        /// <param name="content">The content to stream.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="mediaType">The media type for the file.</param>
        /// <returns>A ResponseMessageResult ready for Web Api to stream the file content back to a client.</returns>
        /// <exception cref="System.ArgumentNullException">If any of the arguments are null or contain only whitespace.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Web Api is responsible for disposing instance.")]
        public static ResponseMessageResult ToOkWithFileResponse(this HttpContent content, string fileName, string mediaType)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentNullException(nameof(fileName));
            }

            if (string.IsNullOrWhiteSpace(mediaType))
            {
                throw new ArgumentNullException(nameof(mediaType));
            }

            content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = fileName
            };

            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = content
            };

            return new ResponseMessageResult(response);
        }

        /// <summary>
        /// Converts the value into a valid file name by replacing invalid characters with underscores.
        /// </summary>
        /// <param name="value">The value to convert into a valid file name.</param>
        /// <returns>A valid file name.</returns>
        /// <exception cref="System.ArgumentNullException">If value is null.</exception>
        public static string ToValidFileName(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentNullException(nameof(value));
            }

            var invalidFileChars = string.Format(
                CultureInfo.InvariantCulture,
                "[{0}]+",
                Regex.Escape(new string(Path.GetInvalidPathChars()) + new string(Path.GetInvalidFileNameChars())));

            var validFileName = Regex.Replace(value, invalidFileChars, "_");

            return validFileName;
        }
    }
}
