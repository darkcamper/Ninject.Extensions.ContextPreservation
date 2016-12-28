//-------------------------------------------------------------------------------
// <copyright file="ContextPreservationTests.cs" company="bbv Software Services AG">
//   Copyright (c) 2010-2011 bbv Software Services AG
//   Author: Remo Gloor remo.gloor@bbv.ch
//
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//       http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
//-------------------------------------------------------------------------------

using System.Linq;
using Ninject.Activation;
using Ninject.Activation.Caching;
using Ninject.Infrastructure;
using Ninject.Planning;
using Ninject.Planning.Bindings;
using Ninject.Planning.Bindings.Resolvers;
using Ninject.Selection;

namespace Ninject.Extensions.ContextPreservation
{
    using System;

    using FluentAssertions;

    using Ninject;
    using Ninject.Extensions.ContextPreservation.Fakes;
    using Ninject.Parameters;
    using Xunit;
    
    /// <summary>
    /// Tests the implementation of <see cref="ContextPreservation"/>.
    /// </summary>
    public class ContextPreservationTests : IDisposable
    {
        /// <summary>
        /// The kernel used in the tests.
        /// </summary>
        private readonly IKernelConfiguration kernelConfiguration;

        private IReadOnlyKernel kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextPreservationTests"/> class.
        /// </summary>
        public ContextPreservationTests()
        {
          this.kernelConfiguration = new KernelConfiguration(new NinjectSettings
          {
              LoadExtensions = false,
              
          });

          kernelConfiguration.Load(new ContextPreservationModule());
        
        }

        /// <summary>
        /// Disposes the kernel.
        /// </summary>
        public void Dispose()
        {
            this.kernel.Dispose();
        }

        /// <summary>
        /// The context is preserved when a factory created new objects.
        /// </summary>
        [Fact]
        public void ContextPreserved()
        {
            this.kernelConfiguration.Bind<WeaponFactory>().ToSelf();
            this.kernelConfiguration.Bind<IWeapon>().To<Sword>().WhenInjectedInto<WeaponFactory>();
            this.kernelConfiguration.Bind<IJewel>().To<RedJewel>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var factory = this.kernel.Get<WeaponFactory>();
            var child = factory.CreateWeapon();

            child.Should().NotBeNull();
        }

        /// <summary>
        /// Parameters are passed through factories.
        /// </summary>
        [Fact]
        public void ParametersArePassed()
        {
            const string Name = "TheName";
            this.kernelConfiguration.Bind<WeaponFactory>().ToSelf();
            this.kernelConfiguration.Bind<INamedWeapon>().To<NamedDagger>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var factory = this.kernel.Get<WeaponFactory>();
            var child = factory.CreateNamedWeapon(Name);

            child.Should().NotBeNull();
            child.Name.Should().Be(Name);
        }

        /// <summary>
        /// Targets the is resolution root owner.
        /// </summary>
        [Fact]
        public void TargetIsResolutionRootOwner()
        {
            this.kernelConfiguration.Bind<WeaponFactory>().ToSelf().Named("Warrior");
            this.kernelConfiguration.Bind<IWeapon>().To<Sword>().WhenParentNamed("Warrior");
            this.kernelConfiguration.Bind<IJewel>().To<RedJewel>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var factory = this.kernel.Get<WeaponFactory>();
            var child = factory.CreateWeapon();

            child.Should().NotBeNull();
        }
        
        /// <summary>
        /// The ContextPreservingGet extension method uses a 
        /// <see cref="ContextPreservingResolutionRoot"/> to get the requested instance.
        /// </summary>
        [Fact]
        public void ContextPreservingGetExtensionMethodWithoutArguments()
        {
            this.kernelConfiguration.Bind<Warrior>().ToSelf();
            this.kernelConfiguration.Bind<IWeapon>().ToMethod(ctx => ctx.ContextPreservingGet<Dagger>());
            this.kernelConfiguration.Bind<Dagger>().ToSelf().WhenInjectedInto<Warrior>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var parent = this.kernel.Get<Warrior>();

            parent.Weapon.Should().NotBeNull();
        }

        /// <summary>
        /// The ContextPreservingGet extension method uses a 
        /// <see cref="ContextPreservingResolutionRoot"/> to get the requested instance.
        /// Works also with name.
        /// </summary>
        [Fact]
        public void ContextPreservingGetExtensionMethodWithName()
        {
            this.kernelConfiguration.Bind<Warrior>().ToSelf();
            this.kernelConfiguration.Bind<IWeapon>().ToMethod(ctx => ctx.ContextPreservingGet<NamedDagger>("1"));
            this.kernelConfiguration.Bind<NamedDagger>().ToSelf()
                       .WhenInjectedInto<Warrior>().Named("1").WithConstructorArgument("name", "1");
            this.kernelConfiguration.Bind<NamedDagger>().ToSelf()
                       .WhenInjectedInto<Warrior>().Named("2").WithConstructorArgument("name", "2");
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var parent = this.kernel.Get<Warrior>();

            parent.Weapon.Should().NotBeNull();
            ((INamedWeapon)parent.Weapon).Name.Should().Be("1");
        }

        [Fact]
        public void ContextPreservingGetExtensionMethodForClosedGenericTypesDefinedByGenericArgument()
        {
            this.kernelConfiguration.Bind<ParentWithOpenGeneric>().ToSelf();
            this.kernelConfiguration.Bind<IOpenGeneric<int>>().ToMethod(ctx => ctx.ContextPreservingGet<OpenGeneric<int>>())
                .WhenInjectedInto<ParentWithOpenGeneric>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var parent = this.kernel.Get<ParentWithOpenGeneric>();

            parent.OpenGeneric.Should().NotBeNull();
        }

