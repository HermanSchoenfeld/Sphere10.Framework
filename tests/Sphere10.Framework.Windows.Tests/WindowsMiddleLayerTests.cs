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
		await Assert.ThatAsync(async () => await WindowsWindow.EnumerateAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		var target = new WindowsWindowInfo(new IntPtr(123), "Fixture", "Fixture", Environment.ProcessId, 1, "Fixture", null, false, "");
		Assert.That(() => WindowMessageSender.Post(target, 0, UIntPtr.Zero, IntPtr.Zero), Throws.ArgumentException);
		Assert.That(() => WindowMessageSender.Send(target, 0, UIntPtr.Zero, IntPtr.Zero, 0, 0), Throws.InstanceOf<ArgumentOutOfRangeException>());
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
