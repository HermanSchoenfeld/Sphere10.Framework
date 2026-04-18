// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;
namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class MemoryMetricTests {


	[Test]
	public void Byte2Bit() {
		Assert.That(Tools.Memory.ConvertMemoryMetric(2, MemoryMetric.Byte, MemoryMetric.Bit), Is.EqualTo(16));
	}

	[Test]
	public void Bit2Byte() {
		Assert.That(Tools.Memory.ConvertMemoryMetric(16, MemoryMetric.Bit, MemoryMetric.Byte), Is.EqualTo(2));
	}

	[Test]
	public void Kilobyte2Byte() {
		Assert.That(Tools.Memory.ConvertMemoryMetric(1, MemoryMetric.Kilobyte, MemoryMetric.Byte), Is.EqualTo(1000));
	}

	[Test]
	public void Byte2Kilobyte() {
		Assert.That(Tools.Memory.ConvertMemoryMetric(1000, MemoryMetric.Byte, MemoryMetric.Kilobyte), Is.EqualTo(1));
	}

	[Test]
	public void KilobyteToMegabyte() {
		Assert.That(Tools.Memory.ConvertMemoryMetric(1000, MemoryMetric.Kilobyte, MemoryMetric.Megabyte), Is.EqualTo(1));
	}

	[Test]
	public void KilobyteToMegabit() {
		Assert.That(Tools.Memory.ConvertMemoryMetric(1000, MemoryMetric.Kilobyte, MemoryMetric.Megabit), Is.EqualTo(1 * 8));
	}
}

