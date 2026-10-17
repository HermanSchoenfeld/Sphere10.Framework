// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class WindowsProcessApiTests {
	[Test]
	public void OwnedProcessHandleQueriesRawIdentityCountersThreadsAndModules() {
		using var managed = Process.GetCurrentProcess();
		using var process = WindowsProcess.Open(Environment.ProcessId);
		Assert.That(process.Identity, Is.EqualTo(new ProcessIdentity(managed.Id, managed.StartTime.ToUniversalTime())));
		Assert.That(process.HasExited, Is.False);
		Assert.That(process.ExecutablePath, Is.EqualTo(Environment.ProcessPath).IgnoreCase);
		var counters = process.ReadCounters();
		Assert.That(counters.Identity, Is.EqualTo(process.Identity));
		Assert.That(counters.WorkingSetBytes, Is.GreaterThan(0));
		Assert.That(counters.HandleCount, Is.GreaterThan(0));
		Assert.That(process.GetThreads(), Is.Not.Empty);
		Assert.That(process.GetModules().Any(module => !string.IsNullOrEmpty(module.FilePath)), Is.True);
		process.Dispose();
		Assert.That(() => process.ReadCounters(), Throws.InstanceOf<ObjectDisposedException>());
	}

	[Test]
	public void ToolhelpEnumerationIncludesTheOwnedProcessWithoutSamplingPolicy() {
		var rows = Tools.Windows.Processes.GetProcesses();
		var owned = rows.Single(row => row.ProcessId == Environment.ProcessId);
		Assert.That(owned.ThreadCount, Is.GreaterThan(0));
		Assert.That(owned.ExecutableName, Is.Not.Empty);
		Assert.That(Tools.Windows.Processes.GetProcessName(Environment.ProcessId), Is.Not.Empty);
	}

	[Test]
	public void SystemQueryExposesRawCountersWithoutHistory() {
		var counters = WindowsSystemInformation.GetCounters();
		Assert.That(counters.PhysicalMemoryTotalBytes, Is.GreaterThan(0));
		Assert.That(counters.PhysicalMemoryAvailableBytes, Is.InRange(0, counters.PhysicalMemoryTotalBytes));
		Assert.That(counters.LogicalProcessorCount, Is.GreaterThan(0));
		Assert.That(counters.ProcessCount, Is.GreaterThan(0));
		Assert.That(counters.IdleTicks, Is.GreaterThanOrEqualTo(0));
	}

	[Test]
	public async Task CanceledEnumerationDoesNotEnterNativeEnumeration() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Processes.GetProcessesAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task CanceledProcessDetailQueriesRejectBeforeOpeningManagedOrProviderObjects() {
		using var process = WindowsProcess.Open(Environment.ProcessId);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await process.GetThreadsAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await process.GetModulesAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await process.GetCommandLineAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(process.HasExited, Is.False);
	}

	[Test]
	public async Task NamedPipeQueriesReturnTheActualConnectedPeers() {
		var name = "Sphere10-Pipe-Peer-" + Guid.NewGuid().ToString("N");
		using var server = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
		using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
		using var lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(5));
		var connected = server.WaitForConnectionAsync(lifetime.Token);
		await client.ConnectAsync(lifetime.Token);
		await connected;
		Assert.That(NamedPipePeer.GetClientProcessId(server), Is.EqualTo(Environment.ProcessId));
		Assert.That(NamedPipePeer.GetServerProcessId(client), Is.EqualTo(Environment.ProcessId));
	}
}
