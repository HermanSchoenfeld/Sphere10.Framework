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
[NonParallelizable]
[Category("Integration")]
public class EnvironmentUtilTests {
	[Test]
	public void ReadAllAndExpandUseOnlyTheOwnedProcessVariable() {
		var name = "SPHERE10_ENV_API_" + Guid.NewGuid().ToString("N");
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => Environment.SetEnvironmentVariable(name, null, EnvironmentVariableTarget.Process));
		Environment.SetEnvironmentVariable(name, "raw-literal", EnvironmentVariableTarget.Process);
		Assert.That(Tools.Windows.Environment.ReadAll(EnvironmentVariableTarget.Process)[name], Is.EqualTo("raw-literal"));
		Assert.That(Tools.Windows.Environment.ExpandVariables("%" + name + "%"), Is.EqualTo("raw-literal"));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("   ")]
	[TestCase("A=B")]
	[TestCase("A\0B")]
	[TestCase("A\r\nB")]
	public void InvalidNamesAreRejectedWithoutChangingAnyEnvironment(string name) {
		Assert.That(Tools.Windows.Environment.Validate(name, "value", EnvironmentVariableTarget.Process).IsFailure, Is.True);
	}

	[Test]
	public void ValidationAcceptsEmptyValuesAndRejectsTruncatedOrUnsupportedInputs() {
		Assert.That(Tools.Windows.Environment.Validate("Test", string.Empty, EnvironmentVariableTarget.Process).IsSuccess, Is.True);
		Assert.That(Tools.Windows.Environment.Validate("Test", null, EnvironmentVariableTarget.Process).IsFailure, Is.True);
		Assert.That(Tools.Windows.Environment.Validate("Test", "a\0b", EnvironmentVariableTarget.Process).IsFailure, Is.True);
		Assert.That(Tools.Windows.Environment.Validate("Test", "value", (EnvironmentVariableTarget)99).IsFailure, Is.True);
		Assert.That(Tools.Windows.Environment.Validate(new string('a', 255), "value", EnvironmentVariableTarget.User).IsFailure, Is.True);
		Assert.That(Tools.Windows.Environment.Validate(new string('a', 254), "value", EnvironmentVariableTarget.Machine).IsSuccess, Is.True);
		Assert.That(Tools.Windows.Environment.Validate("Test", new string('a', 32767), EnvironmentVariableTarget.Process).IsFailure, Is.True);
	}

	[Test]
	public async Task ProcessVariableCanBeCreatedEditedSetEmptyAndDeleted() {
		var name = "Sphere10_WindowsDiagnostics_Test_" + Guid.NewGuid().ToString("N");
		var original = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
		{
			using var restoreCapturedEnvironment = Tools.Scope.ExecuteOnDispose(() => {
				Environment.SetEnvironmentVariable(name, original, EnvironmentVariableTarget.Process);
			});
			await Tools.Windows.Environment.SetAsync(name, "first", EnvironmentVariableTarget.Process, original);
			Assert.That(await Tools.Windows.Environment.ReadAsync(name, EnvironmentVariableTarget.Process), Is.EqualTo("first"));
			await Tools.Windows.Environment.SetAsync(name, "second", EnvironmentVariableTarget.Process, "first");
			Assert.That(await Tools.Windows.Environment.ReadAsync(name, EnvironmentVariableTarget.Process), Is.EqualTo("second"));
			await Tools.Windows.Environment.SetAsync(name, string.Empty, EnvironmentVariableTarget.Process, "second");
			Assert.That(await Tools.Windows.Environment.ReadAsync(name, EnvironmentVariableTarget.Process), Is.EqualTo(string.Empty));
			await Tools.Windows.Environment.DeleteAsync(name, EnvironmentVariableTarget.Process, string.Empty);
			Assert.That(await Tools.Windows.Environment.ReadAsync(name, EnvironmentVariableTarget.Process), Is.Null);
		}
	}

	[Test]
	public async Task OptimisticUpdateAndDeletePreserveAConcurrentChange() {
		var name = "Sphere10_WindowsDiagnostics_Test_" + Guid.NewGuid().ToString("N");
		var original = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
		{
			using var restoreStaleEnvironment = Tools.Scope.ExecuteOnDispose(() => {
				Environment.SetEnvironmentVariable(name, original, EnvironmentVariableTarget.Process);
			});
			await Tools.Windows.Environment.SetAsync(name, "external change", EnvironmentVariableTarget.Process, original);
			await Assert.ThatAsync(async () => await Tools.Windows.Environment.SetAsync(name, "replacement", EnvironmentVariableTarget.Process, "stale value"), Throws.TypeOf<InvalidOperationException>());
			await Assert.ThatAsync(async () => await Tools.Windows.Environment.DeleteAsync(name, EnvironmentVariableTarget.Process, "stale value"), Throws.TypeOf<InvalidOperationException>());
			Assert.That(Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process), Is.EqualTo("external change"));
		}
	}

	[Test]
	public async Task CancellationBeforeDispatchDoesNotCreateAProcessVariable() {
		var name = "Sphere10_WindowsDiagnostics_Test_" + Guid.NewGuid().ToString("N");
		var original = Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		{
			using var restoreDeletedEnvironment = Tools.Scope.ExecuteOnDispose(() => {
				Environment.SetEnvironmentVariable(name, original, EnvironmentVariableTarget.Process);
			});
			await Assert.ThatAsync(async () => await Tools.Windows.Environment.SetAsync(name, "unused", EnvironmentVariableTarget.Process, original, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
			Assert.That(Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.Process), Is.EqualTo(original));
		}
	}
}
