// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Windows.Storage;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class WindowsDriveHardwareApiTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task CancelledPhysicalQueriesDoNotStartWmi(bool partitions) {
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		if (partitions)
			await Assert.ThatAsync(async () => await Tools.Windows.Drives.GetPartitionsAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		else
			await Assert.ThatAsync(async () => await Tools.Windows.Drives.GetPhysicalDisksAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void InvalidQueryTimeoutIsRejectedBeforeNativeIo(bool partitions) {
		if (partitions)
			Assert.Throws<ArgumentException>(() => Tools.Windows.Drives.GetPartitions(timeout: TimeSpan.Zero));
		else
			Assert.Throws<ArgumentException>(() => Tools.Windows.Drives.GetPhysicalDisks(timeout: TimeSpan.Zero));
	}

	[Test]
	public void ReadonlyPhysicalQueriesPreserveNativeDiskAndPartitionIdentities() {
		var disks = Tools.Windows.Drives.GetPhysicalDisks();
		var partitions = Tools.Windows.Drives.GetPartitions();
		Assert.That(disks, Is.Not.Empty);
		foreach (var disk in disks.Where(row => row.ReadError == null)) {
			Assert.That(disk.DeviceId, Is.Not.Empty);
			Assert.That(disk.Index, Is.Not.Null);
		}
		foreach (var partition in partitions.Where(row => row.ReadError == null)) {
			Assert.That(partition.DeviceId, Is.Not.Empty);
			Assert.That(partition.DiskIndex, Is.Not.Null);
			Assert.That(partition.Index, Is.Not.Null);
			Assert.That(partition.StartingOffsetBytes, Is.Not.Null);
		}
		Assert.That(disks.Select(row => row.DeviceId).Where(value => value != null).Distinct().Count(),
			Is.EqualTo(disks.Count(row => row.DeviceId != null)), "Physical identities must remain intact rather than become formatted labels.");
	}
}

