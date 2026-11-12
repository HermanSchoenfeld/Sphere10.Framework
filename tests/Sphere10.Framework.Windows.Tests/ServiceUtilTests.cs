// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Services;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class ServiceUtilTests {
	[Test]
	public async Task OptionalConfigurationReadsPreserveNativeRecoveryAndPrivilegeDataWithoutChangingServiceState() {
		using var service = new ServiceController("EventLog");
		var before = service.Status;
		var configuration = await Tools.Windows.Services.ReadAdvancedConfigurationAsync(service.ServiceName);
		Assert.That(configuration.Name, Is.EqualTo(service.ServiceName));
		Assert.That(configuration.FailureResetPeriodSeconds, Is.Not.Null);
		Assert.That(configuration.DelayedAutoStart, Is.Not.Null);
		Assert.That(configuration.ServiceSidType, Is.Not.Null);
		Assert.That(configuration.RequiredPrivileges, Does.Contain("SeChangeNotifyPrivilege"));
		Assert.That(configuration.Issues.Select(issue => issue.InformationLevel).Distinct().Count(), Is.EqualTo(configuration.Issues.Length));
		Assert.That(configuration.Triggers.SelectMany(trigger => trigger.DataItems).All(item => item.Data.Length <= 1024), Is.True);
		service.Refresh();
		Assert.That(service.Status, Is.EqualTo(before));
	}

	[Test]
	public async Task MissingAndCancelledOptionalConfigurationReadsAreNotSuccessfulEmptyConfiguration() {
		await Assert.ThatAsync(async () => await Tools.Windows.Services.ReadAdvancedConfigurationAsync("Sphere10-Missing-" + Guid.NewGuid().ToString("N")),
			Throws.TypeOf<System.ComponentModel.Win32Exception>().With.Property("NativeErrorCode").EqualTo(1060));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await Tools.Windows.Services.ReadAdvancedConfigurationAsync("EventLog", cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
	}

	[TestCase("\"C:\\Windows\\System32\\svchost.exe\" -k LocalService", "C:\\Windows\\System32\\svchost.exe")]
	[TestCase("C:\\Windows\\System32\\svchost.exe -k LocalService", "C:\\Windows\\System32\\svchost.exe")]
	public void ServiceImageResolutionUsesTheExistingCommandParserAndDoesNotIncludeArguments(string command, string expected)
		=> Assert.That(Tools.Windows.Services.ResolveExecutablePath(command), Is.EqualTo(expected));

	[TestCase("svchost.exe -k LocalService")]
	[TestCase("\"C:\\Windows\\System32\\svchost.exe -k LocalService")]
	public void ServiceImageResolutionRejectsImplicitSearchAndMalformedQuotedRegistrations(string command)
		=> Assert.That(() => Tools.Windows.Services.ResolveExecutablePath(command), Throws.InstanceOf<ArgumentException>());

	[Test]
	public void ExistingFacadeQueriesUseTheNativeUtilityAndReturnOwnedControllers() {
		Assert.That(Tools.Windows.Services.IsServiceInstalled("eventlog"), Is.True);
		Assert.That(Tools.Windows.Services.TryGetServiceController("EventLog", out var controller), Is.True);
		using (controller)
			Assert.That(controller.ServiceName, Is.EqualTo("EventLog").IgnoreCase);
		Assert.That(typeof(ServiceControlManagerSentry).GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.DeclaredOnly)
			.Select(method => method.Name), Is.EquivalentTo(new[] { "CreateServiceAsync", "RemoveServiceAsync" }));
	}

	[Test]
	public async Task ObservingAnUnreachedStateCanBeCancelledWithoutSendingAServiceControl() {
		using var controller = new ServiceController("EventLog");
		var before = controller.Status;
		var desired = before == ServiceControllerStatus.Stopped ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped;
		using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
		await Assert.ThatAsync(async () => await Tools.Windows.Services.WaitForStatusAsync("EventLog", desired, TimeSpan.FromSeconds(5), cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
		controller.Refresh();
		Assert.That(controller.Status, Is.EqualTo(before));
	}

	[Test]
	public async Task ObservingAnUnreachedStateHonorsTheSuppliedTimeout() {
		using var controller = new ServiceController("EventLog");
		var desired = controller.Status == ServiceControllerStatus.Stopped ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped;
		await Assert.ThatAsync(async () => await Tools.Windows.Services.WaitForStatusAsync("EventLog", desired, TimeSpan.FromMilliseconds(50)),
			Throws.TypeOf<System.ServiceProcess.TimeoutException>());
	}
	[Test]
	public async Task ARealServiceQueryReturnsNativeConfigurationStateAndDescriptionWithoutChangingIt() {
		var config = await Tools.Windows.Services.ReadConfigurationAsync("EventLog");
		var info = await Tools.Windows.Services.ReadServiceAsync("EventLog");
		Assert.That(config.Name, Is.EqualTo("EventLog"));
		Assert.That(config.BinaryPath, Is.Not.Empty);
		Assert.That(info.ConfigurationErrorCode, Is.Zero);
		Assert.That(info.StatusErrorCode, Is.Zero);
		Assert.That(info.StartMode, Is.EqualTo(config.StartMode));
		Assert.That(info.ServiceType, Is.EqualTo(config.ServiceType));
		Assert.That(info.Status, Is.Not.Null);
		Assert.That(info.ProcessId, Is.Not.Null);
	}

	[Test]
	public async Task MissingServiceQueriesKeepNativeErrorsInsteadOfInventingState() {
		var info = await Tools.Windows.Services.ReadServiceAsync("Sphere10-Nonexistent-Service-" + Guid.NewGuid().ToString("N"));
		Assert.That(info.Status, Is.Null);
		Assert.That(info.StartMode, Is.Null);
		Assert.That(info.CanStop, Is.Null);
		Assert.That(info.ConfigurationErrorCode, Is.EqualTo(1060));
		Assert.That(info.StatusErrorCode, Is.EqualTo(1060));
	}

	[TestCase("Enumerate")]
	[TestCase("Drivers")]
	[TestCase("Read")]
	[TestCase("Config")]
	[TestCase("Start")]
	[TestCase("Stop")]
	[TestCase("Pause")]
	[TestCase("Resume")]
	[TestCase("Startup")]
	public async Task CancelledNativeOperationsNeverDispatchAControlOrConfigurationChange(string operation) {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		var name = "Sphere10-Nonexistent-Test-Service";
		Task task = operation switch {
			"Enumerate" => Tools.Windows.Services.GetServicesAsync(cancellation.Token),
			"Drivers" => Tools.Windows.Services.GetDriversAsync(cancellation.Token),
			"Read" => Tools.Windows.Services.ReadServiceAsync(name, cancellation.Token),
			"Config" => Tools.Windows.Services.ReadConfigurationAsync(name, cancellation.Token),
			"Start" => Tools.Windows.Services.StartServiceAsync(name, TimeSpan.FromSeconds(1), cancellation.Token),
			"Stop" => Tools.Windows.Services.StopServiceAsync(name, TimeSpan.FromSeconds(1), cancellation.Token),
			"Pause" => Tools.Windows.Services.PauseServiceAsync(name, TimeSpan.FromSeconds(1), cancellation.Token),
			"Resume" => Tools.Windows.Services.ResumeServiceAsync(name, TimeSpan.FromSeconds(1), cancellation.Token),
			_ => Tools.Windows.Services.SetStartModeAsync(name, ServiceStartMode.Manual, cancellation.Token)
		};
		await Assert.ThatAsync(async () => await task, Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task NativeDriverEnumerationReturnsOnlyDriverServiceTypes() {
		var rows = await Tools.Windows.Services.GetDriversAsync();
		Assert.That(rows, Is.Not.Empty);
		Assert.That(rows.Where(row => row.ConfigurationErrorCode == 0).All(row => (row.ServiceType & (ServiceType.KernelDriver | ServiceType.FileSystemDriver | ServiceType.RecognizerDriver)) != 0), Is.True);
	}

	[Test]
	public async Task CancelledDriverPackageQueryDoesNotStartWmiEnumeration() {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		await Assert.ThatAsync(async () => await new DriverManager().GetDriverPackagesAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}
}

