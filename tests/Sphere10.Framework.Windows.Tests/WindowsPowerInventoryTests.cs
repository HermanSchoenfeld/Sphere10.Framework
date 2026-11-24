// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
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
		var source = new ActionAsyncListDataSource<WindowsPowerSettingInfo>(_ => {
			Interlocked.Increment(ref captures);
			return Task.FromResult<IEnumerable<WindowsPowerSettingInfo>>(new[] {
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
			Assert.That(new WindowsPowerSettingDataSource(Guid.Empty).Capabilities.HasFlag(DataSourceCapabilities.CanCreate), Is.False, "The native inventory is read only; the fixture list only supplies rows.");
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

	// Changes only a duplicate of the active plan: the copy is deleted and the original plan is active again afterwards.
	[Test, Category("WindowsIntegration"), NonParallelizable]
	public async Task ADuplicatedPlanTakesCheckedValuesAndIsDeletedWhileTheOriginalPlanIsRestored() {
		var original = (await Tools.Windows.Computer.GetPowerPlansAsync()).Single(plan => plan.IsActive);
		var name = "Framework Test Plan " + Guid.NewGuid().ToString("N").Substring(0, 8);
		var copy = await Tools.Windows.Computer.DuplicatePowerPlanAsync(original.Id, name);
		try {
			var plans = await Tools.Windows.Computer.GetPowerPlansAsync();
			Assert.That(plans.Single(plan => plan.Id == copy).Name, Is.EqualTo(name));
			var settings = await Tools.Windows.Computer.GetPowerSettingsAsync(copy);
			Assert.That(settings.Where(setting => setting.Minimum.HasValue && setting.Maximum.HasValue), Has.All.Matches<WindowsPowerSettingInfo>(setting => setting.Choices.Length == 0),
				"A ranged setting is a number, not a list of its own name.");
			var choice =settings.First(setting => setting.Choices.Length >= 2 && setting.AcValue.HasValue);
			var ranged = settings.First(setting => setting.Choices.Length == 0 && setting.AcValue.HasValue && setting.Minimum.HasValue && setting.Maximum > setting.Minimum
				&& setting.DcValue.HasValue && (setting.Increment ?? 1) <= 1);
			var chosen = choice.Choices.First(option => option.Value != choice.AcValue).Value;
			var rangedValue = ranged.DcValue == ranged.Minimum ? ranged.Minimum.Value + 1 : ranged.Minimum.Value;
			await Tools.Windows.Computer.SetPowerSettingValueAsync(copy, choice.SubgroupId, choice.Id, WindowsPowerSource.AC, chosen);
			await Tools.Windows.Computer.SetPowerSettingValueAsync(copy, ranged.SubgroupId, ranged.Id, WindowsPowerSource.DC, rangedValue);
			var written = await Tools.Windows.Computer.GetPowerSettingsAsync(copy);
			Assert.That(written.Single(setting => setting.Id == choice.Id).AcValue, Is.EqualTo(chosen));
			Assert.That(written.Single(setting => setting.Id == choice.Id).DcValue, Is.EqualTo(choice.DcValue), "Only the chosen power source changes.");
			Assert.That(written.Single(setting => setting.Id == ranged.Id).DcValue, Is.EqualTo(rangedValue));
			var invalid = Enumerable.Range(0, 1000).Select(value => (uint)value).First(value => choice.Choices.All(option => option.Value != value));
			Assert.That(() => Tools.Windows.Computer.SetPowerSettingValueAsync(copy, choice.SubgroupId, choice.Id, WindowsPowerSource.AC, invalid), Throws.ArgumentException);
			if (ranged.Maximum < uint.MaxValue)
				Assert.That(() => Tools.Windows.Computer.SetPowerSettingValueAsync(copy, ranged.SubgroupId, ranged.Id, WindowsPowerSource.DC, ranged.Maximum.Value + 1), Throws.ArgumentException);
			var unchanged = await Tools.Windows.Computer.GetPowerSettingsAsync(original.Id);
			Assert.That(unchanged.Single(setting => setting.Id == choice.Id).AcValue, Is.EqualTo(choice.AcValue), "The original plan keeps its values.");

			await Tools.Windows.Computer.SetActivePowerPlanAsync(copy);
			Assert.That((await Tools.Windows.Computer.GetPowerPlansAsync()).Single(plan => plan.IsActive).Id, Is.EqualTo(copy));
			Assert.That(() => Tools.Windows.Computer.DeletePowerPlanAsync(copy), Throws.ArgumentException, "The active plan cannot be deleted.");
		} finally {
			await Tools.Windows.Computer.SetActivePowerPlanAsync(original.Id);
			await Tools.Windows.Computer.DeletePowerPlanAsync(copy);
		}
		var remaining = await Tools.Windows.Computer.GetPowerPlansAsync();
		Assert.That(remaining.Any(plan => plan.Id == copy), Is.False);
		Assert.That(remaining.Single(plan => plan.IsActive).Id, Is.EqualTo(original.Id));
	}

	[Test, Category("WindowsIntegration")]
	public async Task NativeSleepDiagnosticsDistinguishCapabilitiesFromInterruptTimes() {
		var rows = await Tools.Windows.Computer.GetPowerDiagnosticsAsync();
		Assert.That(rows.Any(row => row.Name == "S3 Sleep" && string.IsNullOrEmpty(row.ReadError)), Is.True);
		Assert.That(rows.Any(row => row.Name == "S0 Low Power Idle (Modern Standby)"), Is.True);
		Assert.That(rows.Where(row => row.Name.Contains("Interrupt Time")), Has.All.Matches<WindowsPowerDiagnosticInfo>(row => row.Source.Contains("Not Wall Clock")));
	}
}
