// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;

namespace Sphere10.Framework.Utils.Profiler.Tests.ObjectSpaces.Lab;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ObjectSpaceBTreeManifestTests {

	[TestCase(1, null)]
	[TestCase(2, null)]
	[TestCase(3, null)]
	[TestCase(3, BTreeType.BTree)]
	[TestCase(3, BTreeType.BTreePlus)]
	public void HistoricalManifestVersionsMapTheirRecordedLayoutIntoASpecification(int version, BTreeType? type) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "original.os");
		var configuration = new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File, IndexBTreeOrder = 4,
			IndexBTreeSpec = type.HasValue ? new BTreeSpec(type.Value, 4) : null };
		using (var space = ObjectSpaceLabFactory.Open(configuration, path)) {
			space.Save(ObjectSpaceLabFactory.CreateRecord(1));
			ObjectSpaceLabFactory.Complete(space);
		}
		var manifestPath = ObjectSpaceLabFactory.GetManifestPath(path);
		var manifest = JsonNode.Parse(File.ReadAllText(manifestPath));
		UseLegacyFields(manifest, version);
		File.WriteAllText(manifestPath, manifest.ToJsonString());
		var originalManifest = File.ReadAllText(manifestPath);
		var recorded = ObjectSpaceLabFactory.ReadConfiguration(path);
		Assert.That(recorded.IndexBTreeSpec, Is.EqualTo(configuration.IndexBTreeSpec));
		Assert.That(recorded.IndexBTreeOrder, Is.EqualTo(4));
		using (var reopened = ObjectSpaceLabFactory.Open(recorded, path))
			Assert.That(reopened.Get((ObjectSpaceLabRecord record) => record.Code, "REC-00000001").Id, Is.EqualTo(1));
		Assert.That(File.ReadAllText(manifestPath), Is.EqualTo(originalManifest), "Reading legacy configuration must not rewrite the sidecar.");
	}

	[TestCase(1)]
	[TestCase(2)]
	public void HistoricalMixedVersionsRejectAnExplicitType(int version) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "original.os");
		using (var space = ObjectSpaceLabFactory.Open(new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File }, path))
			ObjectSpaceLabFactory.Complete(space);
		var manifest = JsonNode.Parse(File.ReadAllText(ObjectSpaceLabFactory.GetManifestPath(path)));
		UseLegacyFields(manifest, version);
		manifest["Configuration"]["IndexBTreeImplementation"] = nameof(BTreeType.BTreePlus);
		File.WriteAllText(ObjectSpaceLabFactory.GetManifestPath(path), manifest.ToJsonString());
		Assert.That(() => ObjectSpaceLabFactory.ReadConfiguration(path), Throws.InvalidOperationException.With.Message.Contains("historical mixed"));
	}

	[Test]
	public void CurrentManifestRecordsASingleSpecificationAndRejectsMismatchesBeforeOpening([Values] BTreeType type) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "selected.os");
		var configuration = new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File, IndexBTreeSpec = new(type, 4) };
		using (var space = ObjectSpaceLabFactory.Open(configuration, path)) {
			space.Save(ObjectSpaceLabFactory.CreateRecord(1));
			ObjectSpaceLabFactory.Complete(space);
		}
		var originalBytes = File.ReadAllBytes(path);
		var wrong = configuration.Clone();
		wrong.IndexBTreeSpec = new BTreeSpec(type == BTreeType.BTree ? BTreeType.BTreePlus : BTreeType.BTree, wrong.IndexBTreeOrder);
		Assert.That(() => {
			using var refused = ObjectSpaceLabFactory.Open(wrong, path);
		}, Throws.InvalidOperationException.With.Message.Contains("differs"));
		wrong.IndexBTreeSpec = new BTreeSpec(configuration.IndexBTreeSpec.Type, 8);
		Assert.That(() => {
			using var refused = ObjectSpaceLabFactory.Open(wrong, path);
		}, Throws.InvalidOperationException.With.Message.Contains("differs"));
		Assert.That(File.ReadAllBytes(path), Is.EqualTo(originalBytes));
		using (var reopened = ObjectSpaceLabFactory.Open(ObjectSpaceLabFactory.ReadConfiguration(path), path))
			Assert.That(reopened.Get((ObjectSpaceLabRecord record) => record.Id, 1L).Code, Is.EqualTo("REC-00000001"));
		var manifest = JsonNode.Parse(File.ReadAllText(ObjectSpaceLabFactory.GetManifestPath(path)));
		Assert.That(manifest["SchemaVersion"].GetValue<int>(), Is.EqualTo(4));
		Assert.That(manifest["Configuration"][nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec)][nameof(BTreeSpec.Type)].GetValue<string>(), Is.EqualTo(type.ToString()));
		Assert.That(manifest["Configuration"][nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec)][nameof(BTreeSpec.Order)].GetValue<int>(), Is.EqualTo(4));
		Assert.That(manifest["Configuration"].AsObject().ContainsKey("IndexBTreeImplementation"), Is.False);
		Assert.That(manifest["Configuration"]["IndexBTreeOrder"].GetValue<int>(), Is.EqualTo(4));
	}

	[TestCase(nameof(BTreeSpec.Type))]
	[TestCase(nameof(BTreeSpec.Order))]
	public void CurrentManifestRejectsAnIncompleteSpecification(string missingField) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "incomplete.os");
		using (var space = ObjectSpaceLabFactory.Open(new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File, IndexBTreeSpec = new(BTreeType.BTree, 4) }, path))
			ObjectSpaceLabFactory.Complete(space);
		var manifestPath = ObjectSpaceLabFactory.GetManifestPath(path);
		var manifest = JsonNode.Parse(File.ReadAllText(manifestPath));
		manifest["Configuration"][nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec)].AsObject().Remove(missingField);
		File.WriteAllText(manifestPath, manifest.ToJsonString());
		Assert.That(() => ObjectSpaceLabFactory.ReadConfiguration(path), Throws.InvalidOperationException.With.Message.Contains("complete IndexBTreeSpec"));
	}

	[TestCase(0)]
	[TestCase(5)]
	public void UnsupportedManifestVersionsAreRejected(int version) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "unsupported.os");
		using (var space = ObjectSpaceLabFactory.Open(new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File }, path))
			ObjectSpaceLabFactory.Complete(space);
		var manifestPath = ObjectSpaceLabFactory.GetManifestPath(path);
		var manifest = JsonNode.Parse(File.ReadAllText(manifestPath));
		manifest["SchemaVersion"] = version;
		File.WriteAllText(manifestPath, manifest.ToJsonString());
		Assert.That(() => ObjectSpaceLabFactory.ReadConfiguration(path), Throws.InvalidOperationException.With.Message.Contains("Unsupported"));
	}

	[Test]
	public void VersionThreeRequiresItsRecordedType() {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "incomplete.os");
		var manifest = new JsonObject {
			["SchemaVersion"] = 4,
			["Configuration"] = JsonSerializer.SerializeToNode(new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File }, ProfilerSettings.JsonOptions)
		};
		UseLegacyFields(manifest, 3);
		manifest["Configuration"].AsObject().Remove("IndexBTreeImplementation");
		File.WriteAllText(ObjectSpaceLabFactory.GetManifestPath(path), manifest.ToJsonString());
		Assert.That(() => ObjectSpaceLabFactory.ReadConfiguration(path), Throws.InvalidOperationException.With.Message.Contains("IndexBTreeImplementation"));
	}

	[TestCase("IndexBTreeOrder")]
	[TestCase("IndexBTreeImplementation")]
	public void CurrentManifestRejectsConflictingLegacyFields(string field) {
		var folder = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(folder);
		var path = Path.Combine(folder, "conflicting.os");
		var manifest = new JsonObject {
			["SchemaVersion"] = 4,
			["Configuration"] = JsonSerializer.SerializeToNode(new ObjectSpaceLabConfiguration { Storage = ObjectSpaceLabStorage.File, IndexBTreeSpec = new(BTreeType.BTree, 4) }, ProfilerSettings.JsonOptions)
		};
		manifest["Configuration"][field] = field == "IndexBTreeOrder" ? JsonValue.Create(8) : null;
		File.WriteAllText(ObjectSpaceLabFactory.GetManifestPath(path), manifest.ToJsonString());
		Assert.That(() => ObjectSpaceLabFactory.ReadConfiguration(path), Throws.InvalidOperationException.With.Message.Contains("conflicts"));
	}

	private static void UseLegacyFields(JsonNode manifest, int version) {
		manifest["SchemaVersion"] = version;
		var configuration = manifest["Configuration"].AsObject();
		var spec = configuration[nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec)];
		configuration["IndexBTreeOrder"] = spec?[nameof(BTreeSpec.Order)]?.DeepClone() ?? configuration["IndexBTreeOrder"]?.DeepClone();
		if (version >= 3)
			configuration["IndexBTreeImplementation"] = spec?[nameof(BTreeSpec.Type)]?.DeepClone();
		configuration.Remove(nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec));
		if (version == 1)
			configuration.Remove(nameof(ObjectSpaceLabConfiguration.SchemaProfile));
	}
}
