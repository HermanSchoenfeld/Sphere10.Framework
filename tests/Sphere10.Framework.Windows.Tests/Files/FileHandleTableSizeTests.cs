// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;
using Sphere10.Framework.Windows.Files;

namespace Sphere10.Framework.Windows.Tests;

/// <summary>How the buffer for the system handle table grows. One leaking process can hold millions of handles (1.7 million were seen
/// in one process, a table of 77 MB), so the table is read up to a stated limit rather than refused at 64 MB.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public sealed class FileHandleTableSizeTests {
	private const int Megabyte = 1024 * 1024;

	[Test]
	public void TheNextBufferIsAQuarterLargerThanWindowsSaidItNeeded() {
		var needed = (uint)(77.2 * Megabyte);
		Assert.That(FileHandleService.NextHandleTableSize(Megabyte, needed), Is.EqualTo((int)(needed + needed / 4 + 65536)),
			"The table can grow between the two reads.");
	}

	[Test]
	public void ATableLargerThanTheFormerLimitIsRead()
		=> Assert.That(FileHandleService.NextHandleTableSize(64 * Megabyte, 80 * Megabyte), Is.GreaterThan(80 * Megabyte));

	[Test]
	public void WithoutAReportedSizeTheBufferDoubles() {
		Assert.That(FileHandleService.NextHandleTableSize(Megabyte, 0), Is.EqualTo(2 * Megabyte));
		Assert.That(FileHandleService.NextHandleTableSize(8 * Megabyte, Megabyte), Is.EqualTo(16 * Megabyte), "A stale smaller size never shrinks the buffer.");
	}

	[Test]
	public void TheBufferStopsAtTheLimitAndARefusalThereIsReported() {
		Assert.That(FileHandleService.NextHandleTableSize(400 * Megabyte, 500 * Megabyte), Is.EqualTo(FileHandleService.MaximumHandleTableBytes));
		Assert.That(FileHandleService.NextHandleTableSize(300 * Megabyte, uint.MaxValue), Is.EqualTo(FileHandleService.MaximumHandleTableBytes));
		Assert.That(() => FileHandleService.NextHandleTableSize(FileHandleService.MaximumHandleTableBytes, 0),
			Throws.InvalidOperationException.With.Message.Contains("exceeds the supported capture size"));
	}
}
