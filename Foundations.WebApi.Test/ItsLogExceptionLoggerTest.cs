// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ItsLogExceptionLoggerTest.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using Its.Log.Instrumentation;
    using NUnit.Framework;
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Web.Http.ExceptionHandling;

    [TestFixture]
    public class ItsLogExceptionLoggerTest
    {
        [Test]
        public void Log_writes_to_Its_Log()
        {
            ExceptionLoggerContext actualContext = null;
            Log.EntryPosted += (sender, args) =>
            {
                actualContext = args.LogEntry.Subject as ExceptionLoggerContext;
            };

            var exception = new Exception("Test");
            var catchBlock = new ExceptionContextCatchBlock("Test", isTopLevel: true, callsHandler: false);
            var exceptionContext = new ExceptionContext(exception, catchBlock);
            var expectedContext = new ExceptionLoggerContext(exceptionContext);

            var logger = new ItsLogExceptionLogger();
            logger.Log(expectedContext);

            Assert.That(actualContext, Is.SameAs(expectedContext));
        }
    }
}
