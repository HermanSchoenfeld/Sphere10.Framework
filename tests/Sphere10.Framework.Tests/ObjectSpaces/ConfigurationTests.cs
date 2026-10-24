// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests.ObjectSpaces;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ConfigurationTests {
	[Test]
	public void FreshIndexOrderUsesObjectSpaceDefaults() {
		Assert.That(new ObjectSpaceDefinition().IndexBTreeOrder, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder));
		using var space = CreateBuilder().UseMemoryStream().Build();
		AssertIndexOrders(space, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder);
	}

	[TestCase(-1)]
	[TestCase(0)]
	[TestCase(2)]
	public void InvalidIndexOrderIsRejectedBeforeMutation(int order) {
		var builder = CreateBuilder().WithIndexBTreeOrder(8);
		Assert.That(() => builder.WithIndexBTreeOrder(order), Throws.InstanceOf<ArgumentException>());
		Assert.That(builder.BuildDefinition().IndexBTreeOrder, Is.EqualTo(8));
		var definition = builder.BuildDefinition();
		Assert.That(() => definition.IndexBTreeOrder = order, Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(definition.IndexBTreeOrder, Is.EqualTo(8));
		Assert.That(definition.IndexBTreeSpec, Is.Null);

		var spec = new BTreeSpec(BTreeType.BTreePlus, 8);
		definition.IndexBTreeSpec = spec;
		Assert.That(() => definition.IndexBTreeOrder = order, Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(definition.IndexBTreeOrder, Is.EqualTo(8));
		Assert.That(definition.IndexBTreeSpec, Is.SameAs(spec));
	}

	[TestCase(3, false)]
	[TestCase(8, false)]
	[TestCase(64, false)]
	[TestCase(128, false)]
	[TestCase(3, true)]
	[TestCase(8, true)]
	[TestCase(64, true)]
	[TestCase(128, true)]
	public void MemberIndexesRetainConfiguredOrderAndDataAfterReopen(int order, bool fileBacked) {
		// Isolate each storage configuration and force splits in all four member-index paths.
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "space.dat");
		using var memory = new MemoryStream();
		var builder = CreateBuilder().WithIndexBTreeOrder(order);
		if (fileBacked)
			builder.UseFile(path).AutoLoad();
		else
			builder.UseMemoryStream(memory);
		using (var space = builder.Build()) {
			for (var offset = 0; offset < 300; offset++) {
				var id = (offset * 137) % 300;
				space.Save(new Record { Id = id, Code = $"record-{id}", Group = id % 5, Label = $"label-{id % 7}" });
			}
			AssertIndexOrders(space, order);
			Assert.That(space.Get((Record record) => record.Id, 173).Code, Is.EqualTo("record-173"));
			Assert.That(space.Get((Record record) => record.Code, "record-173").Id, Is.EqualTo(173));
			Assert.That(GetGroup(space, 3), Has.Length.EqualTo(60));
			Assert.That(GetLabel(space, "label-3"), Has.Length.EqualTo(43));
			var duplicate = new Record { Id = 173, Code = "duplicate", Label = "duplicate" };
			Assert.That(() => space.Save(duplicate), Throws.InvalidOperationException);
			space.Delete(duplicate);
			CompleteWrites(space);
		}

		// Reopen the same schema/order and validate both fixed and checksummed index membership.
		using var reopened = builder.Build();
		AssertIndexOrders(reopened, order);
		Assert.That(reopened.GetAll<Record>().Count(), Is.EqualTo(300));
		Assert.That(reopened.Get((Record record) => record.Id, 173).Code, Is.EqualTo("record-173"));
		Assert.That(reopened.Get((Record record) => record.Code, "record-173").Id, Is.EqualTo(173));
		Assert.That(GetGroup(reopened, 3).Select(record => record.Id), Is.EquivalentTo(Enumerable.Range(0, 300).Where(id => id % 5 == 3)));
		Assert.That(GetLabel(reopened, "label-3").Select(record => record.Id), Is.EquivalentTo(Enumerable.Range(0, 300).Where(id => id % 7 == 3)));
	}

	[Test]
	public void UpdatingAnIndexedObjectReplacesOldMembershipAndPreservesOtherEdits() {
		using var space = CreateBuilder().UseMemoryStream().WithIndexBTreeOrder(8).Build();
		var record = new Record { Id = 1, Code = "one", Group = 2, Label = "old", Description = "before" };
		space.Save(record);
		record.Group = 3;
		record.Label = "new";
		space.Save(record);
		Assert.That(GetGroup(space, 2), Is.Empty);
		Assert.That(GetGroup(space, 3), Is.EqualTo(new[] { record }));
		Assert.That(GetLabel(space, "old"), Is.Empty);
		Assert.That(GetLabel(space, "new"), Is.EqualTo(new[] { record }));

		// Saving a non-indexed change must preserve every index entry.
		record.Description = "after";
		space.Save(record);
		Assert.That(GetGroup(space, 3), Is.EqualTo(new[] { record }));
		Assert.That(GetLabel(space, "new"), Is.EqualTo(new[] { record }));
		Assert.That(space.Get((Record item) => item.Id, 1).Description, Is.EqualTo("after"));
	}

	[Test]
	public void GetAllReturnsTrackedSnapshotAndSkipsRecycledSlots() {
		using var space = CreateBuilder().UseMemoryStream().Build();
		var first = new Record { Id = 1, Code = "one", Label = "group" };
		var second = new Record { Id = 2, Code = "two", Label = "group" };
		var third = new Record { Id = 3, Code = "three", Label = "group" };
		space.Save(first);
		space.Save(second);
		space.Save(third);
		var snapshot = space.GetAll<Record>();
		space.Delete(first);
		Assert.That(snapshot, Is.EqualTo(new[] { first, second, third }));
		Assert.That(space.GetAll<Record>(), Is.EqualTo(new[] { second, third }));
		Assert.That(space.GetAll<Record>().First(), Is.SameAs(space.Get<Record>(1)));

		// Reusing a recycled slot includes its new occupant in physical order.
		var replacement = new Record { Id = 4, Code = "four", Label = "group" };
		Assert.That(space.Save(replacement), Is.EqualTo(0));
		Assert.That(space.GetAll<Record>(), Is.EqualTo(new[] { replacement, second, third }));
	}

	[Test]
	public void GetAllExcludesProvisionalObjectsUntilAutosaveFlush() {
		using var space = CreateBuilder().UseMemoryStream().AutoSave().Build();
		var provisional = space.New<Record>();
		provisional.Id = 1;
		provisional.Code = "one";
		provisional.Label = "group";
		Assert.That(space.Count<Record>(), Is.EqualTo(1));
		Assert.That(space.GetAll<Record>(), Is.Empty);
		space.Flush();
		Assert.That(space.GetAll<Record>().Single(), Is.SameAs(provisional));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void CreatingAfterReopenAndDeletePreservesTheLastLiveSlot(bool fileBacked) {
		// Reopen so provisional allocation starts afresh while the physical store contains rows.
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var builder = CreateBuilder();
		if (fileBacked)
			builder.UseFile(Path.Combine(folder, "space.dat")).AutoLoad();
		else
			builder.UseMemoryStream(memory);
		using (var space = builder.Build()) {
			for (var id = 1; id <= 3; id++)
				space.Save(new Record { Id = id, Code = $"record-{id}", Label = "group" });
			CompleteWrites(space);
		}

		// Cache every row, delete a low slot, then create and persist an object into that hole.
		using var reopened = builder.Build();
		var records = reopened.GetAll<Record>().ToArray();
		var last = records[2];
		reopened.Delete(records[0]);
		var replacement = new Record { Id = 4, Code = "record-4", Label = "group" };
		Assert.That(reopened.AcceptNew(replacement), Is.GreaterThanOrEqualTo(3));
		Assert.That(reopened.Save(replacement), Is.EqualTo(0));
		Assert.That(reopened.Get<Record>(2), Is.SameAs(last));
		Assert.That(reopened.Get((Record record) => record.Id, 3), Is.SameAs(last));
		Assert.That(reopened.Get((Record record) => record.Code, "record-3"), Is.SameAs(last));
		Assert.That(reopened.Get((Record record) => record.Id, 4), Is.SameAs(replacement));
		Assert.That(reopened.GetAll<Record>(), Is.EqualTo(new[] { replacement, records[1], last }));
		CompleteWrites(reopened);
	}

	[Test]
	public void CustomFactoriesProvideTheActualDimensionSerializerAndComparer() {
		var serializers = new SerializerFactory(SerializerFactory.Default);
		serializers.RegisterAutoBuild<Record>();
		var comparer = EqualityComparerBuilder.For<Record>().By(record => record.Id);
		var comparers = new ComparerFactory(ComparerFactory.Default);
		comparers.RegisterEqualityComparer<Record>(comparer);
		using var space = new ObjectSpaceBuilder()
			.UseMemoryStream()
			.UsingSerializerFactory(serializers)
			.UsingComparerFactory(comparers)
			.AddDimension<Record>(true)
				.WithIdentifier(record => record.Id)
				.Done()
			.Build();
		space.Save(new Record { Id = 5, Code = "five", Label = "group" });
		Assert.That(space.Serializers, Is.SameAs(serializers));
		Assert.That(space.Comparers, Is.SameAs(comparers));
		Assert.That(space.Comparers.GetEqualityComparer<Record>(), Is.SameAs(comparer));
		Assert.That(space.Get((Record record) => record.Id, 5).Code, Is.EqualTo("five"));
	}

	private static ObjectSpaceBuilder CreateBuilder() {
		return new ObjectSpaceBuilder()
			.AddDimension<Record>(true)
				.WithIdentifier(record => record.Id)
				.WithUniqueIndexOn(record => record.Code)
				.WithIndexOn(record => record.Group)
				.WithIndexOn(record => record.Label)
				.WithChangeTrackingVia(record => record.Dirty)
				.Done();
	}

	private static Record[] GetGroup(ObjectSpace space, int group) {
		using var access = space.EnterAccessScope();
		var dimension = (ObjectSpace.Dimension<Record>)space.Dimensions.Single(item => item.Definition.ObjectType == typeof(Record));
		return dimension.Container.ObjectStream.GetIndexFor(record => record.Group).Values[group].Select(space.Get<Record>).ToArray();
	}

	private static Record[] GetLabel(ObjectSpace space, string label) {
		using var access = space.EnterAccessScope();
		var dimension = (ObjectSpace.Dimension<Record>)space.Dimensions.Single(item => item.Definition.ObjectType == typeof(Record));
		return dimension.Container.ObjectStream.GetIndexFor(record => record.Label).Values[label].Select(space.Get<Record>).ToArray();
	}

	private static void AssertIndexOrders(ObjectSpace space, int order) {
		using var access = space.EnterAccessScope();
		var attachments = space.Dimensions.Single().Container.ObjectStream.Streams.Attachments;
		Assert.That(((BTreeStorageAttachment<long, long>)attachments["Id.btree"]).BTree.Order, Is.EqualTo(order));
		Assert.That(((BTreeLookupStorageAttachment<int, long>)attachments["Code.btreeLookup"]).BTreeLookup.Order, Is.EqualTo(order));
		Assert.That(((BTreeLookupStorageAttachment<int, long>)attachments["Group.btreeLookup"]).BTreeLookup.Order, Is.EqualTo(order));
		Assert.That(((BTreeLookupStorageAttachment<int, long>)attachments["Label.btreeLookup"]).BTreeLookup.Order, Is.EqualTo(order));
	}

	private static void CompleteWrites(ObjectSpace space) {
		if (space is FileObjectSpace file)
			file.Commit();
		else
			space.Flush();
	}

	public class Record {
		public long Id { get; set; }
		public string Code { get; set; }
		public int Group { get; set; }
		public string Label { get; set; }
		public string Description { get; set; }
		[Transient]
		public bool Dirty { get; set; }
	}
}
