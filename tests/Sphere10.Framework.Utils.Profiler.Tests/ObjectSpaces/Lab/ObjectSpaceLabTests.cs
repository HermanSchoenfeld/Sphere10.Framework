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
using System.Text.Json.Nodes;
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.Tests.ObjectSpaces.Lab;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
[FixtureLifeCycle(LifeCycle.InstancePerTestCase)]
public class ObjectSpaceLabTests {
	private string _directory;
	private ActionScope<string> _directoryCleanup;

	[SetUp]
	public void SetUp() {
		_directory = Path.Combine(Path.GetTempPath(), "Sphere10ObjectSpaceLabTests", Guid.NewGuid().ToString("N"));
		Tools.FileSystem.CreateDirectory(_directory);
		_directoryCleanup = Tools.Scope.DeleteDirOnDispose(_directory);
	}

	[TearDown]
	public void TearDown() => _directoryCleanup.Dispose();

	[TestCase(3)]
	[TestCase(16)]
	[TestCase(64)]
	public void DeterministicRecordsRoundTripWithConfiguredIndexes(int order) {
		var configuration = new ObjectSpaceLabConfiguration { IndexBTreeOrder = order };
		using var stream = new MemoryStream();
		using (var space = ObjectSpaceLabFactory.Open(configuration, memoryStream: stream)) {
			ObjectSpaceLabFactory.Seed(space, 40, 256);
			ObjectSpaceLabFactory.Complete(space);
			Assert.That(space.GetAll<ObjectSpaceLabRecord>().Count(), Is.EqualTo(40));
		}
		Assert.That(stream.CanRead, Is.True, "The supplied stream remains caller-owned.");
		stream.Position = 0;
		using var reopened = ObjectSpaceLabFactory.Open(configuration, memoryStream: stream);
		var record = reopened.Get((ObjectSpaceLabRecord item) => item.Code, "REC-00000017");
		Assert.That(record.Id, Is.EqualTo(17));
		Assert.That(record.Balance, Is.EqualTo(21.25m));
		Assert.That(record.Payload, Is.EqualTo(ObjectSpaceLabFactory.CreateRecord(17, 256).Payload));
		Assert.That(reopened.Get((ObjectSpaceLabRecord item) => item.Id, 17L), Is.SameAs(record));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void DirtyTrackingDemonstratesExplicitSaveVersusAutoSave(bool autoSave) {
		var configuration = new ObjectSpaceLabConfiguration { AutoSave = autoSave };
		using var stream = new MemoryStream();
		using (var space = ObjectSpaceLabFactory.Open(configuration, memoryStream: stream)) {
			ObjectSpaceLabFactory.Seed(space, 1);
			ObjectSpaceLabFactory.Complete(space);
			var tracked = space.Get<ObjectSpaceLabRecord>(0);
			Assert.That(tracked.Dirty, Is.False);
			tracked.Balance = 99m;
			ObjectSpaceLabFactory.Complete(space);
		}
		stream.Position = 0;
		using var reopened = ObjectSpaceLabFactory.Open(configuration, memoryStream: stream);
		Assert.That(reopened.Get<ObjectSpaceLabRecord>(0).Balance, Is.EqualTo(autoSave ? 99m : 1.25m));
	}

	[Test]
	public void FileCommitRollbackAndReopenPreserveOnlyCommittedState() {
		var configuration = new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File, IndexBTreeOrder = 8 };
		var filePath = Path.Combine(_directory, "records.dat");
		using (var space = ObjectSpaceLabFactory.Open(configuration, filePath)) {
			ObjectSpaceLabFactory.Seed(space, 20);
			ObjectSpaceLabFactory.Complete(space);
			var oldRecord = space.Get<ObjectSpaceLabRecord>(0);
			oldRecord.Category = 99;
			oldRecord.Balance = 777m;
			space.Save(oldRecord);
			((FileObjectSpace)space).Rollback();
			var restored = space.Get<ObjectSpaceLabRecord>(0);
			Assert.That(restored, Is.Not.SameAs(oldRecord), "Rollback invalidates the identity cache.");
			Assert.That(restored.Category, Is.Zero);
			Assert.That(restored.Balance, Is.EqualTo(1.25m));
		}
		var recorded = ObjectSpaceLabFactory.ReadConfiguration(filePath);
		Assert.That(recorded.IndexBTreeOrder, Is.EqualTo(8));
		using var reopened = ObjectSpaceLabFactory.Open(recorded, filePath);
		Assert.That(reopened.GetAll<ObjectSpaceLabRecord>().Count(), Is.EqualTo(20));
		Assert.That(reopened.Get<ObjectSpaceLabRecord>(0).Balance, Is.EqualTo(1.25m));
	}

