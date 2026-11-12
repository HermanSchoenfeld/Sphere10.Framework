// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WindowsDeviceTests {
	private const int ErrorFileNotFound = 2;
	private const int ErrorPathNotFound = 3;

	[Test]
	public void ProbeReportsTheWindowsErrorOfAMissingDeviceAndOpenThrowsTheSameError() {
		var path = @"\\.\Sphere10-Missing-Device-" + Guid.NewGuid().ToString("N");
		Assert.That(WindowsDevice.ProbeAccess(path), Is.EqualTo(ErrorFileNotFound).Or.EqualTo(ErrorPathNotFound));
		Assert.That(() => WindowsDevice.Open(path), Throws.TypeOf<Win32Exception>().With.Property(nameof(Win32Exception.NativeErrorCode)).EqualTo(ErrorFileNotFound).Or.Property(nameof(Win32Exception.NativeErrorCode)).EqualTo(ErrorPathNotFound));
	}

	[Test]
	public void AnOpenedDeviceRejectsAnUnknownControlCodeAndADisposedDeviceRejectsEveryControl() {
		const string path = @"\\.\NUL";
		Assume.That(WindowsDevice.ProbeAccess(path), Is.Zero, "The null device must be openable to exercise control dispatch.");
		var device = WindowsDevice.Open(path);
		using (device) {
			Assert.That(device.Path, Is.EqualTo(path));
			Assert.That(device.TryControl(0, null, new byte[8], out var returned), Is.False, "The null device implements no control codes.");
			Assert.That(returned, Is.Zero);
			Assert.That(() => device.Control(0, null), Throws.TypeOf<Win32Exception>());
		}
		Assert.That(() => device.TryControl(0, null, null, out _), Throws.TypeOf<ObjectDisposedException>());
		Assert.That(() => device.Dispose(), Throws.Nothing, "Disposal is idempotent.");
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	public void RejectsAnEmptyDevicePath(string path) {
		Assert.That(() => WindowsDevice.ProbeAccess(path), Throws.ArgumentException);
		Assert.That(() => WindowsDevice.Open(path), Throws.ArgumentException);
	}
}
