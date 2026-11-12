// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture, NonParallelizable]
public class WindowsProcessInspectionTests {
	[Test]
	public async Task SelfTokenAndMitigationsRetainExactIdentityAndIndependentAvailability() {
		var identity = await ProcessIdentity.CaptureAsync(Environment.ProcessId);
		using var process = WindowsProcess.Open(identity.ProcessId);
		var token = await process.GetTokenInformationAsync();
		Assert.That(token.Process, Is.EqualTo(identity));
		using var native = WindowsIdentity.GetCurrent();
		Assert.That(token.UserSid, Is.EqualTo(native.User.Value));
		Assert.That(token.Groups.Select(group => group.Sid), Is.SupersetOf(new[] { token.PrimaryGroupSid }));
		Assert.That(token.CapturedAtUtc, Is.InRange(DateTime.UtcNow.AddSeconds(-10), DateTime.UtcNow));
		var policies = await process.GetMitigationsAsync();
		Assert.That(policies.Select(row => row.Name), Is.Unique);
		Assert.That(policies, Has.Length.EqualTo(18));
		Assert.That(policies.All(row => row.Flags.HasValue != row.Error.HasValue), Is.True);
		Assert.That(policies.Single(row => row.Name == "DEP").Permanent.HasValue, Is.EqualTo(policies.Single(row => row.Name == "DEP").Flags.HasValue));
	}

	[Test]
	public async Task ExplicitModuleHeadersUseMappedImageAndKeepOrdinaryInventoryCheap() {
		using var process = WindowsProcess.Open(Environment.ProcessId);
		var ordinary = await process.GetModulesAsync();
		Assert.That(ordinary.All(row => row.CapturedAtUtc == null && row.Machine == null), Is.True);
		var details = await process.GetModulesWithDetailsAsync();
		var native = details.First(row => row.Name.Equals("kernel32.dll", StringComparison.OrdinalIgnoreCase));
		Assert.That(native.Machine, Is.EqualTo(Environment.Is64BitProcess ? 0x8664 : 0x14c), native.Availability);
		Assert.That(native.Classification, Is.EqualTo("Native PE"), native.Availability);
		Assert.That(native.MappingType, Is.EqualTo(0x1000000));
		Assert.That(native.CapturedAtUtc, Is.Not.Null);
		Assert.That(native.Availability, Is.Empty);
	}

