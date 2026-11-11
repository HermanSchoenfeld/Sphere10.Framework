// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using NUnit.Framework;

namespace Sphere10.Framework.Application.Tests.Lifecycle;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class AutoRunServicesExtensionsTests {
	[Test]
	public void RegistersAndRemovesOnlyWhenTheRegistrationDiffers() {
		var services = new RecordingAutoRunServices();

		Assert.That(services.SetAutoRunEnabled(AutoRunType.CurrentUser, "Lumina", @"C:\Program Files\Lumina\Lumina.exe", true, "--tray"), Is.True);
		Assert.That(services.SetAutoRunEnabled(AutoRunType.CurrentUser, "Lumina", @"C:\Program Files\Lumina\Lumina.exe", true), Is.False);
		Assert.That(services.SetAutoRunEnabled(AutoRunType.CurrentUser, "Lumina", @"C:\Program Files\Lumina\Lumina.exe", false), Is.True);
		Assert.That(services.SetAutoRunEnabled(AutoRunType.CurrentUser, "Lumina", @"C:\Program Files\Lumina\Lumina.exe", false), Is.False);

		Assert.That(services.Calls, Is.EqualTo(new[] { @"Set:CurrentUser:Lumina:C:\Program Files\Lumina\Lumina.exe:--tray", @"Remove:CurrentUser:Lumina:C:\Program Files\Lumina\Lumina.exe" }));
	}

	[Test]
	public void RejectsMissingArguments() {
		var services = new RecordingAutoRunServices();

		Assert.That(() => ((IAutoRunServices)null).SetAutoRunEnabled(AutoRunType.CurrentUser, "Lumina", "Lumina.exe", true), Throws.ArgumentNullException);
		Assert.That(() => services.SetAutoRunEnabled(AutoRunType.CurrentUser, " ", "Lumina.exe", true), Throws.InstanceOf<System.ArgumentException>());
		Assert.That(() => services.SetAutoRunEnabled(AutoRunType.CurrentUser, "Lumina", null, true), Throws.InstanceOf<System.ArgumentException>());
		Assert.That(services.Calls, Is.Empty);
	}

	private sealed class RecordingAutoRunServices : IAutoRunServices {
		private readonly HashSet<string> _registered = new();

		public List<string> Calls { get; } = new();

		public bool DoesAutoRun(AutoRunType type, string applicationName, string executable) => _registered.Contains(type + ":" + applicationName + ":" + executable);

		public void SetAutoRun(AutoRunType type, string applicationName, string executable, string arguments) {
			_registered.Add(type + ":" + applicationName + ":" + executable);
			Calls.Add("Set:" + type + ":" + applicationName + ":" + executable + ":" + arguments);
		}

		public void RemoveAutoRun(AutoRunType type, string applicationName, string executable) {
			_registered.Remove(type + ":" + applicationName + ":" + executable);
			Calls.Add("Remove:" + type + ":" + applicationName + ":" + executable);
		}
	}
}
