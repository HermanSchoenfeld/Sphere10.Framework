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
	/// <summary>Verifies repeated builds produce independent owners and definitions with or without metadata.</summary>
	[Test]
	public void EveryBuildCreatesAnIndependentObjectSpace([Values] bool storeMetaData) {
		// Reuse configuration while letting each build create its own memory storage.
		var builder = CreateBuilder(BTreeType.BTree, false).UseMemoryStream();
		if (storeMetaData)
			builder.StoreMetaData();
		using var first = builder.Build();
		using var second = builder.Build();
		using var third = builder.Build();

		// Instances, schema snapshots and persisted records belong to their individual stores.
		Assert.That(first, Is.Not.SameAs(second));
		Assert.That(second, Is.Not.SameAs(third));
		Assert.That(first.Definition, Is.Not.SameAs(second.Definition));
		Assert.That(first.Definition.Dimensions[0], Is.Not.SameAs(second.Definition.Dimensions[0]));
		Assert.That(first.Definition.Dimensions[0].Indexes[0], Is.Not.SameAs(second.Definition.Dimensions[0].Indexes[0]));
		first.Save(new Record { Id = 42, Code = "first store", Group = 7 });
		first.Flush();
		Assert.That(first.Count<Record>(), Is.EqualTo(1));
		Assert.That(second.Count<Record>(), Is.Zero);
		Assert.That(third.Count<Record>(), Is.Zero);
	}

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

	/// <summary>Verifies that selecting a tree type retains the complete default order until it is replaced.</summary>
	[Test]
	public void SelectingTypePreservesTheConcreteDefaultOrder([Values] BTreeType type) {
		var builder = new ObjectSpaceBuilder().WithIndexBTreeType(type);
		builder.AddDimension<Record>(true).WithIdentifier(record => record.Id).Done();
		var defaults = builder.BuildDefinition();
		Assert.That(defaults.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(type, Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder)));
		builder.WithIndexBTreeSpec(new BTreeSpec(type, 7));
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(type, 7)));
		Assert.That(defaults.IndexBTreeSpec.Order, Is.EqualTo(Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder));
	}

	/// <summary>Verifies complete per-index specifications and independent definition snapshots.</summary>
	[Test]
	public void BuildDefinitionResolvesCompleteSpecificationsForEveryMemberIndex() {
		var builder = CreateBuilder(null, true);
		var definition = builder.BuildDefinition();
		var indexes = definition.Dimensions.Single().Indexes;
		Assert.That(definition.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, 4)));
		Assert.That(indexes.Where(index => index.Member != null).Select(index => index.BTreeSpec), Is.All.EqualTo(definition.IndexBTreeSpec));
		Assert.That(indexes.Where(index => index.Member == null).Select(index => index.BTreeSpec), Is.All.Null);
		builder.WithIndexBTreeOrder(9);
		Assert.That(builder.BuildDefinition().Dimensions.Single().Indexes.Where(index => index.Member != null).Select(index => index.BTreeSpec.Order), Is.All.EqualTo(9));
		Assert.That(indexes.Where(index => index.Member != null).Select(index => index.BTreeSpec.Order), Is.All.EqualTo(4));
	}

	/// <summary>Verifies explicit per-index layout settings survive shared preferences and reopening.</summary>
	[Test]
	public void ExplicitIndexSpecificationSurvivesSharedBuilderChangesAndReopen() {
		var builder = CreateBuilder(null, false);
		var identifier = builder.Configure<Record>().Indexes.Single(index => index.Type == ObjectSpaceDefinition.IndexType.Identifier);
		var specification = new BTreeSpec(BTreeType.BTreePlus, 7);
		identifier.BTreeSpec = specification;
		builder.WithIndexBTreeOrder(9).WithIndexBTreeType(BTreeType.BTree);
		using var memory = new MemoryStream();
		builder.UseMemoryStream(memory);
		using (var space = builder.Build()) {
			using var access = space.EnterAccessScope();
			var indexes = space.Dimensions.Single().Definition.Indexes;
			Assert.That(indexes.Single(index => index.Type == ObjectSpaceDefinition.IndexType.Identifier).BTreeSpec, Is.EqualTo(specification));
			Assert.That(indexes.Where(index => index.Type is ObjectSpaceDefinition.IndexType.UniqueKey or ObjectSpaceDefinition.IndexType.Index)
				.Select(index => index.BTreeSpec), Is.All.EqualTo(new BTreeSpec(BTreeType.BTree, 9)));
			space.Save(new Record { Id = 1, Code = "per-index", Group = 7 });
			space.Flush();
		}
		using var reopened = builder.Build();
		Assert.That(reopened.Get((Record record) => record.Id, 1L).Code, Is.EqualTo("per-index"));
		Assert.That(reopened.Definition.Dimensions.Single().Indexes.Single(index => index.Type == ObjectSpaceDefinition.IndexType.Identifier).BTreeSpec,
			Is.EqualTo(specification));
	}

	/// <summary>Verifies that pre-load definition replacements configure the actual index implementations.</summary>
	[TestCase("order")]
	[TestCase("specification")]
	[TestCase("type")]
	public void DefinitionChangesBeforeLoadingReachTheActualTrees(string setting) {
		var builder = CreateBuilder(null, false);
		var definition = builder.BuildDefinition();
		if (setting == "order")
			definition.IndexBTreeOrder = 8;
		else if (setting == "specification")
			definition.IndexBTreeSpec = new BTreeSpec(BTreeType.BTreePlus, 8);
		else
			definition.IndexBTreeType = BTreeType.BTreePlus;
		var order = setting == "type" ? 4 : 8;
		using var memory = new MemoryStream();
		var serializers = new SerializerFactory(SerializerFactory.Default).RegisterAutoBuild<Record>();
		using var space = new MemoryObjectSpace(memory, definition, serializers, new ComparerFactory(ComparerFactory.Default));
		using var access = space.EnterAccessScope();
		var objects = ((ObjectSpace.Dimension<Record>)space.Dimensions.Single()).Container.ObjectStream;
		var attachments = objects.Streams.Attachments.Values;
		var unique = attachments.OfType<BTreeStorageAttachment<long, long>>().Single();
		Assert.That(unique.Tree.Order, Is.EqualTo(order));
		Assert.That(unique.Tree.GetType(), Is.EqualTo(setting == "order" ? typeof(StreamMappedBTree<long, long>) : typeof(StreamMappedBTreePlus<long, long>)));
		Assert.That(attachments.OfType<BTreeLookupStorageAttachment<int, long>>().Select(index => index.BTreeLookup.Spec),
			Is.All.EqualTo(new BTreeSpec(setting == "order" ? BTreeSpec.Default.Type : BTreeType.BTreePlus, order)));
		var specs = definition.Dimensions.Single().Indexes.Select(index => index.BTreeSpec).ToArray();
		definition.IndexBTreeOrder = order;
		if (setting != "order") {
			definition.IndexBTreeType = BTreeType.BTreePlus;
			definition.IndexBTreeSpec = new BTreeSpec(BTreeType.BTreePlus, order);
		}
		Assert.That(definition.Dimensions.Single().Indexes.Select(index => index.BTreeSpec), Is.EqualTo(specs));
		Assert.That(builder.BuildDefinition().IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, 4)));
		Assert.That(builder.BuildDefinition().Dimensions.Single().Indexes.Where(index => index.Member != null).Select(index => index.BTreeSpec),
			Is.All.EqualTo(new BTreeSpec(BTreeSpec.Default.Type, 4)));
		space.Save(new Record { Id = 1, Code = "changed-before-load", Group = 7 });
		Assert.That(space.Get((Record item) => item.Code, "changed-before-load").Id, Is.EqualTo(1));
	}

	/// <summary>Verifies builder independence and metadata rejection of incompatible index layout changes.</summary>
	[TestCase("order")]
	[TestCase("type")]
	[TestCase("specification")]
	public void BuilderReconfigurationCreatesANewDefinitionAndStoredMetadataRejectsTheChangedLayout(string setting) {
		using var memory = new MemoryStream();
		var builder = CreateBuilder(BTreeType.BTree, false).StoreMetaData().UseMemoryStream(memory);
		ObjectSpaceDefinition original;
		using (var space = builder.Build()) {
			original = space.Definition;
			space.Save(new Record { Id = 1, Code = "stored", Group = 7 });
			space.Flush();
		}
		var bytes = memory.ToArray();
		if (setting == "order")
			builder.WithIndexBTreeOrder(9);
		else if (setting == "type")
			builder.WithIndexBTreeType(BTreeType.BTreePlus);
		else
			builder.WithIndexBTreeSpec(new BTreeSpec(BTreeType.BTreePlus, 9));
		var expected = new BTreeSpec(setting == "order" ? BTreeType.BTree : BTreeType.BTreePlus, setting == "type" ? 4 : 9);
		var configured = builder.BuildDefinition();
		Assert.That(configured.IndexBTreeSpec, Is.EqualTo(expected));
		Assert.That(configured.Dimensions.Single().Indexes.Where(index => index.Member != null).Select(index => index.BTreeSpec), Is.All.EqualTo(expected));
		Assert.That(original.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(BTreeType.BTree, 4)));
		Assert.That(original.Dimensions.Single().Indexes.Where(index => index.Member != null).Select(index => index.BTreeSpec),
			Is.All.EqualTo(new BTreeSpec(BTreeType.BTree, 4)));
		Assert.That(() => { using var rejected = builder.Build(); }, Throws.TypeOf<InvalidDataException>());
		Assert.That(memory.ToArray(), Is.EqualTo(bytes));
		Assert.That(memory.CanRead && memory.CanWrite, Is.True);
	}

	public static IEnumerable<TestCaseData> Layouts() {
		foreach (var type in new BTreeType?[] { null, BTreeType.BTree, BTreeType.BTreePlus })
			foreach (var fileBacked in new[] { false, true })
				foreach (var merkleized in new[] { false, true })
					yield return new TestCaseData(type, fileBacked, merkleized);
	}

	/// <summary>Verifies complete definitions select the intended unique and grouped index implementations.</summary>
	[TestCase(null)]
	[TestCase(BTreeType.BTree)]
	[TestCase(BTreeType.BTreePlus)]
	public void DefinitionSelectsActualUniqueAndGroupedTreeImplementations(BTreeType? type) {
		using var memory = new MemoryStream();
		using var space = CreateBuilder(type, false).UseMemoryStream(memory).Build();
		var selectedType = type ?? BTreeSpec.Default.Type;
		Assert.That(space.Definition.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(selectedType, 4)));
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
		Assert.That(lookups.Select(lookup => lookup.BTreeLookup.Spec.Type), Is.All.EqualTo(selectedType));
	}

	/// <summary>Verifies index mutations, recycled slots and Merkle roots survive reopening with the selected layout.</summary>
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
		}
		using var reopened = builder.Build();
		Assert.That(reopened.Definition.IndexBTreeType, Is.EqualTo(type ?? BTreeSpec.Default.Type));
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

	/// <summary>Verifies that a fresh configured builder reopens the original index implementation and data.</summary>
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
		Assert.That(reopened.Definition.IndexBTreeType, Is.EqualTo(type ?? BTreeSpec.Default.Type));
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
