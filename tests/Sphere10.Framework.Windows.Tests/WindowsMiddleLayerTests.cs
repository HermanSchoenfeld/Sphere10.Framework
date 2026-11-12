// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Files;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WindowsMiddleLayerTests {
	[Test]
	public void RawResultsHaveNoApplicationDisplayMetadataOrFormattedHandleValues() {
		foreach (var type in new[] { typeof(WindowsWindowInfo), typeof(NativeWindowMessageResult), typeof(FileHandleInfo), typeof(FileHandleCapture), typeof(WindowsComputerInfo), typeof(WindowsVolumeInfo) })
			foreach (var property in type.GetProperties())
				Assert.That(property.GetCustomAttributes(false).Any(attribute => attribute is DisplayNameAttribute or System.ComponentModel.DescriptionAttribute), Is.False, $"{type.Name}.{property.Name} is a raw native result, not a screen presentation model.");
		Assert.That(typeof(FileHandleInfo).GetProperty(nameof(FileHandleInfo.HandleValue)).PropertyType, Is.EqualTo(typeof(ulong?)));
	}

	[Test]
	public async Task WindowQueriesAndDispatchRejectCancellationOrUnverifiedTargetsBeforeSending() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Windowing.EnumerateAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await Tools.Windows.Windowing.ReadAtPointerAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await Tools.Windows.Drivers.GetDriverPackagesAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		var target = new WindowsWindowInfo(new IntPtr(123), "Fixture", "Fixture", Environment.ProcessId, 1, "Fixture", null, false, "");
		Assert.That(() => Tools.Windows.Windowing.Post(target, 0, UIntPtr.Zero, IntPtr.Zero), Throws.ArgumentException);
		Assert.That(() => Tools.Windows.Windowing.Send(target, 0, UIntPtr.Zero, IntPtr.Zero, 0, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void ForegroundGrantRejectsInvalidProcessIdsAndReportsAMissingProcess() {
		Assert.That(() => Tools.Windows.Windowing.AllowForeground(0), Throws.InstanceOf<ArgumentOutOfRangeException>());
		Assert.That(() => Tools.Windows.Windowing.AllowForeground(-1), Throws.InstanceOf<ArgumentOutOfRangeException>());
		// Process IDs are multiples of four, so this identifier never names a live process.
		Assert.That(Tools.Windows.Windowing.AllowForeground(int.MaxValue), Is.False);
	}

	[TestCase(@"\SystemRoot\System32\drivers\fixture.sys", @"D:\Windows\System32\drivers\fixture.sys")]
	[TestCase(@"System32\drivers\fixture.sys", @"D:\Windows\System32\drivers\fixture.sys")]
	public void DriverToolsRetainRegisteredLocalPathResolution(string registeredPath, string expected) {
		Assert.That(Tools.Windows.Drivers.ResolveBinaryPath(registeredPath, @"D:\Windows"), Is.EqualTo(expected));
		Assert.That(Tools.Windows.Drivers.ResolveInfPath("fixture.inf", @"D:\Windows"), Is.EqualTo(@"D:\Windows\INF\fixture.inf"));
		Assert.That(() => Tools.Windows.Drivers.ResolveInfPath(@"..\fixture.inf", @"D:\Windows"), Throws.ArgumentException);
		Assert.That(() => Tools.Windows.Drivers.ResolveBinaryPath(@"\\server\fixture.sys", @"D:\Windows"), Throws.ArgumentException);
	}

	[Test]
	public void ToolWorkerDispatchLeavesOrdinaryApplicationArgumentsUnclaimed() {
		var arguments = new[] { "--ordinary-application-option" };
		Assert.That(Tools.Windows.Processes.TryRunSignalWorker(arguments, out var signalExit), Is.False);
		Assert.That(Tools.Windows.UiAutomation.TryRunAutomationWorker(arguments, out var automationExit), Is.False);
		Assert.That(signalExit, Is.Zero);
		Assert.That(automationExit, Is.Zero);
	}

	[Test]
	public async Task CancelledComputerAndVolumeOperationsDoNotEnterNativeMutation() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await WindowsComputer.QueryAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await WindowsComputer.RenameAsync(Environment.MachineName, "Owned-Test", cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await WindowsVolume.SetLabelAsync("C:\\", null, "Owned-Test", cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public void FileCaptureCopiesRawResultMembershipWithoutOwningAnotherProcessesHandles() {
		var row = new FileHandleInfo { HandleValue = 0x123, Path = "Fixture" };
		var rows = new[] { row };
		var capture = new FileHandleCapture { Rows = rows };
		rows[0] = null;
		var returned = capture.Rows;
		returned[0] = null;
		Assert.That(capture.Rows.Single(), Is.SameAs(row));
		Assert.That(capture.Rows.Single().HandleValue, Is.EqualTo(0x123UL));
	}
}