	[Test]
	public async Task ReadOnlyPageStateDistinguishesResidentAndUnmappedAddresses() {
		var pointer = Marshal.AllocHGlobal(8192);
		using var free = Tools.Scope.ExecuteOnDispose(() => Marshal.FreeHGlobal(pointer));
		Marshal.WriteByte(pointer, 0, 42);
		using var process = WindowsProcess.Open(Environment.ProcessId);
		var address = unchecked((ulong)pointer.ToInt64());
		var rows = await process.QueryWorkingSetAsync(new[] { address, 0UL });
		Assert.That(rows[0].Address, Is.EqualTo(address));
		Assert.That(rows[0].IsResident, Is.True);
		Assert.That(rows[0].Protection, Is.Not.Null);
		Assert.That(rows[1].IsResident, Is.False);
		Assert.That(rows[1].Protection, Is.Null);
		using var cancelled = new CancellationTokenSource();
		cancelled.Cancel();
		await Assert.ThatAsync(() => process.QueryWorkingSetAsync(new[] { address }, cancelled.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(() => process.QueryWorkingSetAsync(new ulong[65537]), Throws.InstanceOf<ArgumentException>());
	}

	[Test]
	public async Task HistoricalAddressResolutionRequiresMatchingLocalImageEvidenceAndNeverUsesPid() {
		using var process = WindowsProcess.Open(Environment.ProcessId);
		var modules = await process.GetModulesAsync();
		var module = modules.Single(row => row.Name.Equals("kernel32.dll", StringComparison.OrdinalIgnoreCase));
		using var file = File.OpenRead(module.FilePath);
		using var reader = new PEReader(file, PEStreamOptions.LeaveOpen);
		var headers = reader.PEHeaders;
		var captured = new WindowsSymbolModule(unchecked((ulong)module.BaseAddress.Value.ToInt64()), (uint)module.SizeBytes.Value,
			module.FilePath, unchecked((uint)headers.CoffHeader.TimeDateStamp), headers.PEHeader.CheckSum);
		var address = captured.BaseAddress + (uint)headers.PEHeader.AddressOfEntryPoint;
		var resolved = await Tools.Windows.Symbols.ResolveAddressesAsync(new[] { address }, new[] { captured });
		Assert.That(resolved.Frames.Single().InstructionPointer, Is.EqualTo(address));
		Assert.That(resolved.Frames.Single().ModulePath, Is.EqualTo(module.FilePath), string.Join(" | ", resolved.Issues));
		var rejected = await Tools.Windows.Symbols.ResolveAddressesAsync(new[] { address }, new[] { captured with { TimeDateStamp = captured.TimeDateStamp + 1 } });
		Assert.That(rejected.Frames.Single().SymbolName, Is.Null.Or.Empty);
		Assert.That(rejected.Frames.Single().ModulePath, Is.Null.Or.Empty);
		Assert.That(rejected.Issues, Has.Length.EqualTo(1));
		var ambiguous = await Tools.Windows.Symbols.ResolveAddressesAsync(new[] { address }, new[] { captured, captured with { BaseAddress = captured.BaseAddress + 1 } });
		Assert.That(ambiguous.Frames.Single().ModulePath, Is.Null.Or.Empty);
		Assert.That(ambiguous.Issues, Has.Length.EqualTo(2));
	}

	[Test]
	public void LocalClrImageLookupHonorsRequestedPeIdentityAndPreservesUncheckedLookup() {
		var original = Path.Combine(Environment.SystemDirectory, "kernel32.dll");
		var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "owned-locator-" + Guid.NewGuid().ToString("N") + ".dll");
		File.Copy(original, path);
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => File.Delete(path));
		int timestamp;
		int imageSize;
		int timestampOffset;
		using (var stream = File.OpenRead(path)) {
			using var reader = new PEReader(stream);
			timestamp = reader.PEHeaders.CoffHeader.TimeDateStamp;
			imageSize = reader.PEHeaders.PEHeader.SizeOfImage;
			timestampOffset = reader.PEHeaders.CoffHeaderStartOffset + 4;
		}
		var locator = new LocalDiagnosticFileLocator(new[] { Path.GetDirectoryName(path) });
		Assert.That(locator.FindPEImage(path, timestamp, imageSize, true), Is.EqualTo(path));
		Assert.That(locator.FindPEImage(path, timestamp + 1, imageSize, true), Is.Null);
		Assert.That(locator.FindPEImage(path, timestamp, imageSize + 1, true), Is.Null);
		Assert.That(locator.FindPEImage(path, timestamp + 1, imageSize + 1, false), Is.EqualTo(path));
		using (var changed = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None)) {
			changed.Position = timestampOffset;
			changed.Write(EndianBitConverter.Little.GetBytes(timestamp + 1));
		}
		Assert.That(locator.FindPEImage(path, timestamp, imageSize, true), Is.Null, "A changed same-path image must not satisfy the original captured identity.");
		Assert.That(locator.FindPEImage(Path.GetFileName(path), timestamp, imageSize, true), Is.Null);
		Assert.That(locator.FindPEImage(Path.GetFileName(path), timestamp + 1, imageSize, true), Is.EqualTo(path));
	}

	[Test]
	public async Task ExplicitWatchOnlyEnablesOwnedHelperAndDrainsWithExclusiveReader() {
		var start = new ProcessStartInfo("pwsh.exe");
		start.ArgumentList.Add("-NoProfile");
		start.ArgumentList.Add("-NonInteractive");
		start.ArgumentList.Add("-Command");
		start.ArgumentList.Add("[Console]::WriteLine('READY'); for ($i = 0; $i -lt 200; $i++) { $ownedBytes = [byte[]]::new(8388608); for ($j = 0; $j -lt $ownedBytes.Length; $j += 4096) { $ownedBytes[$j] = 1 }; [Threading.Thread]::Sleep(50) }");
		await using var helper = await WindowsProcessWorkerSession.StartAsync(start, new WindowsProcessWorkerSessionOptions { KillDescendants = true });
		Assert.That(await helper.ReadLineAsync(), Is.EqualTo("READY"));
		using var watch = await WindowsWorkingSetWatch.StartAsync(helper.Identity);
		await Assert.ThatAsync(() => WindowsWorkingSetWatch.StartAsync(helper.Identity), Throws.InstanceOf<InvalidOperationException>());
		await Task.Delay(200);
		var sample = await watch.ReadAsync();
		Assert.That(sample.Process, Is.EqualTo(helper.Identity));
		Assert.That(sample.Faults, Is.Not.Empty, sample.Availability);
		Assert.That(sample.Faults.All(row => row.InstructionPointer != 0), Is.True);
		Assert.That(sample.Faults.Length, Is.LessThanOrEqualTo(65536));
		watch.Dispose();
		await Assert.ThatAsync(() => watch.ReadAsync(), Throws.InstanceOf<ObjectDisposedException>());
	// Disposal is only reader release; the exclusively owned helper is then terminated by its session.
	}
}
