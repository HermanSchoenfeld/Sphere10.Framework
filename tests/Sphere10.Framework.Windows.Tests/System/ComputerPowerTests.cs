// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ComputerPowerTests {
	[Test]
	public async Task AsyncPowerAdaptersCallTheirVirtualPrimitiveOnlyAfterAdmission() {
		var computer = new RecordingComputer();
		Assert.That(await computer.GetPowerCapabilitiesAsync(), Is.EqualTo(new WindowsPowerCapabilities(true, true, true)));
		await computer.ShutdownAsync(0x80000000, true);
		Assert.That(computer.LastRequest, Is.EqualTo("shutdown:2147483648:True"));
		await computer.RestartAsync(0x80000000);
		Assert.That(computer.LastRequest, Is.EqualTo("restart:2147483648:False"));
		await computer.LogoffAsync();
		Assert.That(computer.LastRequest, Is.EqualTo("logoff:False"));
		await computer.LockAsync();
		Assert.That(computer.LastRequest, Is.EqualTo("lock"));
		await computer.SleepAsync(true);
		Assert.That(computer.LastRequest, Is.EqualTo("sleep:True"));
		await computer.HibernateAsync();
		Assert.That(computer.LastRequest, Is.EqualTo("hibernate:False"));
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		await Assert.ThatAsync(() => computer.ShutdownAsync(0x80000000, cancellationToken: canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => computer.LockAsync(canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(computer.LastRequest, Is.EqualTo("hibernate:False"), "A canceled queued request must not invoke a power action.");
	}

	[Test]
	public void InvalidShutdownReasonsFailBeforeAnyPrivilegeOrNativeAction() {
		var computer = new ComputerUtil();
		Assert.That(() => computer.Shutdown(0), Throws.ArgumentException);
		Assert.That(() => computer.Restart(0), Throws.ArgumentException);
	}

	private sealed class RecordingComputer : ComputerUtil {
		public string LastRequest { get; private set; }
		public override WindowsPowerCapabilities GetPowerCapabilities() => new(true, true, true);
		public override void Shutdown(uint reason, bool forceIfHung = false) => LastRequest = $"shutdown:{reason}:{forceIfHung}";
		public override void Restart(uint reason, bool forceIfHung = false) => LastRequest = $"restart:{reason}:{forceIfHung}";
		public override void Logoff(bool forceIfHung = false) => LastRequest = $"logoff:{forceIfHung}";
		public override void Lock() => LastRequest = "lock";
		public override void Sleep(bool disableWakeEvents = false) => LastRequest = $"sleep:{disableWakeEvents}";
		public override void Hibernate(bool disableWakeEvents = false) => LastRequest = $"hibernate:{disableWakeEvents}";
	}
}

