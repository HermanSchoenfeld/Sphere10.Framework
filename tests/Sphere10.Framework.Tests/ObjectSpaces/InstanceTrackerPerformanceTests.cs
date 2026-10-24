// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests.ObjectSpaces;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class InstanceTrackerPerformanceTests {

	[TestCase(false)]
	[TestCase(true)]
	public void LookupsWithoutApplicableProvisionalsAvoidRepeatedCompilation(bool otherTypeProvisional) {
		var tracker = new InstanceTracker();
		tracker.Track(new Record { Id = 1 }, 0);
		if (otherTypeProvisional)
			tracker.TrackNew(new OtherRecord { Id = 2 }, 0);
		Expression<Func<Record, long>> selector = item => item.Id;
		RunMissingProvisionalLookups(tracker, selector, 1);
		var before = GC.GetAllocatedBytesForCurrentThread();
		RunMissingProvisionalLookups(tracker, selector, 1000);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		TestContext.WriteLine($"1,000 provisional misses (other-type provisional={otherTypeProvisional}): {allocated:N0} bytes.");
		if (otherTypeProvisional)
			Assert.That(allocated, Is.LessThan(128_000), "An unrelated provisional must not require compiling the selector on every lookup.");
		else
			Assert.That(allocated, Is.Zero);
	}

	[Test]
	public void CountingWithoutProvisionalsDoesNotAllocatePerCall() {
		var tracker = new InstanceTracker();
		for (var index = 0; index < 16; index++)
			tracker.Track(new Record { Id = index }, index);
		Assert.That(RunProvisionalCounts(tracker, 1), Is.Zero);
		var before = GC.GetAllocatedBytesForCurrentThread();
		var count = RunProvisionalCounts(tracker, 1000);
		var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
		Assert.That(count, Is.Zero);
		Assert.That(allocated, Is.Zero);
		TestContext.WriteLine($"1,000 counts with no provisional objects: {allocated:N0} bytes.");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void CountStillRejectsANullTypeWhenThereAreNoProvisionals(bool trackedType) {
		var tracker = new InstanceTracker();
		if (trackedType)
			tracker.Track(new Record { Id = 1 }, 0);
		Assert.That(() => tracker.GetProvisionalCount(null), Throws.TypeOf<ArgumentNullException>());
	}

	[Test]
	public void ProvisionalLookupRetainsSearchOrderComparerAndCurrentValues() {
		var tracker = new InstanceTracker();
		tracker.Track(new Record { Id = 1, Code = "ALPHA" }, 0);
		var first = new Record { Id = 2, Code = "alpha" };
		var second = new Record { Id = 3, Code = "ALPHA" };
		var other = new OtherRecord { Id = 4 };
		tracker.TrackNew(first, 1);
		tracker.TrackNew(second, 1);
		tracker.TrackNew(other, 0);
		Assert.That(tracker.GetProvisionalCount(typeof(Record)), Is.EqualTo(2));
		Assert.That(tracker.TryFindProvisional((Record item) => item.Code, "AlPhA", StringComparer.OrdinalIgnoreCase, out var found), Is.True);
		Assert.That(found, Is.SameAs(first));

		first.Code = "changed";
		Assert.That(tracker.TryFindProvisional((Record item) => item.Code, "alpha", StringComparer.OrdinalIgnoreCase, out found), Is.True);
		Assert.That(found, Is.SameAs(second));
		Assert.That(tracker.TryFindProvisional((Record item) => item.Code, "CHANGED", StringComparer.OrdinalIgnoreCase, out found), Is.True);
		Assert.That(found, Is.SameAs(first));

		tracker.MarkPersisted(first);
		Assert.That(tracker.TryFindProvisional((Record item) => item.Code, "changed", StringComparer.OrdinalIgnoreCase, out found), Is.False);
		Assert.That(found, Is.Null);
		Assert.That(tracker.GetProvisionalCount(typeof(Record)), Is.EqualTo(1));
		tracker.Untrack(second);
		Assert.That(tracker.GetProvisionalCount(typeof(Record)), Is.Zero);
		Assert.That(tracker.GetProvisionalCount(typeof(OtherRecord)), Is.EqualTo(1));
		tracker.Clear();
		Assert.That(tracker.GetProvisionalCount(typeof(OtherRecord)), Is.Zero);
	}

	[Test]
	public void CapturedSelectorsReadCurrentValuesAndKeepDistinctCaptures() {
		var tracker = new InstanceTracker();
		var provisional = new Record { Id = 3 };
		tracker.TrackNew(provisional, 0);
		var capture = new Capture { Offset = 10 };
		Expression<Func<Record, long>> selector = item => item.Id + capture.Offset;
		Assert.That(tracker.TryFindProvisional(selector, 13L, EqualityComparer<long>.Default, out var found), Is.True);
		Assert.That(found, Is.SameAs(provisional));
		capture.Offset = 20;
		Assert.That(tracker.TryFindProvisional(selector, 13L, EqualityComparer<long>.Default, out _), Is.False);
		Assert.That(tracker.TryFindProvisional(selector, 23L, EqualityComparer<long>.Default, out found), Is.True);
		Assert.That(found, Is.SameAs(provisional));

		var differentCapture = new Capture { Offset = 30 };
		Expression<Func<Record, long>> differentSelector = item => item.Id + differentCapture.Offset;
		Assert.That(tracker.TryFindProvisional(differentSelector, 33L, EqualityComparer<long>.Default, out found), Is.True);
		Assert.That(found, Is.SameAs(provisional));
		provisional.Id = 4;
		Assert.That(tracker.TryFindProvisional(selector, 24L, EqualityComparer<long>.Default, out found), Is.True);
		Assert.That(found, Is.SameAs(provisional));
	}

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public void UnboundSelectorsStillFailCompilationWithoutApplicableProvisionals(bool trackedType, bool otherTypeProvisional) {
		var tracker = new InstanceTracker();
		if (trackedType)
			tracker.Track(new Record { Id = 1 }, 0);
		if (otherTypeProvisional)
			tracker.TrackNew(new OtherRecord { Id = 2 }, 0);
		var selector = CreateUnboundSelector();
		Assert.That(() => tracker.TryFindProvisional(selector, 1L, EqualityComparer<long>.Default, out _), Throws.TypeOf<InvalidOperationException>());
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NullSelectorsRetainTheirFailureWithoutApplicableProvisionals(bool otherTypeProvisional) {
		var tracker = new InstanceTracker();
		if (otherTypeProvisional)
			tracker.TrackNew(new OtherRecord { Id = 2 }, 0);
		Assert.That(() => tracker.TryFindProvisional<Record, long>(null, 1L, EqualityComparer<long>.Default, out _), Throws.TypeOf<NullReferenceException>());
	}

	[Test]
	public void UnboundSelectorFailsBeforeThePersistedMemberIndexCanReturnAnItem() {
		using var memory = new MemoryStream();
		using var space = CreateBuilder().UseMemoryStream(memory).Build();
		space.Save(new Record { Id = 1, Code = "one" });
		Assert.That(() => space.TryGet(CreateUnboundSelector(), 1L, out Record _), Throws.TypeOf<InvalidOperationException>());
	}

	[Test]
	public void MatchingProvisionalExpressionsStillPrecedeMemberIndexValidation() {
		using var memory = new MemoryStream();
		using var space = CreateBuilder().UseMemoryStream(memory).Build();
		var provisional = space.New<Record>();
		provisional.Id = 7;
		provisional.Code = "seven";
		var offset = 3L;
		Expression<Func<Record, long>> selector = item => item.Id + offset;
		Assert.That(space.TryGet(selector, 10L, out var found), Is.True);
		Assert.That(found, Is.SameAs(provisional));
		space.Delete(provisional);
		Assert.That(space.Count<Record>(), Is.Zero);
		Assert.That(() => space.TryGet(selector, 10L, out Record _), Throws.TypeOf<ArgumentException>());
	}

	[Test]
	public void SaveDiscardDeleteAndSlotReusePreserveCountsAndLookupIdentity() {
		using var memory = new MemoryStream();
		using var space = CreateBuilder().UseMemoryStream(memory).Build();
		var first = new Record { Id = 1, Code = "one" };
		Assert.That(space.Save(first), Is.Zero);
		var second = space.New<Record>();
		second.Id = 2;
		second.Code = "two";
		Assert.That(space.Count<Record>(), Is.EqualTo(2));
		Assert.That(space.Get((Record item) => item.Id, 2L), Is.SameAs(second));
		Assert.That(space.Save(second), Is.EqualTo(1));
		Assert.That(space.Count<Record>(), Is.EqualTo(2));

		var discarded = space.New<Record>();
		discarded.Id = 3;
		discarded.Code = "three";
		Assert.That(space.Count<Record>(), Is.EqualTo(3));
		space.Delete(discarded);
		Assert.That(space.Count<Record>(), Is.EqualTo(2));
		Assert.That(space.TryGet((Record item) => item.Id, 3L, out _), Is.False);

		space.Delete(second);
		Assert.That(space.Count<Record>(), Is.EqualTo(1));
		var replacement = space.New<Record>();
		replacement.Id = 4;
		replacement.Code = "four";
		Assert.That(space.Count<Record>(), Is.EqualTo(2));
		Assert.That(space.GetAll<Record>(), Is.EqualTo(new[] { first }));
		Assert.That(space.Save(replacement), Is.EqualTo(1));
		Assert.That(space.Count<Record>(), Is.EqualTo(2));
		Assert.That(space.Get<Record>(0), Is.SameAs(first));
		Assert.That(space.Get<Record>(1), Is.SameAs(replacement));
		Assert.That(space.Get((Record item) => item.Id, 4L), Is.SameAs(replacement));
		Assert.That(space.GetAll<Record>(), Is.EqualTo(new[] { first, replacement }));
	}

	[Test]
	public void AutosaveFlushPersistsProvisionalsAndLaterIndexChanges() {
		using var memory = new MemoryStream();
		using var space = CreateBuilder().UseMemoryStream(memory).AutoSave().Build();
		var provisional = space.New<Record>();
		provisional.Id = 1;
		provisional.Code = "before";
		Assert.That(space.Get((Record item) => item.Code, "before"), Is.SameAs(provisional));
		Assert.That(space.GetAll<Record>(), Is.Empty);
		space.Flush();
		Assert.That(provisional.Dirty, Is.False);
		Assert.That(space.Count<Record>(), Is.EqualTo(1));
		Assert.That(space.GetAll<Record>().Single(), Is.SameAs(provisional));
		provisional.Code = "after";
		provisional.Dirty = true;
		space.Flush();
		Assert.That(space.TryGet((Record item) => item.Code, "before", out _), Is.False);
		Assert.That(space.Get((Record item) => item.Code, "after"), Is.SameAs(provisional));
	}

	[Test]
	public void RollbackDropsProvisionalStateAndRebuildsIdentity() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var pagesFolder = Path.Combine(folder, "pages");
		Directory.CreateDirectory(pagesFolder);
		var builder = CreateBuilder().UseFile(Path.Combine(folder, "instances.dat")).KeepUncommittedPagesAt(pagesFolder);
		using (var space = (FileObjectSpace)builder.Build()) {
			var original = new Record { Id = 1, Code = "one" };
			space.Save(original);
			space.Commit();
			var provisional = space.New<Record>();
			provisional.Id = 2;
			provisional.Code = "two";
			original.Code = "pending";
			space.Save(original);
			Assert.That(space.Count<Record>(), Is.EqualTo(2));
			space.Rollback();
			Assert.That(space.Count<Record>(), Is.EqualTo(1));
			Assert.That(space.TryGet((Record item) => item.Id, 2L, out _), Is.False);
			var restored = space.Get((Record item) => item.Id, 1L);
			Assert.That(restored, Is.Not.SameAs(original));
			Assert.That(restored.Code, Is.EqualTo("one"));
			var next = space.New<Record>();
			next.Id = 3;
			next.Code = "three";
			Assert.That(space.Save(next), Is.EqualTo(1));
			Assert.That(space.Count<Record>(), Is.EqualTo(2));
			space.Commit();
		}
		using var reopened = (FileObjectSpace)builder.Build();
		Assert.That(reopened.Count<Record>(), Is.EqualTo(2));
		Assert.That(reopened.Get((Record item) => item.Id, 1L).Code, Is.EqualTo("one"));
		Assert.That(reopened.Get((Record item) => item.Id, 3L).Code, Is.EqualTo("three"));
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void RunMissingProvisionalLookups(InstanceTracker tracker, Expression<Func<Record, long>> selector, int count) {
		for (var index = 0; index < count; index++)
			if (tracker.TryFindProvisional(selector, -1L, EqualityComparer<long>.Default, out var item) || item is not null)
				throw new InvalidOperationException("A persisted or unrelated object was returned as provisional.");
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static int RunProvisionalCounts(InstanceTracker tracker, int count) {
		var total = 0;
		for (var index = 0; index < count; index++)
			total += tracker.GetProvisionalCount(typeof(Record));
		return total;
	}

	private static Expression<Func<Record, long>> CreateUnboundSelector() {
		var bound = Expression.Parameter(typeof(Record), "bound");
		var unbound = Expression.Parameter(typeof(Record), "unbound");
		return Expression.Lambda<Func<Record, long>>(Expression.Property(unbound, nameof(Record.Id)), bound);
	}

	private static ObjectSpaceBuilder CreateBuilder() => new ObjectSpaceBuilder()
		.WithIndexBTreeOrder(8)
		.AutoLoad()
		.AddDimension<Record>(true)
			.WithIdentifier(item => item.Id)
			.WithUniqueIndexOn(item => item.Code)
			.WithChangeTrackingVia(item => item.Dirty)
			.Done()
		.AddDimension<OtherRecord>(true)
			.WithIdentifier(item => item.Id)
			.WithChangeTrackingVia(item => item.Dirty)
			.Done();

	public sealed class Record {
		public long Id { get; set; }
		public string Code { get; set; }
		[Transient]
		public bool Dirty { get; set; }
	}

	public sealed class OtherRecord {
		public long Id { get; set; }
		[Transient]
		public bool Dirty { get; set; }
	}

	private sealed class Capture {
		public long Offset { get; set; }
	}
}
