// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NullTraceOutputFactory.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi
{
    using System.IO;
    using Microsoft.Owin.Hosting.Tracing;

    /// <summary>
    /// A default no-op implementation of the trace output factory.
    /// </summary>
    /// <seealso cref="Microsoft.Owin.Hosting.Tracing.ITraceOutputFactory" />
    public class NullTraceOutputFactory : ITraceOutputFactory
    {
        /// <summary>
        /// Used to create the trace output.
        /// </summary>
        /// <param name="outputFile">Ignored. Here to satisfy interface.</param>
        /// <returns>A null StreamWriter.</returns>
        public TextWriter Create(string outputFile)
        {
            return StreamWriter.Null;
        }
    }
}