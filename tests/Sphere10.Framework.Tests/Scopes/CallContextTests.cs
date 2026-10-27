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

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CallContextTests {
	[Test]
	public async Task ConcurrentFirstPublicationSharesOneSlotWithoutSharingFlowValues() {
		var key = "CallContextTests:" + Guid.NewGuid().ToString("N");
		var markers = Enumerable.Range(0, 8).Select(_ => new object()).ToArray();
		var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var written = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var writers = 0;
		using var clear = Tools.Scope.ExecuteOnDispose<string>(static name => CallContext.LogicalSetData(name, null), key);
		Assert.That(CallContext.LogicalGetData(key), Is.Null);
		var tasks = markers.Select(marker => Task.Run(async () => {
			await start.Task;
			CallContext.LogicalSetData(key, marker);
			using var clearChild = Tools.Scope.ExecuteOnDispose<string>(static name => CallContext.LogicalSetData(name, null), key);
			if (Interlocked.Increment(ref writers) == markers.Length) written.SetResult();
			await written.Task.WaitAsync(TimeSpan.FromSeconds(10));
			Assert.That(CallContext.LogicalGetData(key), Is.SameAs(marker));
			Assert.That(await Task.Run(() => CallContext.LogicalGetData(key)), Is.SameAs(marker), "The published slot must carry this branch's value into its own descendants.");
		})).ToArray();
		start.SetResult();
		await Task.WhenAll(tasks).WaitAsync(TimeSpan.FromSeconds(10));
		Assert.That(CallContext.LogicalGetData(key), Is.Null, "Child writes must not populate the parent's ambient value.");
		Assert.That(CallContext.LogicalSearchData(key), Is.EqualTo(new[] { key }), "The registry retains one shared name even though its values belong to separate flows.");
	}

	[Test]
	public void SearchSnapshotsKeysAtFirstEnumerationAndAllowsWritesBetweenYields() {
		var prefix = "CallContextTests:" + Guid.NewGuid().ToString("N") + ":";
		var first = prefix + "first";
		var second = prefix + "second";
		using var clear = Tools.Scope.ExecuteOnDispose<string[]>(static names => {
			foreach (var name in names) CallContext.LogicalSetData(name, null);
		}, new[] { first, second });
		var search = CallContext.LogicalSearchData(prefix);
		CallContext.LogicalSetData(first, new object());
		using var iterator = search.GetEnumerator();
		Assert.That(iterator.MoveNext(), Is.True);
		Assert.That(iterator.Current, Is.EqualTo(first));
		// A suspended iterator must not retain a global read lock or include later additions in its snapshot.
		CallContext.LogicalSetData(second, new object());
		Assert.That(iterator.MoveNext(), Is.False);
		Assert.That(CallContext.LogicalSearchData(prefix), Is.EquivalentTo(new[] { first, second }));
	}

	[Test]
	public void SearchReturnsCaseSensitiveKeyNamesIncludingClearedSlots() {
		var prefix = "CallContextTests:" + Guid.NewGuid().ToString("N") + ":";
		var first = prefix + "first";
		var cleared = prefix + "cleared";
		var differentCase = prefix.ToUpperInvariant() + "other";
		using var clear = Tools.Scope.ExecuteOnDispose<string[]>(static names => {
			foreach (var name in names) CallContext.LogicalSetData(name, null);
		}, new[] { first, cleared, differentCase });
		CallContext.LogicalSetData(first, new object());
		CallContext.LogicalSetData(cleared, new object());
		CallContext.LogicalSetData(cleared, null);
		CallContext.LogicalSetData(differentCase, new object());
		Assert.That(CallContext.LogicalSearchData(prefix), Is.EquivalentTo(new[] { first, cleared }));
		Assert.That(CallContext.LogicalGetData(cleared), Is.Null);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NullKeysKeepTheirArgumentException(bool write) {
		if (write)
			Assert.That(() => CallContext.LogicalSetData(null, new object()), Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentException.ParamName)).EqualTo("key"));
		else
			Assert.That(() => CallContext.LogicalGetData(null), Throws.TypeOf<ArgumentNullException>().With.Property(nameof(ArgumentException.ParamName)).EqualTo("key"));
	}

	[Test]
	public void NullSearchPrefixThrowsOnlyWhenEnumerationStarts() {
		var key = "CallContextTests:" + Guid.NewGuid().ToString("N");
		using var clear = Tools.Scope.ExecuteOnDispose<string>(static name => CallContext.LogicalSetData(name, null), key);
		CallContext.LogicalSetData(key, new object());
		var search = CallContext.LogicalSearchData(null);
		using var iterator = search.GetEnumerator();
		Assert.That(() => iterator.MoveNext(), Throws.TypeOf<ArgumentNullException>());
	}
}
