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
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests.ObjectSpaces;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class IndexBTreeSpecTests {
	[Test]
	public void BuilderConveniencesComposeOneSpecification([Values] BTreeType type, [Values] bool specFirst) {
		var original = new BTreeSpec(BTreeType.BTree, 7);
		var builder = new ObjectSpaceBuilder();
		builder.AddDimension<Record>(true).WithIdentifier(record => record.Id).Done();
		if (specFirst)
			builder.WithIndexBTreeSpec(original).WithIndexBTreeType(type).WithIndexBTreeOrder(9);
		else
			builder.WithIndexBTreeType(type).WithIndexBTreeOrder(9);
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(type, 9)));
		Assert.That(original, Is.EqualTo(new BTreeSpec(BTreeType.BTree, 7)));
	}

	[Test]
	public void SelectingTypeAloneDoesNotDeclareTheOpeningOrder([Values] BTreeType type) {
		var builder = new ObjectSpaceBuilder().WithIndexBTreeType(type);
		builder.AddDimension<Record>(true).WithIdentifier(record => record.Id).Done();
		Assert.That(builder.BuildDefinition().HasExplicitIndexBTreeOrder, Is.False);
		builder.WithIndexBTreeSpec(new BTreeSpec(type, 7));
		Assert.That(builder.BuildDefinition().HasExplicitIndexBTreeOrder, Is.True);
	}

	[Test]
	public void LoadedDefinitionAcceptsAnEqualSpecAndRejectsEitherLayoutChange([Values] BTreeType type) {
		using var memory = new MemoryStream();
		using var space = CreateBuilder(type, false).UseMemoryStream(memory).Build();
		var definition = space.Definition;
		var spec = definition.IndexBTreeSpec;
		Assert.That(definition.HasBoundIndexBTreeSpec, Is.True);
		Assert.That(() => definition.IndexBTreeSpec = new BTreeSpec(type, spec.Order), Throws.Nothing);
		Assert.That(() => definition.IndexBTreeSpec = new BTreeSpec(type, spec.Order + 1), Throws.InvalidOperationException);
		Assert.That(() => definition.IndexBTreeSpec = new BTreeSpec(type == BTreeType.BTree ? BTreeType.BTreePlus : BTreeType.BTree, spec.Order), Throws.InvalidOperationException);
		Assert.That(definition.IndexBTreeSpec, Is.EqualTo(spec));
		space.Save(new Record { Id = 1, Code = "after-rejection", Group = 7 });
		Assert.That(space.Get((Record record) => record.Id, 1L).Code, Is.EqualTo("after-rejection"));
	}

	public static IEnumerable<TestCaseData> Layouts() {
		foreach (var type in new BTreeType?[] { null, BTreeType.BTree, BTreeType.BTreePlus })
			foreach (var fileBacked in new[] { false, true })
				foreach (var merkleized in new[] { false, true })
					yield return new TestCaseData(type, fileBacked, merkleized);
	}

	[TestCase(null)]
	[TestCase(BTreeType.BTree)]
	[TestCase(BTreeType.BTreePlus)]
	public void DefinitionSelectsActualUniqueAndGroupedTreeImplementations(BTreeType? type) {
		using var memory = new MemoryStream();
		using var space = CreateBuilder(type, false).UseMemoryStream(memory).Build();
		if (type.HasValue)
			Assert.That(space.Definition.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(type.Value, 4)));
		else
			Assert.That(space.Definition.IndexBTreeSpec, Is.Null, "Order-only configuration must not create a partial specification.");
		using var access = space.EnterAccessScope();
		var slot = space.Save(new Record { Id = 42, Code = "direct-value", Group = 7 });
		var dimension = (ObjectSpace.Dimension<Record>)space.Dimensions.Single();
		Assert.That(dimension.Container.ObjectStream.GetUniqueIndexFor(record => record.Id).Values[42L], Is.EqualTo(slot));
		var attachments = dimension.Container.ObjectStream.Streams.Attachments.Values;
		var unique = attachments.OfType<BTreeStorageAttachment<long, long>>().Single();
		var expectedUniqueType = type == BTreeType.BTreePlus ? typeof(StreamMappedBTreePlus<long, long>) : typeof(StreamMappedBTree<long, long>);
		Assert.That(unique.Tree.GetType(), Is.EqualTo(expectedUniqueType));
		var lookups = attachments.OfType<BTreeLookupStorageAttachment<int, long>>().ToArray();
		Assert.That(lookups, Has.Length.EqualTo(2), "The category and variable-key checksum indexes must both be covered.");
		Assert.That(lookups.Select(lookup => lookup.BTreeLookup.Spec.Type), Is.All.EqualTo(type ?? BTreeType.BTreePlus));
	}

	[TestCaseSource(nameof(Layouts))]
	public void SelectedIndexesPreserveMutationSlotsAndMerkleRootAcrossReopen(BTreeType? type, bool fileBacked, bool merkleized) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var builder = ConfigureStorage(CreateBuilder(type, merkleized), folder, memory, fileBacked);
		var expected = new SortedDictionary<long, Record>();
		byte[] root;
		using (var space = builder.Build()) {
			var slots = new Dictionary<long, long>();
			for (var id = 0; id < 64; id++) {
				var record = new Record { Id = id, Code = $"record-{id}", Group = id % 7 };
				slots[id] = space.Save(record);
				expected.Add(id, record);
			}
			var removedSlots = new HashSet<long>();
			foreach (var id in Enumerable.Range(0, 64).Where(id => id % 3 == 0)) {
				space.Delete(expected[id]);
				removedSlots.Add(slots[id]);
				expected.Remove(id);
			}
			foreach (var record in expected.Values.Where(record => record.Id % 2 == 0)) {
				record.Code += "-updated";
				record.Group += 20;
				space.Save(record);
			}
			var provisional = space.New<Record>();
			provisional.Id = 1000;
			provisional.Code = "provisional";
			provisional.Group = 3;
			Assert.That(space.Get((Record record) => record.Id, 1000L), Is.SameAs(provisional));
			var reusedSlot = space.Save(provisional);
			Assert.That(removedSlots, Does.Contain(reusedSlot));
			expected.Add(provisional.Id, provisional);
			Complete(space);
			AssertIndexes(space, expected);
			root = merkleized ? ReadRoot(space) : null;
			Assert.That(() => space.Definition.IndexBTreeType = type == BTreeType.BTree
				? BTreeType.BTreePlus : BTreeType.BTree, Throws.InvalidOperationException);
		}
		using var reopened = builder.Build();
		Assert.That(reopened.Definition.IndexBTreeType, Is.EqualTo(type));
		AssertIndexes(reopened, expected);
		if (merkleized)
			Assert.That(ReadRoot(reopened), Is.EqualTo(root));
	}

	[Test]
	public void FileRollbackReconstructsBothSelectedIndexKinds([Values] BTreeType type) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var builder = ConfigureStorage(CreateBuilder(type, true), folder, memory, true);
		using var space = (FileObjectSpace)builder.Build();
		space.Save(new Record { Id = 1, Code = "original", Group = 7 });
		space.Commit();
		var root = ReadRoot(space);
		var record = space.Get((Record item) => item.Id, 1L);
		record.Code = "pending";
		record.Group = 8;
		space.Save(record);
		space.Save(new Record { Id = 2, Code = "new", Group = 8 });
		space.Rollback();
		Assert.That(space.Count<Record>(), Is.EqualTo(1));
		Assert.That(space.Get((Record item) => item.Code, "original").Group, Is.EqualTo(7));
		Assert.That(space.TryGet((Record item) => item.Code, "pending", out _), Is.False);
		Assert.That(space.TryGet((Record item) => item.Id, 2L, out _), Is.False);
		Assert.That(ReadRoot(space), Is.EqualTo(root));
	}

	[TestCaseSource(nameof(Layouts))]
	public void FreshBuilderReopensWithSuppliedIndexImplementation(BTreeType? type, bool fileBacked, bool merkleized) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		using var memory = new MemoryStream();
		var expected = new SortedDictionary<long, Record>();
		byte[] root;
		using (var original = ConfigureStorage(CreateBuilder(type, merkleized), folder, memory, fileBacked).Build()) {
			for (var id = 0; id < 24; id++) {
				var record = new Record { Id = id, Code = $"record-{id}", Group = id % 5 };
				original.Save(record);
				expected.Add(id, record);
			}
			Complete(original);
			root = merkleized ? ReadRoot(original) : null;
		}
		using var reopened = ConfigureStorage(CreateBuilder(type, merkleized), folder, memory, fileBacked).Build();
		Assert.That(reopened.Definition.IndexBTreeType, Is.EqualTo(type));
		AssertIndexes(reopened, expected);
		if (merkleized)
			Assert.That(ReadRoot(reopened), Is.EqualTo(root));
	}

	private static ObjectSpaceBuilder CreateBuilder(BTreeType? type, bool merkleized) {
		var builder = new ObjectSpaceBuilder()
			.WithIndexBTreeOrder(4)
			.WithPageSize(4096)
			.WithMaxMemory(8192)
			.AutoLoad()
			.AddDimension<Record>(true)
				.WithIdentifier(record => record.Id)
				.WithUniqueIndexOn(record => record.Code)
				.WithIndexOn(record => record.Group)
				.Done();
		if (type.HasValue)
			builder.WithIndexBTreeType(type.Value);
		if (merkleized)
			builder.Merkleized();
		return builder;
	}

	private static ObjectSpaceBuilder ConfigureStorage(ObjectSpaceBuilder builder, string folder, MemoryStream memory, bool fileBacked) {
		if (!fileBacked)
			return builder.UseMemoryStream(memory);
		var pages = Path.Combine(folder, "pages");
		Directory.CreateDirectory(pages);
		return builder.UseFile(Path.Combine(folder, "store.dat")).KeepUncommittedPagesAt(pages);
	}

	private static void Complete(ObjectSpace space) {
		if (space is FileObjectSpace file)
			file.Commit();
		else
			space.Flush();
	}

	private static byte[] ReadRoot(ObjectSpace space) {
		using var access = space.EnterAccessScope();
		var digestSize = Hashers.GetDigestSizeBytes(space.Definition.HashFunction);
		return space.Streams.Header.MapExtensionProperty(0, sizeof(bool) + digestSize, new ConstantSizeNullableByteArraySerializer(digestSize)).Value;
	}

	private static void AssertIndexes(ObjectSpace space, SortedDictionary<long, Record> expected) {
		Assert.That(space.Count<Record>(), Is.EqualTo(expected.Count));
		foreach (var record in expected.Values) {
			Assert.That(space.Get((Record item) => item.Id, record.Id).Code, Is.EqualTo(record.Code));
			Assert.That(space.Get((Record item) => item.Code, record.Code).Id, Is.EqualTo(record.Id));
		}
		using var access = space.EnterAccessScope();
		var dimension = (ObjectSpace.Dimension<Record>)space.Dimensions.Single();
		var grouped = dimension.Container.ObjectStream.GetIndexFor(record => record.Group).Values;
		foreach (var group in expected.Values.GroupBy(record => record.Group))
			Assert.That(grouped[group.Key].Select(space.Get<Record>).Select(record => record.Id), Is.EquivalentTo(group.Select(record => record.Id)));
	}

	public sealed class Record {
		public long Id { get; set; }
		public string Code { get; set; }
		public int Group { get; set; }
	}
}
