// --------------------------------------------------------------------------------------------------------------------
// <copyright file="MultipartFormDataCustomStreamProvider.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Spritely.Recipes;

    /// <summary>
    /// Delegate responsible for creating a writable stream into which the incoming multi-part form data stream will be written.
    /// All the information that is currently available in the request will be handed to the delegate, but information is only
    /// available to the point it has been read from the incoming data stream so the client should order elements appropriately
    /// if formData is needed to create the stream (i.e. client should send its fields first and files after or will instead need
    /// to use headers to transmit additional data).
    /// </summary>
    /// <param name="fileName">Name of the file.</param>
    /// <param name="formData">The form data.</param>
    /// <param name="headers">The headers.</param>
    /// <returns>The newly created writable stream.</returns>
    [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix", Justification = "Method is responsible for creating a Stream and name clearly indicates this.")]
    public delegate Stream CreateWriteStream(string fileName, IDictionary<string, Func<Task<string>>> formData, HttpContentHeaders headers);

    // Inspired by source for MultipartFileStreamProvider.
    /// <summary>
    /// A <see cref="MultipartStreamProvider"/> suited for writing each MIME body parts of the MIME multipart
    /// message to custom streams.
    /// </summary>
    public class MultipartFormDataCustomStreamProvider : MultipartStreamProvider, IDisposable
    {
        private const int DefaultBufferSize = 0x1000;

        // Overrides MultipartStreamProvider.Contents so additions can be observed. See corresponding property below.
        private readonly ObservableCollection<HttpContent> contents = new ObservableCollection<HttpContent>();

        private readonly CreateWriteStream createWriteStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultipartFormDataCustomStreamProvider" /> class.
        /// </summary>
        /// <param name="createWriteStream">The function responsible for creating a writable stream while reading the incoming multi-part form data stream.</param>
        /// <param name="bufferSize">The number of bytes buffered for writes to a file.</param>
        /// <exception cref="ArgumentNullException">If createWriteStream is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">bufferSize must be greater than or equal to 1.</exception>
        public MultipartFormDataCustomStreamProvider(CreateWriteStream createWriteStream, int bufferSize = DefaultBufferSize)
        {
            if (createWriteStream == null)
            {
                throw new ArgumentNullException(nameof(createWriteStream));
            }

            if (bufferSize < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), bufferSize, string.Format(CultureInfo.InvariantCulture, Messages.Exception_ValueMustBeGreaterThanOrEqualTo, 1));
            }

            this.createWriteStream = createWriteStream;
            BufferSize = bufferSize;
            contents.CollectionChanged += ContentCollectionChanged;

            // Base class doesn't provide a way to set this, but it is necessary to be able to
            // detect additions to the collection as they occur
            var contentsField = typeof(MultipartStreamProvider).GetField("_contents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            contentsField?.SetValue(this, contents);
        }

        /// <summary>
        /// Gets the number of bytes buffered for writes to a file.
        /// </summary>
        public int BufferSize { get; }

        /// <summary>
        /// Gets the form data passed as part of the multipart form data.
        /// </summary>
        public IDictionary<string, Func<Task<string>>> FormData { get; } = new Dictionary<string, Func<Task<string>>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                contents.CollectionChanged -= ContentCollectionChanged;
            }
        }

        /// <summary>
        /// Gets the stream where to write the body part to. This method is called when a MIME multipart body part has been parsed.
        /// </summary>
        /// <param name="parent">The content of the HTTP.</param>
        /// <param name="headers">The header fields describing the body part.</param>
        /// <returns>
        /// The <see cref="T:System.IO.Stream" /> instance where the message body part is written to.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// If parent or headers arguments are null.
        /// </exception>
        [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Stream is closed by caller (MultipartWriteDelegatingStream is just a wrapper that calls into the inner stream.)")]
        public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            if (headers == null)
            {
                throw new ArgumentNullException(nameof(headers));
            }

            // For form data, Content-Disposition header is required
            var contentDisposition = headers.ContentDisposition;
            if (contentDisposition == null)
            {
                throw new InvalidOperationException(Messages.Exception_ContentDispositionHeaderRequired);
            }

            // The file name's existence indicates presence of a file
            if (!string.IsNullOrEmpty(contentDisposition.FileName))
            {
                var fileName = UnquoteToken(contentDisposition.FileName);
                return createWriteStream(fileName, FormData, headers);
            }

            return new MemoryStream();
        }

        private void ContentCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Anytime a new stream has finished reading it is added to the content collection
            // React immediately rather than waiting for all content to be loaded at once
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                var content = e.NewItems[0] as HttpContent;
                if (content != null)
                {
                    var contentDisposition = content.Headers.ContentDisposition;

                    // If FileName is null or empty the content is form data
                    if (string.IsNullOrEmpty(contentDisposition.FileName))
                    {
                        var formFieldName = UnquoteToken(contentDisposition.Name) ?? string.Empty;
                        FormData.Add(formFieldName, content.ReadAsStringAsync);
                    }
                }
            }
        }

        /// <summary>
        /// Remove bounding quotes on a token if present.
        /// </summary>
        /// <param name="token">Token to unquote.</param>
        /// <returns>Unquoted token.</returns>
        private static string UnquoteToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return token;
            }

            if (token.StartsWith("\"", StringComparison.Ordinal) && token.EndsWith("\"", StringComparison.Ordinal) && token.Length > 1)
            {
                return token.Substring(1, token.Length - 2);
            }

            return token;
        }
    }
}
