// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class StreamDisposalGuardTests {

	[TestCase(false, 0)]
	[TestCase(false, 1)]
	[TestCase(true, 0)]
	[TestCase(true, 1)]
	public void ClosedBlockingStreamRejectsReadsAndWritesIncludingEmptyRequests(bool write, int count) {
		using var stream = new BlockingStream();
		var buffer = new byte[1];
		stream.Close();
		var expected = Throws.TypeOf<ObjectDisposedException>().With.Property(nameof(ObjectDisposedException.ObjectName)).EqualTo(typeof(BlockingStream).FullName);
		if (write)
			Assert.That(() => stream.Write(buffer, 0, count), expected);
		else
			Assert.That(() => stream.Read(buffer, 0, count), expected);
	}

	[Test]
	public void ClosedBlockingStreamRejectsEndOfStream() {
		using var stream = new BlockingStream();
		stream.Close();
		Assert.That(stream.SetEndOfStream, Throws.TypeOf<ObjectDisposedException>());
	}

	[Test]
	public void DisposedPipelineRejectsRunningBeforeInspectingItsInputs() {
		using var pipeline = new StreamPipeline(static (input, output) => input.CopyTo(output));
		pipeline.Dispose();
		Assert.That(() => pipeline.Run(null, null), Throws.TypeOf<ObjectDisposedException>().With.Property(nameof(ObjectDisposedException.ObjectName)).EqualTo(typeof(StreamPipeline).FullName));
	}
}
