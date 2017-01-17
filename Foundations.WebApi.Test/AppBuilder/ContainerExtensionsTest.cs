// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContainerExtensionsTest.cs">
//     Copyright (c) 2017. All rights reserved. Licensed under the MIT license. See LICENSE file in
//     the project root for full license information.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Spritely.Foundations.WebApi.Test
{
    using Microsoft.Owin.Builder;
    using NSubstitute;
    using NUnit.Framework;
    using Owin;

    [TestFixture]
    public class ContainerExtensionsTest
    {
        [Test]
        public void UserContainerInitializer_registers_method_for_callback_on_container_creation()
        {
            var app = Substitute.For<IAppBuilder>();
            var initializeContainer = Substitute.For<InitializeContainer>();

            app.UseContainerInitializer(initializeContainer);

            app.GetContainer();

            initializeContainer.ReceivedWithAnyArgs(requiredNumberOfCalls: 1);
        }

        [Test]
        public void GetInstanceOfT_returns_expected_result()
        {
            var expected = new TestType();
            var app = new AppBuilder();

            InitializeContainer initializeContainer = container =>
            {
                container.Register<TestType>(() => expected);
            };

            app.UseContainerInitializer(initializeContainer);

            Assert.That(app.GetInstance<TestType>(), Is.SameAs(expected));
        }

        [Test]
        public void GetInstance_returns_expected_result()
        {
            var expected = new TestType();
            var app = new AppBuilder();

            InitializeContainer initializeContainer = container =>
            {
                container.Register<TestType>(() => expected);
            };

            app.UseContainerInitializer(initializeContainer);

            Assert.That(app.GetInstance(typeof(TestType)), Is.SameAs(expected));
        }

        private class TestType
        {
            // Irrelevant
        };
    }
}
