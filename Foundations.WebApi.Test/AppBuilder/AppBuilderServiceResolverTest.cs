// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AppBuilderServiceResolverTest.cs">
//     Copyright (c) 2016. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using System;
    using Microsoft.Owin.Builder;
    using NUnit.Framework;

    [TestFixture]
    public class AppBuilderServiceResolverTest
    {
        [Test]
        public void Constructor_throws_on_null_arguments()
        {
            Assert.Throws<ArgumentNullException>(() => new AppBuilderServiceResolver(null));
        }

        [Test]
        public void GetInstanceOfT_returns_expected_result()
        {
            var app = new AppBuilder();
            var resolver = new AppBuilderServiceResolver(app);
            var expected = new TestType();

            InitializeContainer initializeContainer = container =>
            {
                container.Register<TestType>(() => expected);
            };

            app.UseContainerInitializer(initializeContainer);

            Assert.That(resolver.GetInstance<TestType>(), Is.SameAs(expected));
        }

        [Test]
        public void GetInstance_returns_expected_result()
        {
            var app = new AppBuilder();
            var resolver = new AppBuilderServiceResolver(app);
            var expected = new TestType();

            InitializeContainer initializeContainer = container =>
            {
                container.Register<TestType>(() => expected);
            };

            app.UseContainerInitializer(initializeContainer);

            Assert.That(resolver.GetInstance(typeof(TestType)), Is.SameAs(expected));
        }

        private class TestType
        {
            // Irrelevant
        };
    }
}
