// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BasicWebApiLogPolicyTest.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Its.Log.Instrumentation;
    using NUnit.Framework;

    [TestFixture]
    public class BasicWebApiLogPolicyTest
    {
        [Test]
        public void Initialize_configures_log_policy_that_writes_to_trace_listeners_by_default()
        {
            var stringBuilder = new StringBuilder();
            using (var stringWriter = new StringWriter(stringBuilder, CultureInfo.InvariantCulture))
            {
                var traceListener = new TextWriterTraceListener(stringWriter);
                Trace.Listeners.Add(traceListener);

                BasicWebApiLogPolicy.Initialize();
                Log.Write("Initialize_configures_log_policy_that_writes_to_trace_listeners_by_default");

                // Format should be: "UTC Date: Message"
                var resultParts = stringBuilder.ToString().Split(':');
                var message = resultParts.Last().Trim();
                var dateString = string.Join(":", resultParts.Reverse().Skip(1).Reverse()); // All but last element
                var written = DateTime.Parse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                Assert.That(message, Is.EqualTo("Initialize_configures_log_policy_that_writes_to_trace_listeners_by_default"));
                Assert.That(written, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromMilliseconds(1000)));
            }
        }

        [Test]
        public void Initialize_configures_log_policy_that_writes_to_custom_destination_when_Log_provided()
        {
            var stringBuilder = new StringBuilder();

            BasicWebApiLogPolicy.Initialize();
            var originalLog = BasicWebApiLogPolicy.Log;
            BasicWebApiLogPolicy.Log = s => stringBuilder.AppendLine(s);
            Log.Write("Initialize_configures_log_policy_that_writes_to_custom_destination_when_Log_provided");
            BasicWebApiLogPolicy.Log = originalLog;

            // Format should be: "UTC Date: Message"
            var resultParts = stringBuilder.ToString().Split(':');
            var message = resultParts.Last().Trim();
            var dateString = string.Join(":", resultParts.Reverse().Skip(1).Reverse()); // All but last element
            var written = DateTime.Parse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

            Assert.That(message, Is.EqualTo("Initialize_configures_log_policy_that_writes_to_custom_destination_when_Log_provided"));
            Assert.That(written, Is.EqualTo(DateTime.Now).Within(TimeSpan.FromMilliseconds(500)));
        }
    }
}
