// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Startup;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class WindowsStartupRegistrationTests {
	[Test]
	public async Task PublishedSourcePagesAndFiltersWithoutReadingNativeProvidersAgain() {
		var captures = 0;
		var source = new ActionAsyncListDataSource<WindowsStartupRegistration>(_ => {
			Interlocked.Increment(ref captures);
			return Task.FromResult<IEnumerable<WindowsStartupRegistration>>(Enumerable.Range(0, 17).Select(index => new WindowsStartupRegistration { Identity = index.ToString(), Name = "Task " + index, Location = "Boot" }).ToArray());
		});
		var first = await source.ReadRangeAsync("Boot", 3, 0, nameof(WindowsStartupRegistration.Name), SortDirection.Descending);
		var second = await source.ReadRangeAsync("Boot", 3, 1, nameof(WindowsStartupRegistration.Name), SortDirection.Descending);
		Assert.That(first.TotalCount, Is.EqualTo(17));
		Assert.That(second.Items.Count(), Is.EqualTo(3));
		Assert.That(second.Items.Select(row => row.Identity).Intersect(first.Items.Select(row => row.Identity)), Is.Empty);
		Assert.That(captures, Is.EqualTo(1));
		Assert.That(source.Capabilities.HasFlag(DataSourceCapabilities.CanPage), Is.True);
	}

	[Test]
	public async Task NativeOptInSourcesRetainTaskTriggersAndBoundedCaptureAvailability() {
		var rows = await Tools.Windows.Startup.ReadRegistrationsAsync(WindowsStartupSources.All, queryTimeoutSeconds: 3);
		Assert.That(rows.Any(row => row.Family == WindowsStartupSources.LogonRegistry), Is.True);
		var tasks = rows.Where(row => row.Family == WindowsStartupSources.ScheduledTasks).ToArray();
		Assert.That(tasks, Is.Not.Empty);
		Assert.That(tasks.Any(row => row.Trigger.Contains("MSFT_TaskBootTrigger", StringComparison.Ordinal) || row.Trigger.Contains("MSFT_TaskLogonTrigger", StringComparison.Ordinal)),
			Is.True, string.Join(Environment.NewLine, tasks.Select(row => row.Availability)));
		Assert.That(rows.Select(row => row.Identity).Distinct(StringComparer.OrdinalIgnoreCase).Count(), Is.EqualTo(rows.Length));
		var bounded = await Tools.Windows.Startup.ReadRegistrationsAsync(WindowsStartupSources.All, maximumRows: 1, queryTimeoutSeconds: 3);
		Assert.That(bounded.Length, Is.EqualTo(2));
		Assert.That(bounded.Single(row => row.Identity == "limit").Availability, Does.Contain("limit"));
	}

	[Test]
	public async Task NoFamiliesAndPreCanceledCaptureDoNotQueryUnselectedProviders() {
		Assert.That(await Tools.Windows.Startup.ReadRegistrationsAsync(WindowsStartupSources.None), Is.Empty);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Startup.ReadRegistrationsAsync(WindowsStartupSources.All, cancellationToken: cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(async () => await Tools.Windows.Startup.ReadRegistrationsAsync((WindowsStartupSources)16), Throws.InstanceOf<ArgumentException>());
	}

	[TestCase("\"C:\\Tools\\Sample App.exe\" --example", "C:\\Tools\\Sample App.exe", "--example")]
	[TestCase("C:\\Tools\\Sample.exe --example", "C:\\Tools\\Sample.exe", "--example")]
	[TestCase("C:\\Tools\\Sample.exe --example=a,b", "C:\\Tools\\Sample.exe", "--example=a,b")]
	public async Task ExplicitAbsoluteCommandsPreserveArgumentsAndDoNotRequireOrLaunchTheirTargets(string command, string expectedPath, string arguments) {
		var target = await Task.Run(() => Tools.Windows.Startup.ReadTarget(command));
		Assert.That(target.Path, Is.EqualTo(expectedPath));
		Assert.That(target.Arguments, Is.EqualTo(arguments));
	}

	[TestCase("explorer.exe")]
	[TestCase("C:\\Program Files\\Ambiguous.exe --example")]
	[TestCase("\\\\remote\\share\\Program.exe")]
	[TestCase("C:\\Tools\\First.exe,C:\\Tools\\Second.exe")]
	[TestCase("%UNDEFINED_SYSTEMEXPERT_STARTUP_TEST%\\Program.exe")]
	public async Task RelativeAmbiguousNetworkAndMultiCommandTargetsRemainUnavailable(string command) {
		var target = await Task.Run(() => Tools.Windows.Startup.ReadTarget(command));
		Assert.That(target.Path, Is.Empty);
		Assert.That(target.Availability, Is.Not.Empty);
	}

	[Test, Apartment(ApartmentState.STA)]
	public async Task StoredShortcutInspectionReadsNonexistentTargetWithoutResolutionOrRepair() {
		var directory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "StartupShortcut-" + Guid.NewGuid().ToString("N"))).FullName;
		using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
		var path = Path.Combine(directory, "Fixture.lnk");
		var nonexistentTarget = Path.Combine(directory, "Absent Target.exe");
		using (var link = new ShellLink()) {
			link.Target = nonexistentTarget;
			link.Arguments = "--stored";
			link.WorkingDirectory = directory;
			link.Save(path);
		}
		var before = File.ReadAllBytes(path);
		var result = await Tools.Windows.Startup.ReadShortcutTargetAsync(path);
		Assert.That(result.Path, Is.EqualTo(nonexistentTarget));
		Assert.That(result.Arguments, Is.EqualTo("--stored"));
		Assert.That(result.WorkingDirectory, Is.EqualTo(directory));
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
		Assert.That(File.Exists(nonexistentTarget), Is.False);
	}
}
