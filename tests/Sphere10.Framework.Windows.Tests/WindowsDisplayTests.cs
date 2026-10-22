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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Native = Sphere10.Framework.Windows.WinAPI.USER32;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class WindowsDisplayTests {
	[TestCase(0)]
	[TestCase(7)]
	[TestCase(127)]
	public void TruncatedEdidNeverManufacturesPanelMetadata(int length) {
		var edid = WindowsMonitorEdid.Parse(new byte[length]);
		Assert.That(edid.ValidHeader, Is.False);
		Assert.That(edid.Complete, Is.False);
		Assert.That(edid.WidthCm, Is.Null);
		Assert.That(edid.DigitalInput, Is.Null);
	}

	[Test]
	public void BaseBlockDimensionsGammaAndChecksumsAreValidatedBeforeDecoding() {
		var bytes = Edid();
		var edid = WindowsMonitorEdid.Parse(bytes, true);
		Assert.That(edid.ValidHeader && edid.Complete && edid.BlockChecksums.Single(), Is.True);
		Assert.That(edid.OverridePresent, Is.True);
		Assert.That(edid.WidthCm, Is.EqualTo(60));
		Assert.That(edid.HeightCm, Is.EqualTo(34));
		Assert.That(edid.DiagonalInches, Is.EqualTo(Math.Sqrt(60 * 60 + 34 * 34) / 2.54).Within(0.001));
		Assert.That(edid.Gamma, Is.EqualTo(2.2));
		bytes[23]++;
		var invalid = WindowsMonitorEdid.Parse(bytes);
		Assert.That(invalid.BlockChecksums.Single(), Is.False);
		Assert.That(invalid.Gamma, Is.Null);
		Assert.That(invalid.WidthCm, Is.Null);
	}

	[Test]
	public void AspectRatioAndUndefinedGammaAreNotMistakenForPhysicalDimensions() {
		var bytes = Edid();
		bytes[21] = 0;
		bytes[23] = 255;
		Checksum(bytes);
		var edid = WindowsMonitorEdid.Parse(bytes);
		Assert.That(edid.WidthCm, Is.Null);
		Assert.That(edid.HeightCm, Is.Null);
		Assert.That(edid.Gamma, Is.Null);
	}

	[Test]
	public void DeclaredMissingExtensionIsIncompleteAndRawDataRemainsExportable() {
		var bytes = Edid();
		bytes[126] = 1;
		Checksum(bytes);
		var edid = WindowsMonitorEdid.Parse(bytes);
		Assert.That(edid.Complete, Is.False);
		Assert.That(edid.RawHex, Is.EqualTo(Convert.ToHexString(bytes)));
	}

	[Test]
	public void NativeDisplayStructuresMatchWindowsSdkLayouts() {
		Assert.That(Marshal.SizeOf<Native.DISPLAYCONFIG_PATH_INFO>(), Is.EqualTo(72));
		Assert.That(Marshal.SizeOf<Native.DISPLAYCONFIG_MODE_INFO>(), Is.EqualTo(64));
		Assert.That(Marshal.SizeOf<Native.DISPLAYCONFIG_SOURCE_DEVICE_NAME>(), Is.EqualTo(84));
		Assert.That(Marshal.SizeOf<Native.DISPLAYCONFIG_TARGET_DEVICE_NAME>(), Is.EqualTo(420));
		Assert.That(Marshal.SizeOf<Native.DISPLAYCONFIG_TARGET_PREFERRED_MODE>(), Is.EqualTo(80));
		Assert.That(Marshal.SizeOf<Native.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO>(), Is.EqualTo(32));
		Assert.That(Marshal.SizeOf<Native.DISPLAYCONFIG_GET_ADVANCED_COLOR_INFO_2>(), Is.EqualTo(36));
		Assert.That(Marshal.SizeOf<Native.DISPLAY_DEVMODE>(), Is.EqualTo(220));
	}

	[Test]
	public async Task PagingAndFilteringReuseOneWorkerCaptureUntilExplicitReload() {
		var caller = Environment.CurrentManagedThreadId;
		var captureThread = caller;
		var calls = 0;
		var source = new WindowsDisplayDataSource(_ => {
			captureThread = Environment.CurrentManagedThreadId;
			Interlocked.Increment(ref calls);
			return Task.FromResult(new[] { new WindowsDisplayInfo { Name = "Panel A" }, new WindowsDisplayInfo { Name = "Panel B" }, new WindowsDisplayInfo { Name = "Other" } });
		});
		var first = await source.ReadRangeAsync("Panel", 1, 0, nameof(WindowsDisplayInfo.Name), SortDirection.Ascending);
		var second = await source.ReadRangeAsync("Panel", 1, 1, nameof(WindowsDisplayInfo.Name), SortDirection.Ascending);
		Assert.That(first.TotalCount, Is.EqualTo(2));
		Assert.That(second.Items.Single().Name, Is.EqualTo("Panel B"));
		Assert.That(calls, Is.EqualTo(1));
		Assert.That(captureThread, Is.Not.EqualTo(caller));
		await source.ReloadAsync();
		Assert.That(calls, Is.EqualTo(2));
	}

	[Test, Category("Integration")]
	public async Task CurrentDesktopReportsRealTargetPathsAndSaneNativeModes() {
		using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
		var displays = await Tools.Windows.Display.GetDisplaysAsync(timeout.Token);
		Assert.That(displays.Select(display => display.Id).Distinct().Count(), Is.EqualTo(displays.Length));
		foreach (var display in displays) {
			Assert.That(display.Id, Is.Not.Empty);
			Assert.That(display.PathFlags & 1, Is.EqualTo(1));
			if (display.ActiveTiming != null) {
				Assert.That(display.ActiveTiming.ActiveWidth, Is.InRange(1U, 65535U));
				Assert.That(display.ActiveTiming.TotalWidth, Is.GreaterThanOrEqualTo(display.ActiveTiming.ActiveWidth));
			}
			TestContext.Progress.WriteLine($"{display.GdiDeviceName}: {display.Name}, {display.Connector}, {display.Modes.Length} source modes; HDR supported={display.HdrSupported}, active={display.ActiveColorMode}");
		}
	}

	private static byte[] Edid() {
		var bytes = new byte[128];
		Array.Copy(new byte[] { 0, 255, 255, 255, 255, 255, 255, 0 }, bytes, 8);
		bytes[18] = 1; bytes[19] = 4; bytes[20] = 128; bytes[21] = 60; bytes[22] = 34; bytes[23] = 120;
		Checksum(bytes);
		return bytes;
	}

	private static void Checksum(byte[] bytes) => bytes[127] = unchecked((byte)-bytes.Take(127).Sum(value => (int)value));
}
