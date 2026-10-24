// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.BTrees;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[NonParallelizable]
public class BTreeNodeIoPerformanceTests {
	[Test]
	[Explicit("Serial allocation diagnostic; compare the same fixture against the before and after Framework binaries.")]
	[Category("Performance")]
	public void StreamedNodeMutationSamples([Values] BTreeType type, [Values(4, 64, 256)] int order, [Values] Endianness endianness) {
		const int recordCount = 512;
		var keys = Enumerable.Range(0, recordCount).Select(index => index * 73 % recordCount).ToArray();
		var samples = new List<object>();
		for (var sample = -1; sample < 5; sample++) {
			using var stream = new MemoryStream(2 * 1024 * 1024);
			BTreeBase<int, long> tree = type == BTreeType.BTree
				? new StreamMappedBTree<int, long>(order, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<long>.Instance, endianness: endianness)
				: new StreamMappedBTreePlus<int, long>(order, stream, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<long>.Instance, endianness: endianness);
			using var lifetime = (IDisposable)tree;

			Capture("Insert", () => {
				foreach (var key in keys)
					tree.Add(key, key * 10L);
			}, 0);
			Capture("Update", () => {
				foreach (var key in keys)
					tree[key] = key * 10L + 1;
			}, 1);
			Capture("DeleteAndRecycle", () => {
				foreach (var key in keys)
					if (!tree.Remove(key))
						throw new InvalidOperationException("An existing key could not be removed.");
				foreach (var key in keys)
					tree.Add(key, key * 10L + 2);
			}, 2);

			void Capture(string workload, Action action, int valueOffset) {
				var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
				var started = Stopwatch.GetTimestamp();
				action();
				var elapsedTicks = Stopwatch.GetTimestamp() - started;
				var allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
				Assert.That(tree.Validate(out var error), Is.True, error);
				Assert.That(tree.ToArray(), Is.EqualTo(Enumerable.Range(0, recordCount).Select(key => new KeyValuePair<int, long>(key, key * 10L + valueOffset)).ToArray()));
				using var snapshot = new MemoryStream(stream.ToArray());
				BTreeBase<int, long> reopened = type == BTreeType.BTree
					? new StreamMappedBTree<int, long>(order, snapshot, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<long>.Instance, endianness: endianness)
					: new StreamMappedBTreePlus<int, long>(order, snapshot, PrimitiveSerializer<int>.Instance, PrimitiveSerializer<long>.Instance, endianness: endianness);
				using var reopenedLifetime = (IDisposable)reopened;
				Assert.That(reopened.ToArray(), Is.EqualTo(tree.ToArray()));
				if (sample >= 0)
					samples.Add(new {
						Sample = sample, Workload = workload, AllocatedBytes = allocatedBytes, ElapsedTicks = elapsedTicks,
						StreamBytes = stream.Length, StorageSha256 = Convert.ToHexString(Hashers.Hash(CHF.SHA2_256, stream.GetBuffer().AsSpan(0, (int)stream.Length)))
					});
			}
		}
		var directory = Path.GetFullPath(TestContext.Parameters.Get("BTreeNodeIoArtifactRoot", Path.Combine(TestContext.CurrentContext.WorkDirectory, "BTreeNodeIo")));
		Directory.CreateDirectory(directory);
		var path = Path.Combine(directory, $"{type}-{order}-{endianness}.json");
		File.WriteAllText(path, JsonSerializer.Serialize(new {
			Type = type, Order = order, Endianness = endianness, RecordCount = recordCount,
			StopwatchFrequency = Stopwatch.Frequency, Runtime = Environment.Version.ToString(), Measurements = samples,
			Conditions = "One unrecorded warmup and five recorded samples; preallocated MemoryStream, construction, verification, reopen and hashing outside measurement. Thread allocations and screening timings, not BenchmarkDotNet confirmation."
		}, BTreeProfilerSettings.JsonOptions));
		TestContext.AddTestAttachment(path);
		TestContext.Out.WriteLine(path);
	}
}
