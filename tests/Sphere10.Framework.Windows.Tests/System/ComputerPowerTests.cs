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
	public async Task CanceledPowerRequestIsNotAdmitted() {
		// Reason zero is rejected by the synchronous primitive, so nothing can reach Windows even if admission were to change
		using var canceled = new CancellationTokenSource();
		canceled.Cancel();
		await Assert.ThatAsync(() => Tools.Windows.Computer.ShutdownAsync(0, cancellationToken: canceled.Token), Throws.InstanceOf<OperationCanceledException>());
		await Assert.ThatAsync(() => Tools.Windows.Computer.RestartAsync(0, cancellationToken: canceled.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[Test]
	public void InvalidShutdownReasonsFailBeforeAnyPrivilegeOrNativeAction() {
		Assert.That(() => Tools.Windows.Computer.Shutdown(0), Throws.ArgumentException);
		Assert.That(() => Tools.Windows.Computer.Restart(0), Throws.ArgumentException);
	}
}
