// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class WindowsManagementQueryTests {
	[TestCase(0)]
	[TestCase(1)]
	[TestCase(256)]
	public void IpmiByteArrayVariantPreservesAllByteValues(int count) {
		var bytes = Enumerable.Range(0, count).Select(value => (byte)value).ToArray();
		var native = WindowsVariantMarshaller.ConvertToUnmanaged(bytes);
		try {
			Assert.That(native.Type, Is.EqualTo((ushort)(VarEnum.VT_ARRAY | VarEnum.VT_UI1)));
			Assert.That(WindowsVariantMarshaller.ConvertToManaged(native), Is.EqualTo(bytes));
		} finally {
			WindowsVariantMarshaller.Free(native);
		}
	}

	[Test]
	public void NativeVariantLayoutMatchesRuntimeVariant() {
		Assert.That(Unsafe.SizeOf<WinAPI.OLEAUT32.VARIANT>(), Is.EqualTo(Unsafe.SizeOf<ComVariant>()));
		Assert.That(Marshal.OffsetOf<WinAPI.OLEAUT32.VARIANT>(nameof(WinAPI.OLEAUT32.VARIANT.Data)).ToInt32(), Is.EqualTo(8));
	}

	[Test]
	public void IntegerSafeArrayPreservesValuesAndOrder() {
		var values = new[] { int.MinValue, 0, 17, int.MaxValue };
		var native = WindowsInt32SafeArrayMarshaller.ConvertToUnmanaged(values);
		try {
			Assert.That(WindowsInt32SafeArrayMarshaller.ConvertToManaged(native), Is.EqualTo(values));
		} finally {
			WindowsInt32SafeArrayMarshaller.Free(native);
		}
	}

	[TestCase("Win32_BIOS", "Manufacturer", "SMBIOSBIOSVersion")]
	[TestCase("Win32_SystemEnclosure", "Manufacturer", "ChassisTypes")]
	[TestCase("Win32_PhysicalMemory", "DeviceLocator", "Capacity")]
	[Explicit("Reads local WMI providers to compare native and System.Management results.")]
	[Category("NativeIntegration")]
	public void NativeQueryMatchesManagedProvider(string className, string first, string second) {
		var query = "SELECT " + first + "," + second + " FROM " + className;
		using var expectedQuery = new ManagementObjectSearcher(query);
		using var expectedObjects = expectedQuery.Get();
		var expected = new List<object[]>();
		foreach (ManagementObject item in expectedObjects) {
			using (item)
				expected.Add(new[] { item[first], item[second] });
		}
		using var actualQuery = new WindowsManagementQuery(TimeSpan.FromSeconds(5));
		var actual = new List<object[]>();
		foreach (var item in actualQuery.Read(query)) {
			using (item)
				actual.Add(new[] { item[first], item[second] });
		}
		Assert.That(actual, Is.EqualTo(expected));
	}

	[Test]
	[Explicit("Reads method metadata and invokes only GetOwner on the test process.")]
	[Category("NativeIntegration")]
	public void NativeMethodParametersAndReadOnlyInvocationWork() {
		using var query = new WindowsManagementQuery(TimeSpan.FromSeconds(5));
		using var process = query.Read("SELECT * FROM Win32_Process WHERE ProcessId=" + Environment.ProcessId).Single();
		using var result = process.InvokeMethod("GetOwner", null, TimeSpan.FromSeconds(5));
		Assert.That(result["ReturnValue"], Is.EqualTo(0u));
		Assert.That(result["User"], Is.Not.Null.And.Not.Empty);
		using var computer = query.Read("SELECT * FROM Win32_ComputerSystem").Single();
		using var parameters = computer.GetMethodParameters("Rename");
		parameters["Name"] = "Unused Native Test Name";
		Assert.That(parameters["Name"], Is.EqualTo("Unused Native Test Name"));
	}

	[Test]
	[Explicit("Compares embedded scheduled-task trigger and action objects without modifying tasks.")]
	[Category("NativeIntegration")]
	public void EmbeddedTaskObjectsMatchManagedProvider() {
		const string scope = @"root\Microsoft\Windows\TaskScheduler";
		const string query = "SELECT TaskPath,TaskName,Triggers,Actions FROM MSFT_ScheduledTask";
		using var expectedQuery = new ManagementObjectSearcher(scope, query);
		using var expectedObjects = expectedQuery.Get();
		var expected = new List<string>();
		foreach (ManagementObject task in expectedObjects) {
			using (task) {
				foreach (var property in new[] { "Triggers", "Actions" }) {
					var children = task[property] as ManagementBaseObject[] ?? Array.Empty<ManagementBaseObject>();
					foreach (var child in children) {
						using (child)
							expected.Add(task["TaskPath"] + "/" + task["TaskName"] + "/" + property + "/" + child.ClassPath.ClassName);
					}
				}
			}
		}
		using var actualQuery = new WindowsManagementQuery(TimeSpan.FromSeconds(10), scope);
		var actual = new List<string>();
		foreach (var task in actualQuery.Read(query)) {
			using (task) {
				foreach (var property in new[] { "Triggers", "Actions" })
					foreach (var child in task[property] as WindowsManagementObject[] ?? Array.Empty<WindowsManagementObject>())
						actual.Add(task["TaskPath"] + "/" + task["TaskName"] + "/" + property + "/" + child.ClassName);
			}
		}
		Assert.That(actual.OrderBy(item => item), Is.EqualTo(expected.OrderBy(item => item)));
	}

	[Test]
	[Explicit("Queries a deliberately absent WMI class to verify provider errors.")]
	[Category("NativeIntegration")]
	public void NativeFailurePreservesManagementStatus() {
		using var query = new WindowsManagementQuery(TimeSpan.FromSeconds(5));
		var error = Assert.Throws<ManagementException>(() => query.Read("SELECT * FROM Sphere10_Nonexistent_Interop_Test_Class").ToArray());
		Assert.That(error.ErrorCode, Is.EqualTo(ManagementStatus.InvalidClass));
	}
}
