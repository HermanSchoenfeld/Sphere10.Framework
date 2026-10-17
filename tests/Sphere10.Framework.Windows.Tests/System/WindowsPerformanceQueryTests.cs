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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[NonParallelizable]
public class WindowsPerformanceQueryTests {
	[Test]
	public async Task ScalarAndWildcardCountersKeepRawIdentityAndUseNativeCalculations() {
		const string scalarPath = @"\System\Processes";
		const string wildcardPath = @"\Process(*)\ID Process";
		using var query = await new PerformanceCounterUtil().OpenQueryAsync(new[] { scalarPath, wildcardPath });
		var samples = await query.ReadRawSamplesAsync();
		var scalar = samples.Single(item => item.CounterPath == scalarPath);
		Assert.That(scalar.IsValid, Is.True, $"Native PDH status: 0x{scalar.Status:X8}");
		Assert.That(scalar.FirstValue, Is.GreaterThan(0));
		Assert.That(scalar.TimestampUtc, Is.InRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1)));
		Assert.That(scalar.RawTimestampFileTime, Is.GreaterThan(0));
		Assert.That(WindowsPerformanceQuery.ConvertLocalTimestampToUtc(scalar.RawTimestampFileTime, TimeZoneInfo.Local),
			Is.InRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1)));
		Assert.That(query.CalculateValue(scalar), Is.GreaterThan(0));
		Assert.That(samples.Any(item => item.CounterPath == wildcardPath && item.InstanceName.Length > 0 && item.IsValid && item.FirstValue == Environment.ProcessId), Is.True);
		using var other = new WindowsPerformanceQuery(new[] { scalarPath });
		Assert.That(() => other.CalculateValue(scalar), Throws.ArgumentException);
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		await Assert.ThatAsync(() => query.ReadRawSamplesAsync(canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		query.Dispose();
		Assert.That(() => query.ReadRawSamples(), Throws.TypeOf<ObjectDisposedException>());
	}

	[TestCase(600)]
	[TestCase(-420)]
	[TestCase(345)]
	public void PdhLocalFileTimeConvertsToUtcWithoutChangingTheRawValue(int offsetMinutes) {
		var zone = TimeZoneInfo.CreateCustomTimeZone("test", TimeSpan.FromMinutes(offsetMinutes), "test", "test");
		var expected = new DateTime(2026, 10, 15, 3, 19, 35, DateTimeKind.Utc).AddTicks(1234);
		var wallTime = TimeZoneInfo.ConvertTimeFromUtc(expected, zone);
		var raw = DateTime.SpecifyKind(wallTime, DateTimeKind.Utc).ToFileTimeUtc();
		var result = WindowsPerformanceQuery.ConvertLocalTimestampToUtc(raw, zone);
		Assert.That(result, Is.EqualTo(expected));
		Assert.That(result.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
		Assert.That(DateTime.FromFileTimeUtc(raw).Ticks, Is.EqualTo(wallTime.Ticks), "The provider value remains a local wall-clock FILETIME for native calculations.");
	}

	[Test]
	public void PdhTimeWithoutAnUnambiguousUtcInstantRemainsUnavailable() {
		var daylight = TimeZoneInfo.AdjustmentRule.CreateAdjustmentRule(new DateTime(2020, 1, 1), new DateTime(2030, 12, 31), TimeSpan.FromHours(1),
			TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 3, 2, DayOfWeek.Sunday),
			TimeZoneInfo.TransitionTime.CreateFloatingDateRule(new DateTime(1, 1, 1, 2, 0, 0), 11, 1, DayOfWeek.Sunday));
		var zone = TimeZoneInfo.CreateCustomTimeZone("test daylight", TimeSpan.FromHours(-8), "test", "standard", "daylight", new[] { daylight });
		foreach (var wallTime in new[] { new DateTime(2026, 3, 8, 2, 30, 0), new DateTime(2026, 11, 1, 1, 30, 0) })
			Assert.That(WindowsPerformanceQuery.ConvertLocalTimestampToUtc(DateTime.SpecifyKind(wallTime, DateTimeKind.Utc).ToFileTimeUtc(), zone), Is.Null);
		Assert.That(WindowsPerformanceQuery.ConvertLocalTimestampToUtc(0, zone), Is.Null);
		Assert.That(WindowsPerformanceQuery.ConvertLocalTimestampToUtc(long.MaxValue, zone), Is.Null);
	}

	[Test]
	public void AnUnavailableCounterRetainsItsNativeFailureInsteadOfProducingAValidZero() {
		const string path = @"\Sphere10 nonexistent object\missing";
		using var query = new WindowsPerformanceQuery(new[] { path });
		var sample = query.ReadRawSamples().Single();
		Assert.That(sample.CounterPath, Is.EqualTo(path));
		Assert.That(sample.IsValid, Is.False);
		Assert.That(sample.Status, Is.Not.Zero);
		Assert.That(() => query.CalculateValue(sample), Throws.TypeOf<Win32Exception>());
	}

	[Test]
	public void NativeCounterArrayLayoutsRetainTheirWindowsPacking() {
		Assert.That(Marshal.SizeOf<WinAPI.PDH.RAW_COUNTER>(), Is.EqualTo(40));
		Assert.That(Marshal.SizeOf<WinAPI.PDH.RAW_COUNTER_ITEM>(), Is.EqualTo(48));
		Assert.That(Marshal.SizeOf<WinAPI.PDH.FORMATTED_COUNTER_VALUE>(), Is.EqualTo(16));
	}
}

