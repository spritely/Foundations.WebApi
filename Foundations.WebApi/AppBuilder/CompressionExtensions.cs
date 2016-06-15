// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CompressionExtensions.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Owin;
    using Owin;

    /// <summary>
    /// Compress extensions for IAppuilder
    /// </summary>
    public static class CompressionExtensions
    {
        internal static IReadOnlyDictionary<string, Func<Stream, Stream>> CreateCompressionStream =
            new Dictionary<string, Func<Stream, Stream>>(StringComparer.OrdinalIgnoreCase)
            {
                { "gzip", destinataionStream => new GZipStream(destinataionStream, CompressionMode.Compress, leaveOpen: true) },
                { "deflate", destinataionStream => new DeflateStream(destinataionStream, CompressionMode.Compress, leaveOpen: true) }
            };

        /// <summary>
        /// Uses the g-zip deflate compression.
        /// </summary>
        /// <param name="app">The application.</param>
        /// <param name="compressResponsesOver">The size over which responses should be compressed.</param>
        /// <param name="bufferSize">Size of the buffer.</param>
        /// <returns>
        /// The modified application.
        /// </returns>
        public static IAppBuilder UseGzipDeflateCompression(this IAppBuilder app, int compressResponsesOver = 4096, int bufferSize = 8196)
        {
            if (app == null)
            {
                throw new ArgumentNullException(nameof(app));
            }

            app.Use(async (context, next) =>
            {
                var acceptEncoding = (context.Request.Headers["Accept-Encoding"] ?? string.Empty).ToLowerInvariant();
                var acceptedEncodings = acceptEncoding.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim());
                var acceptedEncoding = acceptedEncodings.FirstOrDefault(e => CreateCompressionStream.ContainsKey(e));

                if (acceptedEncoding == null)
                {
                    await next.Invoke();
                }
                else
                {
                    // Copy original value and restore in finally
                    var body = context.Response.Body;
                    try
                    {
                        // Underlying source will be written to source
                        using (var responseStream = new MemoryStream())
                        {
                            context.Response.Body = responseStream;
                            await next.Invoke();

                            if (responseStream.Length <= compressResponsesOver)
                            {
                                await responseStream.CopyTo(body, context, bufferSize);
                            }
                            else
                            {
                                await responseStream.CompressTo(body, context, CreateCompressionStream, acceptedEncoding, bufferSize);
                            }
                        }
                    }
                    finally
                    {
                        context.Response.Body = body;
                    }
                }
            });

            return app;
        }

        internal static async Task CompressTo(this Stream source, Stream destination, IOwinContext context, IReadOnlyDictionary<string, Func<Stream, Stream>> createCompressionStream, string acceptedEncoding, int bufferSize)
        {
            using (var compressedResponse = new MemoryStream())
            {
                // Need to open and close compression stream for it to write all info to output stream
                // See: https://blogs.msdn.microsoft.com/bclteam/2006/05/10/using-a-memorystream-with-gzipstream-lakshan-fernando/
                using (var compressionStream = createCompressionStream[acceptedEncoding](compressedResponse))
                {
                    source.Seek(0, SeekOrigin.Begin);
                    await source.CopyToAsync(compressionStream, bufferSize, context.Request.CallCancelled);
                }

                context.Response.Headers.Set("Content-Encoding", acceptedEncoding);
                await compressedResponse.CopyTo(destination, context, bufferSize);
            }
        }

        private static async Task CopyTo(this Stream source, Stream destination, IOwinContext context, int bufferSize)
        {
            source.Seek(0, SeekOrigin.Begin);
            context.Response.ContentLength = source.Length;
            await source.CopyToAsync(destination, bufferSize, context.Request.CallCancelled);
        }
    }
}
