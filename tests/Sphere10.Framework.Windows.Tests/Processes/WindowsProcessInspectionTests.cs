// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class WindowsProcessInspectionTests {
	[Test]
	public async Task EnvironmentReadsTheOwnedProcessAndPreservesDriveAndEmptyValueEntries() {
		var name = "SPHERE10_INSPECTION_" + Guid.NewGuid().ToString("N");
		using var restore = Tools.Scope.ExecuteOnDispose(() => Environment.SetEnvironmentVariable(name, null));
		Environment.SetEnvironmentVariable(name, "owned environment value");
		using var process = WindowsProcess.Open(Environment.ProcessId);
		var values = await process.GetEnvironmentAsync();
		Assert.That(values.Single(item => item.Name == name).Value, Is.EqualTo("owned environment value"));
		var parsed = WindowsProcess.ParseEnvironment("=C:=C:\\Work\0EMPTY=\0VALUE=a=b\0");
		Assert.That(parsed.Select(item => item.Name), Is.EqualTo(new[] { "=C:", "EMPTY", "VALUE" }));
		Assert.That(parsed.Select(item => item.Value), Is.EqualTo(new[] { "C:\\Work", "", "a=b" }));
		Assert.That(() => WindowsProcess.ParseEnvironment("malformed"), Throws.TypeOf<System.IO.InvalidDataException>());
	}

	[Test]
	public async Task TokenQueriesReturnDetachedSidsGroupsPrivilegesAndCurrentJobMembership() {
		using var process = WindowsProcess.Open(Environment.ProcessId);
		using var identity = WindowsIdentity.GetCurrent();
		var token = await process.GetTokenInformationAsync();
		using var current = System.Diagnostics.Process.GetCurrentProcess();
		Assert.That(token.UserSid, Is.EqualTo(identity.User.Value));
		Assert.That(token.OwnerSid, Does.StartWith("S-1-"));
		Assert.That(token.PrimaryGroupSid, Does.StartWith("S-1-"));
		Assert.That(token.IntegritySid, Does.StartWith("S-1-16-"));
		Assert.That(token.TokenType, Is.EqualTo(1));
		Assert.That(token.SessionId, Is.EqualTo(current.SessionId));
		Assert.That(token.ElevationType, Is.InRange(1, 3));
		Assert.That(token.Groups, Is.Not.Empty);
		Assert.That(token.Groups.All(group => group.Sid.StartsWith("S-1-")), Is.True);
		Assert.That(token.Privileges.All(privilege => privilege.Name.StartsWith("Se") && privilege.Luid != 0), Is.True);
		var job = await process.GetJobInformationAsync();
		if (job.Accounting != null)
			Assert.That(job.IsInJob && job.Accounting.ActiveProcesses >= 1, Is.True);
	}

	[Test]
	public async Task ExplicitMemoryAndImageScansRespectResultAndByteBoundsAndJoinCancellation() {
		using var process = WindowsProcess.Open(Environment.ProcessId);
		var image = await process.GetImageStringsAsync(4, 5, 65536);
		Assert.That(image.BytesExamined, Is.LessThanOrEqualTo(65536));
		Assert.That(image.Strings.Length, Is.LessThanOrEqualTo(5));
		Assert.That(image.Strings.All(item => item.Value.Length is >= 4 and <= 4096), Is.True);
		var memory = await process.GetMemoryStringsAsync(4, 5, 65536);
		Assert.That(memory.BytesExamined, Is.LessThanOrEqualTo(65536));
		Assert.That(memory.Strings.Length, Is.LessThanOrEqualTo(5));
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		await Assert.ThatAsync(() => process.GetEnvironmentAsync(cancellationToken: canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => process.GetTokenInformationAsync(canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => process.GetMemoryStringsAsync(cancellationToken: canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(() => process.GetMemoryStrings(1), Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(() => process.GetEnvironment(3), Throws.ArgumentException);
	}

	[Test]
	public void StreamingStringExtractionKeepsChunkBoundariesAndBoundsSingleRuns() {
		var scanner = new ProcessStringScanner(4, 100);
		var ascii = Encoding.ASCII.GetBytes("ASCII_STRING\0");
		scanner.Feed(ascii[..5], 5, 0x1000, null);
		scanner.Feed(ascii[5..], ascii.Length - 5, 0x1005, null);
		scanner.EndRegion();
		var unicode = Encoding.Unicode.GetBytes("WIDE_STRING\0");
		scanner.Feed(unicode[..7], 7, 0x2000, null);
		scanner.Feed(unicode[7..], unicode.Length - 7, 0x2007, null);
		scanner.EndRegion();
		Assert.That(scanner.Strings.Any(item => item.Value == "ASCII_STRING" && item.Address == 0x1000 && !item.IsUnicode), Is.True);
		Assert.That(scanner.Strings.Any(item => item.Value == "WIDE_STRING" && item.Address == 0x2000 && item.IsUnicode), Is.True);
		var bounded = new ProcessStringScanner(4, 1);
		var longRun = Encoding.ASCII.GetBytes(new string('x', 5000) + "\0");
		bounded.Feed(longRun, longRun.Length, 0, null);
		bounded.EndRegion();
		Assert.That(bounded.Strings, Has.Length.EqualTo(1));
		Assert.That(bounded.Strings[0].Value.Length, Is.EqualTo(4096));
		Assert.That(bounded.Strings[0].IsTruncated, Is.True);
	}

	[Test]
	public void NativeMemoryAndTokenLayoutsMatchTheWindowsAbi() {
		Assert.That(Marshal.SizeOf<WinAPI.KERNEL32.MEMORY_BASIC_INFORMATION>(), Is.EqualTo(IntPtr.Size == 8 ? 48 : 28));
		Assert.That(Marshal.SizeOf<WinAPI.KERNEL32.JOBOBJECT_BASIC_ACCOUNTING_INFORMATION>(), Is.EqualTo(48));
		Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.SID_AND_ATTRIBUTES>(), Is.EqualTo(IntPtr.Size == 8 ? 16 : 8));
		Assert.That(Marshal.SizeOf<WinAPI.ADVAPI32.LUID_AND_ATTRIBUTES>(), Is.EqualTo(12));
	}
}

