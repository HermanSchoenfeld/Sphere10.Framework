// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Files;

namespace Sphere10.Framework.UnitTests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class FileHandleReleaseTests {
	[Test]
	public void ExactNativeProcessObjectTypeHandleAndAccessIdentityPassesTheFinalReleaseGuard() {
		var expected = Expected();
		Assert.That(() => FileHandleService.ValidateReleaseEntry(expected, expected.Identity, Entry(expected)), Throws.Nothing);
	}

	[TestCase("process")]
	[TestCase("handle")]
	[TestCase("object")]
	[TestCase("type")]
	[TestCase("access")]
	public void ChangedSourceIdentityPreventsClosureBeforeDestructiveNativeDispatch(string changed) {
		var expected = Expected();
		var process = expected.Identity;
		var entry = Entry(expected);
		switch (changed) {
			case "process": process = process with { StartTimeUtc = process.StartTimeUtc.Value.AddSeconds(1) }; break;
			case "handle": entry.HandleValue = (UIntPtr)456; break;
			case "object": entry.Object = new IntPtr(456); break;
			case "type": entry.ObjectTypeIndex++; break;
			case "access": entry.GrantedAccess++; break;
		}
		Assert.That(() => FileHandleService.ValidateReleaseEntry(expected, process, entry), Throws.InstanceOf<InvalidOperationException>());
	}

	[TestCase("process")]
	[TestCase("handle")]
	[TestCase("object")]
	[TestCase("path")]
	[TestCase("partial")]
	public async Task IncompleteCaptureCannotStartAReleaseWorker(string missing) {
		var expected = Expected();
		expected = missing switch {
			"process" => expected with { Identity = expected.Identity with { StartTimeUtc = null } },
			"handle" => expected with { HandleValue = null },
			"object" => expected with { ObjectAddress = 0 },
			"path" => expected with { Path = string.Empty },
			_ => expected with { Availability = "Partial captured identity" }
		};
		await Assert.ThatAsync(async () => await new FileHandleService().ForceReleaseAsync(expected), Throws.InstanceOf<ArgumentException>());
	}

	private static FileHandleInfo Expected() => new() { Identity = new ProcessIdentity(232, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
		HandleValue = 123, ObjectAddress = 7654, ObjectTypeIndex = 7, GrantedAccess = 0x1234, Path = @"C:\Owned-Fixture\file.txt" };

	private static WinAPI.NTDLL.SYSTEM_HANDLE_TABLE_ENTRY_INFO_EX Entry(FileHandleInfo expected) => new() {
		ProcessId = (UIntPtr)(uint)expected.ProcessId, HandleValue = (UIntPtr)expected.HandleValue.Value,
		Object = new IntPtr((long)expected.ObjectAddress), ObjectTypeIndex = expected.ObjectTypeIndex, GrantedAccess = expected.GrantedAccess
	};
}
