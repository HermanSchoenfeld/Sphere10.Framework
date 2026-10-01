// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ScreenActivationPolicyTests {
	[Test]
	public void CompatibleDeclarationsPromoteInferredPolicyToExplicitPolicy() {
		var registry = new ScreenActivationPolicyRegistry();
		registry.RegisterInstance(typeof(FirstScreen), ScreenActivationMode.MultiInstance);
		Assert.That(registry.IsExplicitlyDeclared(typeof(FirstScreen)), Is.False);

		registry.RegisterDeclarations(new[] {
			new KeyValuePair<Type, ScreenActivationMode>(typeof(FirstScreen), ScreenActivationMode.MultiInstance),
			new KeyValuePair<Type, ScreenActivationMode>(typeof(FirstScreen), ScreenActivationMode.MultiInstance)
		});

		Assert.That(registry.IsExplicitlyDeclared(typeof(FirstScreen)), Is.True);
		Assert.That(registry.TryGetPolicy(typeof(FirstScreen), out var policy), Is.True);
		Assert.That(policy, Is.EqualTo(ScreenActivationMode.MultiInstance));
	}

	[Test]
	public void DeclarationBatchRejectsConflictsBeforeAddingAnyPolicy() {
		var registry = new ScreenActivationPolicyRegistry();
		registry.RegisterDeclarations(new[] { new KeyValuePair<Type, ScreenActivationMode>(typeof(FirstScreen), ScreenActivationMode.SingleInstance) });

		Assert.That(() => registry.RegisterDeclarations(new[] {
			new KeyValuePair<Type, ScreenActivationMode>(typeof(SecondScreen), ScreenActivationMode.SingleInstance),
			new KeyValuePair<Type, ScreenActivationMode>(typeof(FirstScreen), ScreenActivationMode.MultiInstance)
		}), Throws.InstanceOf<ArgumentException>());
		Assert.That(registry.TryGetPolicy(typeof(SecondScreen), out _), Is.False);
		Assert.That(registry.IsExplicitlyDeclared(typeof(SecondScreen)), Is.False);
		Assert.That(registry.TryGetPolicy(typeof(FirstScreen), out var policy), Is.True);
		Assert.That(policy, Is.EqualTo(ScreenActivationMode.SingleInstance));
	}

	[Test]
	public void ConflictingDeclarationsWithinOneBatchLeaveTheRegistryEmpty() {
		var registry = new ScreenActivationPolicyRegistry();

		Assert.That(() => registry.RegisterDeclarations(new[] {
			new KeyValuePair<Type, ScreenActivationMode>(typeof(FirstScreen), ScreenActivationMode.SingleInstance),
			new KeyValuePair<Type, ScreenActivationMode>(typeof(FirstScreen), ScreenActivationMode.MultiInstance)
		}), Throws.InstanceOf<ArgumentException>());
		Assert.That(registry.TryGetPolicy(typeof(FirstScreen), out _), Is.False);
		Assert.That(registry.IsExplicitlyDeclared(typeof(FirstScreen)), Is.False);
	}

	[Test]
	public void AdapterValidationFailureDoesNotPartiallyInstallDeclarations() {
		var registry = new ScreenActivationPolicyRegistry();

		Assert.That(() => registry.RegisterDeclarations(new[] {
			new KeyValuePair<Type, ScreenActivationMode>(typeof(AdaptedScreen), ScreenActivationMode.SingleInstance),
			new KeyValuePair<Type, ScreenActivationMode>(typeof(FirstScreen), ScreenActivationMode.SingleInstance)
		}, screenType => Tools.UI.ValidateScreenType(screenType, typeof(IAdapterScreen))), Throws.InstanceOf<ArgumentException>());
		Assert.That(registry.TryGetPolicy(typeof(AdaptedScreen), out _), Is.False);
		Assert.That(registry.IsExplicitlyDeclared(typeof(AdaptedScreen)), Is.False);
	}

	[TestCase(ScreenActivationMode.SingleInstance, ScreenActivationMode.MultiInstance)]
	[TestCase(ScreenActivationMode.MultiInstance, ScreenActivationMode.SingleInstance)]
	public void RegisteredInstancePolicyCannotBeChanged(ScreenActivationMode initialMode, ScreenActivationMode otherMode) {
		var registry = new ScreenActivationPolicyRegistry();
		registry.RegisterInstance(typeof(FirstScreen), initialMode);

		Assert.That(() => registry.RegisterInstance(typeof(FirstScreen), otherMode), Throws.InstanceOf<ArgumentException>());
		Assert.That(registry.TryGetPolicy(typeof(FirstScreen), out var policy), Is.True);
		Assert.That(policy, Is.EqualTo(initialMode));
		Assert.That(registry.IsExplicitlyDeclared(typeof(FirstScreen)), Is.False);
	}

	[Test]
	public void ValidationDoesNotRegisterAnInstance() {
		var registry = new ScreenActivationPolicyRegistry();
		registry.Validate(typeof(FirstScreen), ScreenActivationMode.SingleInstance);

		Assert.That(registry.TryGetPolicy(typeof(FirstScreen), out _), Is.False);
		Assert.That(registry.IsExplicitlyDeclared(typeof(FirstScreen)), Is.False);
	}

	[TestCase(typeof(string))]
	[TestCase(typeof(IApplicationScreen))]
	[TestCase(typeof(AbstractScreen))]
	[TestCase(typeof(GenericScreen<>))]
	[TestCase(null)]
	public void InvalidScreenTypesAreRejectedWithoutRegisteringPolicy(Type screenType) {
		var registry = new ScreenActivationPolicyRegistry();

		Assert.That(() => registry.RegisterInstance(screenType, ScreenActivationMode.SingleInstance), Throws.InstanceOf<ArgumentException>());
		if (screenType != null)
			Assert.That(registry.TryGetPolicy(screenType, out _), Is.False);
	}

	[TestCase(-1)]
	[TestCase(2)]
	[TestCase(int.MaxValue)]
	public void InvalidActivationModesAreRejectedWithoutRegisteringPolicy(int mode) {
		var registry = new ScreenActivationPolicyRegistry();

		Assert.That(() => registry.RegisterDeclarations(new[] {
			new KeyValuePair<Type, ScreenActivationMode>(typeof(FirstScreen), (ScreenActivationMode)mode)
		}), Throws.InstanceOf<ArgumentException>());
		Assert.That(registry.TryGetPolicy(typeof(FirstScreen), out _), Is.False);
		Assert.That(registry.IsExplicitlyDeclared(typeof(FirstScreen)), Is.False);
	}

	private class FirstScreen : IApplicationScreen {
	}

	private class SecondScreen : IApplicationScreen {
	}

	private interface IAdapterScreen : IApplicationScreen {
	}

	private class AdaptedScreen : IAdapterScreen {
	}

	private abstract class AbstractScreen : IApplicationScreen {
	}

	private class GenericScreen<T> : IApplicationScreen {
	}
}
