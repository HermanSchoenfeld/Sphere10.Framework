// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Files;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class FileToolDiscoveryTests {
	[Test]
	public async Task FactoriesReturnFreshServicesAndCancelledRequestsNeverStartTheirDiagnosticHost() {
		var host = new WindowsProcessWorkerHost(typeof(FileHandleService).Assembly.Location);
		var handles = Tools.Windows.Files.CreateHandleService(host);
		var another = Tools.Windows.Files.CreateHandleService(host);
		Assert.That(handles, Is.Not.SameAs(another), "The per-thread utility must not retain a shared service or its configured host.");
		Assert.That(Tools.Windows.Files.CreateHandleService(), Is.Not.SameAs(Tools.Windows.Files.CreateHandleService()));
		var inspection = Tools.Windows.Files.CreateInspectionService();
		Assert.That(inspection, Is.Not.SameAs(Tools.Windows.Files.CreateInspectionService()));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await handles.CaptureAsync(cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await handles.CaptureObjectsAsync(new FileHandleCaptureOptions(), cancellationToken: cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await another.FindBlockingProcessesAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await inspection.InspectAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")), cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public void AnExplicitNullHandleHostRetainsTheOriginalConstructorsRejection() {
		Assert.That(() => new FileHandleService(null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("workerHost"));
		Assert.That(() => Tools.Windows.Files.CreateHandleService(null), Throws.ArgumentNullException.With.Property("ParamName").EqualTo("workerHost"));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("--ordinary-application-mode")]
	public void HandleWorkerDispatchLeavesUnrelatedStartupArgumentsAndConsoleConfigurationUntouched(string argument) {
		var args = argument == null ? null : argument.Length == 0 ? Array.Empty<string>() : new[] { argument };
		var encoding = Console.OutputEncoding;
		Assert.That(Tools.Windows.Files.TryRunHandleWorker(args, out var exitCode), Is.False);
		Assert.That(exitCode, Is.Zero);
		Assert.That(Console.OutputEncoding, Is.SameAs(encoding), "An unrecognized argument must not initialize worker transport or change the host console.");
	}

	[Test]
	public void DumpWorkerDispatchLeavesUnrelatedStartupArgumentsAndConsoleConfigurationUntouched() {
		var encoding = Console.OutputEncoding;
		Assert.That(Tools.Windows.Dumps.TryRunCaptureWorker(new[] { "--ordinary-application-mode" }, out var exitCode), Is.False);
		Assert.That(exitCode, Is.Zero);
		Assert.That(Console.OutputEncoding, Is.SameAs(encoding));
	}
}
