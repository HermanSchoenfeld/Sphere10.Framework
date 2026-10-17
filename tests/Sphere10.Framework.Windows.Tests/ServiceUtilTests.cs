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
	public void ExistingFacadeQueriesUseTheNativeUtilityAndReturnOwnedControllers() {
		Assert.That(Tools.Windows.Services, Is.TypeOf<ServiceUtil>());
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
		await Assert.ThatAsync(async () => await new ServiceUtil().WaitForStatusAsync("EventLog", desired, TimeSpan.FromSeconds(5), cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
		controller.Refresh();
		Assert.That(controller.Status, Is.EqualTo(before));
	}

	[Test]
	public async Task ObservingAnUnreachedStateHonorsTheSuppliedTimeout() {
		using var controller = new ServiceController("EventLog");
		var desired = controller.Status == ServiceControllerStatus.Stopped ? ServiceControllerStatus.Running : ServiceControllerStatus.Stopped;
		await Assert.ThatAsync(async () => await new ServiceUtil().WaitForStatusAsync("EventLog", desired, TimeSpan.FromMilliseconds(50)),
			Throws.TypeOf<System.ServiceProcess.TimeoutException>());
	}
	[Test]
	public async Task ARealServiceQueryReturnsNativeConfigurationStateAndDescriptionWithoutChangingIt() {
		var scm = new ServiceUtil();
		var config = await scm.ReadConfigurationAsync("EventLog");
		var info = await scm.ReadServiceAsync("EventLog");
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
		var info = await new ServiceUtil().ReadServiceAsync("Sphere10-Nonexistent-Service-" + Guid.NewGuid().ToString("N"));
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
		var scm = new ServiceUtil();
		var name = "Sphere10-Nonexistent-Test-Service";
		Task task = operation switch {
			"Enumerate" => scm.GetServicesAsync(cancellation.Token),
			"Drivers" => scm.GetDriversAsync(cancellation.Token),
			"Read" => scm.ReadServiceAsync(name, cancellation.Token),
			"Config" => scm.ReadConfigurationAsync(name, cancellation.Token),
			"Start" => scm.StartServiceAsync(name, TimeSpan.FromSeconds(1), cancellation.Token),
			"Stop" => scm.StopServiceAsync(name, TimeSpan.FromSeconds(1), cancellation.Token),
			"Pause" => scm.PauseServiceAsync(name, TimeSpan.FromSeconds(1), cancellation.Token),
			"Resume" => scm.ResumeServiceAsync(name, TimeSpan.FromSeconds(1), cancellation.Token),
			_ => scm.SetStartModeAsync(name, ServiceStartMode.Manual, cancellation.Token)
		};
		await Assert.ThatAsync(async () => await task, Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public async Task NativeDriverEnumerationReturnsOnlyDriverServiceTypes() {
		var rows = await new ServiceUtil().GetDriversAsync();
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

