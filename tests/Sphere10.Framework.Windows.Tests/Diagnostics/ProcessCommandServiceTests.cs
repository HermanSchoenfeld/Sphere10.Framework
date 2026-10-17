// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class ProcessCommandServiceTests {
	[Test]
	public async Task VerifiedCallingProcessAllowsAReadOnlyAction() {
		var identity = ProcessIdentity.Capture(Environment.ProcessId);
		var called = false;
		await ProcessCommandService.ExecuteAsync(identity, process => {
			Assert.That(process.Id, Is.EqualTo(Environment.ProcessId));
			Assert.That(process.StartTime.ToUniversalTime(), Is.EqualTo(identity.StartTimeUtc));
			Assert.That(process.HasExited, Is.False);
			Assert.That(process.ProcessName, Is.Not.Empty);
			called = true;
		});
		Assert.That(called, Is.True, "A reusable process API must leave application control policy to its caller.");
	}

	[TestCase(0)]
	[TestCase(-1)]
	public async Task NonpositiveIdentityCannotDispatchAnAction(int processId) {
		var called = false;
		await Assert.ThatAsync(() => ProcessCommandService.ExecuteAsync(new ProcessIdentity(processId, DateTime.UtcNow), _ => called = true),
			Throws.InstanceOf<ArgumentException>());
		Assert.That(called, Is.False);
	}

	[Test]
	public async Task MissingOrStaleCreationTimeCannotDispatchAnAction() {
		var identity = ProcessIdentity.Capture(Environment.ProcessId);
		var called = false;
		await Assert.ThatAsync(() => ProcessCommandService.ExecuteAsync(new ProcessIdentity(identity.ProcessId, null), _ => called = true),
			Throws.InstanceOf<InvalidOperationException>());
		await Assert.ThatAsync(() => ProcessCommandService.ExecuteAsync(new ProcessIdentity(identity.ProcessId, identity.StartTimeUtc.Value.AddTicks(1)), _ => called = true),
			Throws.InstanceOf<InvalidOperationException>());
		Assert.That(called, Is.False);
	}

	[Test]
	public async Task PrecancelledDispatchDoesNotRunItsAction() {
		var identity = ProcessIdentity.Capture(Environment.ProcessId);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var called = false;
		await Assert.ThatAsync(() => ProcessCommandService.ExecuteAsync(identity, _ => called = true, cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
		Assert.That(called, Is.False);
	}
}
