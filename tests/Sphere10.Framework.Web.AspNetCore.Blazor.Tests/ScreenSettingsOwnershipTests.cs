// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
using Sphere10.Framework.Utils.BlazorTester.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Verifies renderer-owned demonstration preferences stay nested under their actual component.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ScreenSettingsOwnershipTests {

	[TestCaseSource(nameof(Declarations))]
	public void ActualWebScreensDeclareTheirOwnPublicNestedSettingsWithoutActivation(Type screenType, Type settingsType) {
		Assert.That(settingsType.IsNestedPublic, Is.True);
		Assert.That(settingsType.DeclaringType, Is.EqualTo(screenType));
		Assert.That(Tools.UI.GetScreenSettingsType(screenType), Is.EqualTo(settingsType));
	}

	// Both real component preferences must remain discoverable before the renderer creates a component instance.
	private static TestCaseData[] Declarations() {
		return new TestCaseData[] {
			new(typeof(ComponentScreen), typeof(ComponentScreen.GallerySettings)),
			new(typeof(RegistrationExampleScreen), typeof(RegistrationExampleScreen.RegistrationInspectorSettings)),
		};
	}
}
