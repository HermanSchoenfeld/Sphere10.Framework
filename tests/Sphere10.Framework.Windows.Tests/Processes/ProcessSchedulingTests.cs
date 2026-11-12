// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class ProcessSchedulingTests {
	[Test]
	public async Task OwnedProcessMetadataAndSchedulingRemainBoundToItsCreationTime() {
		using var child = StartOwnedProcess();
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => { if (!child.HasExited) child.Kill(); child.WaitForExit(5000); });
		var identity = ProcessIdentity.Capture(child.Id);
		using var process = WindowsProcess.Open(child.Id);
		var metadata = await process.ReadMetadataAsync();
		Assert.That(metadata.Identity, Is.EqualTo(identity));
		Assert.That(metadata.Architecture, Is.Not.Empty);
		Assert.That(metadata.UserSid, Does.StartWith("S-1-"));
		Assert.That(metadata.IntegritySid, Does.StartWith("S-1-16-"));
		Assert.That(metadata.Mitigations, Has.Length.EqualTo(14));
		Assert.That(metadata.Mitigations.All(policy => policy.Flags.HasValue || policy.Error.HasValue), Is.True);
		var original = await ProcessSchedulingService.ReadAsync(identity);
		Assert.That(original.AffinityMask, Is.GreaterThan(0));
		Assert.That(original.AvailableCpuSets, Is.Not.Empty);
		Assert.That(original.CpuSetAvailability, Is.Empty);
		var oneProcessor = original.AffinityMask.Value & (~original.AffinityMask.Value + 1);
		await ProcessSchedulingService.SetAffinityAsync(identity, oneProcessor);
		Assert.That((await ProcessSchedulingService.ReadAsync(identity)).AffinityMask, Is.EqualTo(oneProcessor));
		await ProcessSchedulingService.SetAffinityAsync(identity, original.AffinityMask.Value);
		var cpu = original.AvailableCpuSets.First(set => !set.Allocated || set.AllocatedToTargetProcess);
		await ProcessSchedulingService.SetDefaultCpuSetsAsync(identity, new[] { cpu.Id });
		Assert.That((await ProcessSchedulingService.ReadAsync(identity)).DefaultCpuSetIds, Is.EqualTo(new[] { cpu.Id }));
		await ProcessSchedulingService.SetDefaultCpuSetsAsync(identity, original.DefaultCpuSetIds);
		await Assert.ThatAsync(() => ProcessSchedulingService.SetAffinityAsync(identity, 0), Throws.InstanceOf<ArgumentException>());
		await Assert.ThatAsync(() => ProcessSchedulingService.SetDefaultCpuSetsAsync(identity, new[] { uint.MaxValue }), Throws.InstanceOf<ArgumentException>());
		var stale = identity with { StartTimeUtc = identity.StartTimeUtc.Value.AddTicks(1) };
		await Assert.ThatAsync(() => ProcessSchedulingService.SetAffinityAsync(stale, oneProcessor), Throws.TypeOf<InvalidOperationException>());
		await Assert.ThatAsync(() => ProcessSchedulingService.SetDefaultCpuSetsAsync(stale, Array.Empty<uint>()), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public async Task PrecancelledSchedulingActionsNeverDispatch() {
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		var identity = new ProcessIdentity(Environment.ProcessId, null);
		await Assert.ThatAsync(() => ProcessSchedulingService.SetAffinityAsync(identity, 1, canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => ProcessSchedulingService.SetDefaultCpuSetsAsync(identity, Array.Empty<uint>(), canceled.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	private static Process StartOwnedProcess() {
		return Process.Start(new ProcessStartInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe"), "/d /q") {
			UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true
		});
	}
}
