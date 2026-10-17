// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class ComputerHardwareInventoryTests {
	[TestCase("memory-modules")]
	[TestCase("memory-arrays")]
	[TestCase("enclosures")]
	[TestCase("video-controllers")]
	[TestCase("devices")]
	[TestCase("monitors")]
	[TestCase("printers")]
	public async Task ReadOnlyHardwareInventoryReturnsDetachedDataWithoutRequiringOptionalDevices(string query) {
		var computer = Tools.Windows.Computer;
		Array result;
		try {
			result = query switch {
				"memory-modules" => await computer.GetMemoryModulesAsync(),
				"memory-arrays" => await computer.GetMemoryArraysAsync(),
				"enclosures" => await computer.GetEnclosuresAsync(),
				"video-controllers" => await computer.GetVideoControllersAsync(),
				"devices" => await computer.GetDevicesAsync(),
				"monitors" => await computer.GetMonitorsAsync(),
				_ => await computer.GetPrintersAsync()
			};
		} catch (ManagementException error) when (error.ErrorCode is ManagementStatus.InvalidClass or ManagementStatus.InvalidNamespace or ManagementStatus.AccessDenied) {
			Assert.Ignore($"Windows provider unavailable for {query}: {error.ErrorCode}. No inventory was manufactured.");
			return;
		}
		Assert.That(result, Is.Not.Null);
		Assert.That(result.Cast<object>().All(item => item != null && item is not ManagementBaseObject && item is not IDisposable), Is.True);
		TestContext.Progress.WriteLine($"{query}: {result.Length} detached rows.");
		foreach (var item in result) {
			switch (item) {
				case WindowsMemoryArrayInfo memory:
					Assert.That(memory.MaximumCapacityBytes == null || memory.MaximumCapacityBytes % 1024 == 0, Is.True, "Memory-array capacities are normalized to bytes.");
					break;
				case WindowsEnclosureInfo enclosure:
					Assert.That(enclosure.ChassisTypes, Is.Not.Null);
					break;
				case WindowsVideoControllerInfo video when video.DriverDate.HasValue:
					Assert.That(video.DriverDate.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
					break;
				case WindowsPlugAndPlayDeviceInfo device:
					Assert.That(device.Present, Is.True, "Only currently present PnP entities are queried.");
					Assert.That(device.HardwareIds, Is.Not.Null);
					Assert.That(device.CompatibleIds, Is.Not.Null);
					break;
				case WindowsMonitorInfo monitor:
					Assert.That(monitor.Name.Length, Is.LessThanOrEqualTo(256));
					Assert.That(monitor.Name.IndexOf('\0'), Is.EqualTo(-1));
					Assert.That(monitor.SerialNumber.IndexOf('\0'), Is.EqualTo(-1));
					break;
			}
		}
	}

	[Test]
	public void SynchronousMemoryQueryCompletesBeforeItsDetachedDataIsReturned() {
		var arrays = Tools.Windows.Computer.GetMemoryArrays();
		Assert.That(arrays, Is.Not.Null);
		Assert.That(arrays.All(item => item != null), Is.True);
		Assert.That(arrays.Where(item => item.MaximumCapacityBytes.HasValue).All(item => item.MaximumCapacityBytes > 0), Is.True);
	}

	[TestCase("memory-modules")]
	[TestCase("memory-arrays")]
	[TestCase("enclosures")]
	[TestCase("video-controllers")]
	[TestCase("devices")]
	[TestCase("monitors")]
	[TestCase("printers")]
	public async Task PreCanceledHardwareQueriesDoNotEnterTheProvider(string query) {
		var computer = Tools.Windows.Computer;
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var pending = query switch {
			"memory-modules" => (Task)computer.GetMemoryModulesAsync(cancellation.Token),
			"memory-arrays" => computer.GetMemoryArraysAsync(cancellation.Token),
			"enclosures" => computer.GetEnclosuresAsync(cancellation.Token),
			"video-controllers" => computer.GetVideoControllersAsync(cancellation.Token),
			"devices" => computer.GetDevicesAsync(cancellation.Token),
			"monitors" => computer.GetMonitorsAsync(cancellation.Token),
			_ => computer.GetPrintersAsync(cancellation.Token)
		};
		await Assert.ThatAsync(async () => await pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(pending.IsCanceled, Is.True);
	}

	[Test]
	public void ExtendedMemoryCapacityUsesKilobytesAndTakesPriorityOverTheLegacyValue() {
		Assert.That(WindowsComputer.ConvertMemoryCapacity(67108864, 1), Is.EqualTo(64UL * 1024 * 1024 * 1024));
		Assert.That(WindowsComputer.ConvertMemoryCapacity(0, 67108864), Is.EqualTo(64UL * 1024 * 1024 * 1024));
		Assert.That(WindowsComputer.ConvertMemoryCapacity(null, 67108864), Is.EqualTo(64UL * 1024 * 1024 * 1024));
		Assert.That(WindowsComputer.ConvertMemoryCapacity(ulong.MaxValue / 1024, null), Is.EqualTo(ulong.MaxValue - 1023));
	}

	[Test]
	public void UnknownOrOverflowingMemoryCapacityRemainsUnavailable() {
		Assert.That(WindowsComputer.ConvertMemoryCapacity(null, null), Is.Null);
		Assert.That(WindowsComputer.ConvertMemoryCapacity(0, 0), Is.Null);
		Assert.That(WindowsComputer.ConvertMemoryCapacity(ulong.MaxValue / 1024 + 1, 1024), Is.Null);
		Assert.That(WindowsComputer.ConvertMemoryCapacity(ulong.MaxValue, uint.MaxValue), Is.Null);
	}

	[Test]
	public void MonitorIdentifiersAreNullTerminatedBoundedAsciiRatherThanUtf16OrTrailingData() {
		Assert.That(WindowsComputer.DecodeMonitorText(null), Is.Empty);
		Assert.That(WindowsComputer.DecodeMonitorText(Array.Empty<ushort>()), Is.Empty);
		Assert.That(WindowsComputer.DecodeMonitorText(new ushort[] { 68, 69, 76, 0, 88 }), Is.EqualTo("DEL"));
		Assert.That(WindowsComputer.DecodeMonitorText(new ushort[] { 77, 0x100, 49 }), Is.EqualTo("M\uFFFD1"));
		Assert.That(WindowsComputer.DecodeMonitorText(Enumerable.Repeat((ushort)65, 4096).ToArray()), Is.EqualTo(new string('A', 256)));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("**************.******+***")]
	[TestCase("invalid")]
	public void UnavailableDriverDatesStayUnavailable(string text) {
		Assert.That(WindowsComputer.ParseDmtfDate(text), Is.Null);
	}

	[Test]
	public void DriverDatesUseTheirDeclaredOffsetWhenConvertedToUtc() {
		var result = WindowsComputer.ParseDmtfDate("20261016093000.000000+600");
		Assert.That(result, Is.EqualTo(new DateTime(2026, 10, 15, 23, 30, 0, DateTimeKind.Utc)));
		Assert.That(result.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
	}
}