        [Fact]
        public void ContextPreservingGetExtensionMethodForClosedGenericTypesDefinedByType()
        {
            this.kernelConfiguration.Bind<ParentWithOpenGeneric>().ToSelf();
            this.kernelConfiguration.Bind(typeof(IOpenGeneric<int>)).ToMethod(ctx => ctx.ContextPreservingGet(typeof(OpenGeneric<int>)))
                .WhenInjectedInto<ParentWithOpenGeneric>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var parent = this.kernel.Get<ParentWithOpenGeneric>();

            parent.OpenGeneric.Should().NotBeNull();
        }
        
        [Fact]
        public void ContextPreservingGetExtensionMethodForOpenGenericTypes()
        {
            this.kernelConfiguration.Bind<ParentWithOpenGeneric>().ToSelf();
            this.kernelConfiguration.Bind(typeof(IOpenGeneric<>)).ToMethod(ctx => ctx.ContextPreservingGet(typeof(OpenGeneric<>)))
                .WhenInjectedInto<ParentWithOpenGeneric>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var parent = this.kernel.Get<ParentWithOpenGeneric>();

            parent.OpenGeneric.Should().NotBeNull();
        }

        /// <summary>
        /// The ContextPreservingGet extension method uses a 
        /// <see cref="ContextPreservingResolutionRoot"/> to get the requested instance.
        /// Works also with constraint.
        /// </summary>
        [Fact]
        public void ContextPreservingGetExtensionMethodWithConstraint()
        {
            this.kernelConfiguration.Bind<Warrior>().ToSelf();
            this.kernelConfiguration.Bind<IWeapon>().ToMethod(ctx => ctx.ContextPreservingGet<NamedDagger>(m => m.Has("2")));
            this.kernelConfiguration.Bind<NamedDagger>().ToSelf()
                       .WhenInjectedInto<Warrior>().WithMetadata("1", null).WithConstructorArgument("name", "1");
            this.kernelConfiguration.Bind<NamedDagger>().ToSelf()
                       .WhenInjectedInto<Warrior>().WithMetadata("2", null).WithConstructorArgument("name", "2");
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var parent = this.kernel.Get<Warrior>();

            parent.Weapon.Should().NotBeNull();
            ((INamedWeapon)parent.Weapon).Name.Should().Be("2");
        }

        /// <summary>
        /// The ContextPreservingGet extension method uses a 
        /// <see cref="ContextPreservingResolutionRoot"/> to get the requested instance.
        /// Works also with parameters.
        /// </summary>
        [Fact]
        public void ContextPreservingGetExtensionMethodWithParameters()
        {
            this.kernelConfiguration.Bind<Warrior>().ToSelf();
            this.kernelConfiguration.Bind<IWeapon>().ToMethod(
                ctx => ctx.ContextPreservingGet<NamedSword>(
                            new ConstructorArgument("name", "3"), 
                            new PropertyValue("Inscription", "4")));
            this.kernelConfiguration.Bind<NamedSword>().ToSelf().WhenInjectedInto<Warrior>();
            this.kernelConfiguration.Bind<IJewel>().To<RedJewel>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var parent = this.kernel.Get<Warrior>();

            parent.Weapon.Should().NotBeNull();
            ((NamedSword)parent.Weapon).Name.Should().Be("3");
            ((NamedSword)parent.Weapon).Inscription.Should().Be("4");
        }

        /// <summary>
        /// The GetContextPreservingResolutionRoot extension method creates a
        /// <see cref="ContextPreservingResolutionRoot"/> for the current context instance. 
        /// </summary>
        [Fact]
        public void GetContextPreservingResolutionRootExtensionMethod()
        {
            this.kernelConfiguration.Bind<Warrior>().ToSelf();
            this.kernelConfiguration.Bind<IWeapon>().ToMethod(ctx => ctx.GetContextPreservingResolutionRoot().Get<Dagger>());
            this.kernelConfiguration.Bind<Dagger>().ToSelf().WhenInjectedInto<Warrior>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var parent = this.kernel.Get<Warrior>();

            parent.Weapon.Should().NotBeNull();
        }

        /// <summary>
        /// Targets the is resolution root owner.
        /// </summary>
        [Fact]
        public void ContextualConditions()
        {
            this.kernelConfiguration.Bind<IWeapon>().To<Sword>()
                .When(request => request.ParentRequest.ParentRequest.Service == typeof(Town));
            this.kernelConfiguration.Bind<IWeapon>().To<NamedSword>()
                .When(request => request.ParentRequest.ParentRequest.Service == typeof(Village))
                .WithConstructorArgument("name", "Excalibur");
            this.kernelConfiguration.Bind<IWeaponFactory>().To<WeaponFactory>();
            this.kernelConfiguration.Bind<IJewel>().To<RedJewel>();
            kernel = kernelConfiguration.BuildReadonlyKernel();

            var town = this.kernel.Get<Town>();
            var village = this.kernel.Get<Village>();

            town.WeaponFactory.CreateWeapon().Should().BeOfType<Sword>();
            village.WeaponFactory.CreateWeapon().Should().BeOfType<NamedSword>();
        }     
    }
}