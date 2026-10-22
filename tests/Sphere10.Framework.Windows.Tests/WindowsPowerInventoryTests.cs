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

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class WindowsPowerInventoryTests {
	[Test]
	public void PowerCapabilitiesMatchesTheModernWindowsSdkLayout() {
		Assert.That(Marshal.SizeOf<WinAPI.POWRPROF.SYSTEM_POWER_CAPABILITIES>(), Is.EqualTo(76));
		Assert.That(Marshal.OffsetOf<WinAPI.POWRPROF.SYSTEM_POWER_CAPABILITIES>(nameof(WinAPI.POWRPROF.SYSTEM_POWER_CAPABILITIES.AoAc)).ToInt32(), Is.EqualTo(20));
		Assert.That(Marshal.OffsetOf<WinAPI.POWRPROF.SYSTEM_POWER_CAPABILITIES>(nameof(WinAPI.POWRPROF.SYSTEM_POWER_CAPABILITIES.AcOnLineWake)).ToInt32(), Is.EqualTo(56));
	}

	[Test]
	public async Task SettingPagesSearchAndSortOneImmutablePublishedCapture() {
		var captures = 0;
		var source = new WindowsPowerSettingDataSource(_ => {
			Interlocked.Increment(ref captures);
			return Task.FromResult(new[] {
				new WindowsPowerSettingInfo { Name = "Sleep Timeout", AcValue = 300 },
				new WindowsPowerSettingInfo { Name = "Sleep Allow", AcValue = 1 },
				new WindowsPowerSettingInfo { Name = "Display Timeout", AcValue = 120 }
			});
		});
		var first = await source.ReadRangeAsync("Sleep", 1, 0, nameof(WindowsPowerSettingInfo.AcValue), SortDirection.Descending);
		var second = await source.ReadRangeAsync("Sleep", 1, 1, nameof(WindowsPowerSettingInfo.AcValue), SortDirection.Descending);
		Assert.Multiple(() => {
			Assert.That(captures, Is.EqualTo(1));
			Assert.That(first.TotalCount, Is.EqualTo(2));
			Assert.That(first.Items.Single().AcValue, Is.EqualTo(300));
			Assert.That(second.Items.Single().AcValue, Is.EqualTo(1));
			Assert.That(source.Capabilities.HasFlag(DataSourceCapabilities.CanCreate), Is.False);
		});
	}

	[Test, Category("WindowsIntegration")]
	public async Task NativePlansAndSettingsRetainIdentifiersAndZeroValues() {
		var plans = await Tools.Windows.Computer.GetPowerPlansAsync();
		Assert.That(plans, Is.Not.Empty);
		Assert.That(plans.Select(plan => plan.Id).Distinct().Count(), Is.EqualTo(plans.Length));
		var active = plans.Single(plan => plan.IsActive);
		Assert.That(active.Name, Is.Not.Empty);
		var settings = await Tools.Windows.Computer.GetPowerSettingsAsync(active.Id);
		Assert.That(settings, Is.Not.Empty);
		Assert.That(settings, Has.All.Matches<WindowsPowerSettingInfo>(row => row.PlanId == active.Id && row.Id != Guid.Empty));
		Assert.That(settings.Any(row => row.AcValue.HasValue || row.DcValue.HasValue), Is.True);
		Assert.That(settings.Where(row => row.AcValue == 0), Has.All.Matches<WindowsPowerSettingInfo>(row => row.AcText.Contains('0')));
		TestContext.Out.WriteLine($"{plans.Length} Plans; {settings.Length} Settings; Active {active.Name} ({active.Id:D})");
	}

	[Test, Category("WindowsIntegration")]
	public async Task NativeSleepDiagnosticsDistinguishCapabilitiesFromInterruptTimes() {
		var rows = await Tools.Windows.Computer.GetPowerDiagnosticsAsync();
		Assert.That(rows.Any(row => row.Name == "S3 Sleep" && string.IsNullOrEmpty(row.ReadError)), Is.True);
		Assert.That(rows.Any(row => row.Name == "S0 Low Power Idle (Modern Standby)"), Is.True);
		Assert.That(rows.Where(row => row.Name.Contains("Interrupt Time")), Has.All.Matches<WindowsPowerDiagnosticInfo>(row => row.Source.Contains("Not Wall Clock")));
	}
}