	[Test]
	public void ReopeningDifferentLayoutIsRejectedBeforeTouchingData() {
		var configuration = new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File };
		var filePath = Path.Combine(_directory, "records.dat");
		using (var space = ObjectSpaceLabFactory.Open(configuration, filePath)) {
			ObjectSpaceLabFactory.Seed(space, 3);
			ObjectSpaceLabFactory.Complete(space);
		}
		var originalBytes = File.ReadAllBytes(filePath);
		var incompatible = configuration.Clone();
		incompatible.IndexBTreeOrder = 8;
		Assert.That(() => ObjectSpaceLabFactory.Open(incompatible, filePath), Throws.InvalidOperationException);
		Assert.That(File.ReadAllBytes(filePath), Is.EqualTo(originalBytes));
		File.Delete(ObjectSpaceLabFactory.GetManifestPath(filePath));
		Assert.That(() => ObjectSpaceLabFactory.Open(configuration, filePath), Throws.InvalidOperationException);
		Assert.That(File.ReadAllBytes(filePath), Is.EqualTo(originalBytes));
	}

	[TestCase(ObjectSpaceLabIndexProfile.IdentifierOnly)]
	[TestCase(ObjectSpaceLabIndexProfile.Full)]
	public void DeleteAndReuseRetainLiveRecordsAndUniqueLookup(ObjectSpaceLabIndexProfile profile) {
		using var space = ObjectSpaceLabFactory.Open(new ObjectSpaceLabConfiguration { IndexProfile = profile });
		ObjectSpaceLabFactory.Seed(space, 6);
		var deleted = space.Get((ObjectSpaceLabRecord record) => record.Id, 2L);
		space.Delete(deleted);
		var inserted = ObjectSpaceLabFactory.CreateRecord(99);
		space.AcceptNew(inserted);
		space.Save(inserted);
		Assert.That(space.GetAll<ObjectSpaceLabRecord>().Select(record => record.Id), Is.EquivalentTo(new long[] { 1, 3, 4, 5, 6, 99 }));
		Assert.That(space.Get((ObjectSpaceLabRecord record) => record.Id, 99L), Is.SameAs(inserted));
		Assert.That(space.TryGet((ObjectSpaceLabRecord record) => record.Id, 2L, out _), Is.False);
	}

	[TestCase(nameof(ObjectSpaceLabConfiguration.IndexBTreeOrder))]
	[TestCase(nameof(ObjectSpaceLabConfiguration.IndexProfile))]
	[TestCase(nameof(ObjectSpaceLabConfiguration.HashFunction))]
	public void IncompleteManifestCannotFallBackToAnotherLayout(string missingField) {
		var configuration = new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File, IndexBTreeOrder = 8 };
		var filePath = Path.Combine(_directory, "records.dat");
		using (var space = ObjectSpaceLabFactory.Open(configuration, filePath)) {
			ObjectSpaceLabFactory.Seed(space, 3);
			ObjectSpaceLabFactory.Complete(space);
		}
		var originalBytes = File.ReadAllBytes(filePath);
		var manifestPath = ObjectSpaceLabFactory.GetManifestPath(filePath);
		var manifest = JsonNode.Parse(File.ReadAllText(manifestPath));
		manifest["Configuration"].AsObject().Remove(missingField);
		File.WriteAllText(manifestPath, manifest.ToJsonString());
		Assert.That(() => ObjectSpaceLabFactory.ReadConfiguration(filePath), Throws.InvalidOperationException);
		Assert.That(() => ObjectSpaceLabFactory.Open(configuration, filePath), Throws.InvalidOperationException);
		Assert.That(File.ReadAllBytes(filePath), Is.EqualTo(originalBytes));
	}

	[TestCase(CHF.SHA2_256)]
	[TestCase(CHF.Blake2b_256)]
	[TestCase(CHF.Blake2b_128)]
	public void MerkleConfigurationsRehydrateWithSelectedHashAlgorithm(CHF algorithm) {
		var configuration = new ObjectSpaceLabConfiguration { HashFunction = algorithm, Merkleized = true };
		using var stream = new MemoryStream();
		using (var space = ObjectSpaceLabFactory.Open(configuration, memoryStream: stream)) {
			ObjectSpaceLabFactory.Seed(space, 12);
			ObjectSpaceLabFactory.Complete(space);
			Assert.That(space.Definition.HashFunction, Is.EqualTo(algorithm));
		}
		stream.Position = 0;
		using var reopened = ObjectSpaceLabFactory.Open(configuration, memoryStream: stream);
		Assert.That(reopened.Get((ObjectSpaceLabRecord record) => record.Code, "REC-00000012").Id, Is.EqualTo(12));
		Assert.That(reopened.GetAll<ObjectSpaceLabRecord>().Count(), Is.EqualTo(12));
	}

	[Test]
	public void ClonedRecordsCannotMutateTrackedPayload() {
		using var space = ObjectSpaceLabFactory.Open(new ObjectSpaceLabConfiguration());
		ObjectSpaceLabFactory.Seed(space, 1);
		var tracked = space.Get<ObjectSpaceLabRecord>(0);
		var detached = tracked.Clone();
		detached.Payload[0] = 255;
		detached.Balance = 500m;
		Assert.That(tracked.Payload[0], Is.EqualTo(1));
		Assert.That(tracked.Balance, Is.EqualTo(1.25m));
	}

	[Test]
	public void InvalidConfigurationFailsBeforeCreatingStorage() {
		var configuration = new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File, MaxMemory = 1, PageSize = 4096, IndexBTreeOrder = 2 };
		var filePath = Path.Combine(_directory, "invalid.dat");
		Assert.That(configuration.Validate().IsFailure, Is.True);
		Assert.That(() => ObjectSpaceLabFactory.Open(configuration, filePath), Throws.InvalidOperationException);
		Assert.That(File.Exists(filePath), Is.False);
		Assert.That(File.Exists(ObjectSpaceLabFactory.GetManifestPath(filePath)), Is.False);
	}
	[TestCase(false)]
	[TestCase(true)]
	public void DeveloperSchemaPersistsIndependentDimensionsAndTheirIndexes(bool merkleized) {
		var configuration = new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File, SchemaProfile = ObjectSpaceLabSchemaProfile.DeveloperLab, Merkleized = merkleized, IndexBTreeOrder = 3 };
		var path = Path.Combine(_directory, "developer.os");
		using (var space = ObjectSpaceLabFactory.Open(configuration, path)) {
			space.Save(ObjectSpaceLabFactory.CreateRecord(1));
			space.Save(ObjectSpaceLabFactory.CreateGroup(1));
			space.Save(ObjectSpaceLabFactory.CreateEvent(1, 1));
			ObjectSpaceLabFactory.Complete(space);
		}
		using var reopened = ObjectSpaceLabFactory.Open(ObjectSpaceLabFactory.ReadConfiguration(path), path);
		Assert.That(reopened.Dimensions.Select(dimension => dimension.Definition.ObjectType), Is.EqualTo(new[] { typeof(ObjectSpaceLabRecord), typeof(ObjectSpaceLabGroup), typeof(ObjectSpaceLabEvent) }));
		Assert.That(reopened.Get<ObjectSpaceLabRecord>(0).Code, Is.EqualTo("REC-00000001"));
		Assert.That(reopened.Get<ObjectSpaceLabGroup>(0).Code, Is.EqualTo("GRP-0001"));
		Assert.That(reopened.Get<ObjectSpaceLabEvent>(0).RecordId, Is.EqualTo(1));
		Assert.That(reopened.Get((ObjectSpaceLabGroup group) => group.Code, "GRP-0001").Id, Is.EqualTo(1));
	}

	[Test]
	public void OriginalManifestMapsExplicitlyToPayloadOnlyAndNewManifestRequiresSchemaProfile() {
		var path = Path.Combine(_directory, "original.os");
		var configuration = new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File };
		using (var space = ObjectSpaceLabFactory.Open(configuration, path)) { space.Save(ObjectSpaceLabFactory.CreateRecord(1)); ObjectSpaceLabFactory.Complete(space); }
		var manifestPath = ObjectSpaceLabFactory.GetManifestPath(path);
		var manifest = JsonNode.Parse(File.ReadAllText(manifestPath));
		manifest["SchemaVersion"] = 1;
		manifest["Configuration"].AsObject().Remove(nameof(ObjectSpaceLabConfiguration.SchemaProfile));
		manifest["Configuration"]["IndexBTreeOrder"] = configuration.IndexBTreeOrder;
		manifest["Configuration"].AsObject().Remove(nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec));
		File.WriteAllText(manifestPath, manifest.ToJsonString());
		Assert.That(ObjectSpaceLabFactory.ReadConfiguration(path).SchemaProfile, Is.EqualTo(ObjectSpaceLabSchemaProfile.PayloadOnly));
		using (var space = ObjectSpaceLabFactory.Open(ObjectSpaceLabFactory.ReadConfiguration(path), path))
			Assert.That(space.Get<ObjectSpaceLabRecord>(0).Id, Is.EqualTo(1));
		manifest["SchemaVersion"] = 2;
		File.WriteAllText(manifestPath, manifest.ToJsonString());
		Assert.That(() => ObjectSpaceLabFactory.ReadConfiguration(path), Throws.InvalidOperationException.With.Message.Contains("SchemaProfile"));
	}

}
