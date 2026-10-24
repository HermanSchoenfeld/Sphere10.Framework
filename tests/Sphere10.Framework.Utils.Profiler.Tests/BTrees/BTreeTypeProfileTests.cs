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
using System.Text.Json;
using System.Text.Json.Nodes;
using NUnit.Framework;
using Sphere10.Framework.Utils.Profiler.BTrees;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BTreeTypeProfileTests {
	[Test]
	public void LookupImplementationCompletesRealBenchmarkMutationsAndReopen([Values] BTreeStorage storage, [Values] BTreeType implementation) {
		var root = Path.Combine(Path.GetTempPath(), "Sphere10-LookupImplementation-" + Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(root);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(root);
		var profile = new BTreeProfile {
			Name = "selected", Storage = storage, Family = BTreeFamily.Lookup, Spec = new BTreeSpec(implementation, 4),
			KeyShape = BTreeKeyShape.Int32, PageSize = 4096, CacheBytes = 8192
		};
		var settings = new BTreeProfilerSettings {
			RecordCount = 96, OperationCount = 16, GroupSize = 8, DataDirectory = root,
			OutputDirectory = Path.Combine(root, "results"), Profiles = new[] { profile }
		};
		using var fixture = new BTreeBenchmarkFixture(settings, profile);
		fixture.PrepareBaseline();
		fixture.VerifyRead(nameof(BTreeLookupReadBenchmarks.GroupLookup), fixture.GroupLookup());
		fixture.DeleteAndRecycle();
		fixture.Mixed();
		fixture.Verify();
		fixture.Persist();
		if (storage is not (BTreeStorage.ManagedNodes or BTreeStorage.StreamMemory)) {
			fixture.ReopenAndLookup();
			fixture.Verify();
		}
		if (storage == BTreeStorage.TransactionalFile) {
			fixture.PrepareBaseline();
			var original = fixture.Scan();
			fixture.Update();
			fixture.RollbackPreparedChanges();
			Assert.That(fixture.Scan(), Is.EqualTo(original));
			fixture.Verify();
		}
	}

	[Test]
	public void CliSelectionAndSuitesUseTheSharedCoreEnum([Values] BTreeType implementation, [Values("--type", "--implementation")] string option) {
		var settings = ProfilerSettings.Parse(new[] { option, implementation.ToString(), "--order", "8", "--show-config" }, out var forwarded, out var show);
		Assert.That(show, Is.True);
		Assert.That(forwarded, Is.Empty);
		Assert.That(settings.Profiles.Single().Configuration.IndexBTreeSpec.Type, Is.EqualTo(implementation));
		Assert.That(settings.Profiles.Single().Configuration.IndexBTreeOrder, Is.EqualTo(8));
		var treeSettings = BTreeProfilerSettings.Parse(new[] { "--family", "Lookup", option, implementation.ToString(), "--order", "8", "--show-config" }, out forwarded, out show);
		Assert.That(show, Is.True);
		Assert.That(forwarded, Is.Empty);
		Assert.That(treeSettings.Profiles.Single().EffectiveType, Is.EqualTo(implementation));
		Assert.That(treeSettings.Profiles.Single().Order, Is.EqualTo(8));
		Assert.That(ObjectSpaceProfile.CreateSuite("implementation", ObjectSpaceLabStorage.Memory).Select(profile => profile.Configuration.IndexBTreeSpec.Type),
			Is.EqualTo(Enum.GetValues<BTreeType>()));
		Assert.That(BTreeProfile.CreateSuite("implementation", BTreeStorage.StreamMemory, BTreeFamily.Lookup, BTreeKeyShape.Int32).Select(profile => profile.EffectiveType),
			Is.EqualTo(Enum.GetValues<BTreeType>()));
	}

	[Test]
	public void TreeProfileJsonPreservesNewAndLegacySpecifications([Values] bool legacy, [Values] bool lowerCase, [Values] BTreeType type) {
		var root = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(root);
		var path = Path.Combine(root, "profiles.json");
		var profiles = new[] { new BTreeProfile { Name = "selected", Family = BTreeFamily.Lookup, Spec = new(type, 32) } };
		var json = JsonNode.Parse(JsonSerializer.Serialize(profiles, BTreeProfilerSettings.JsonOptions));
		if (legacy) {
			json[0].AsObject().Remove(nameof(BTreeProfile.Spec));
			json[0]["Order"] = 32;
			json[0]["Implementation"] = type.ToString();
		}
		File.WriteAllText(path, lowerCase ? json.ToJsonString().ToLowerInvariant() : json.ToJsonString());
		var settings = BTreeProfilerSettings.Parse(new[] { "--config", path, "--show-config" }, out _, out _);
		Assert.That(settings.Profiles.Single().Spec, Is.EqualTo(profiles.Single().Spec));
		var roundTrip = JsonSerializer.Deserialize<BTreeProfilerSettings>(JsonSerializer.Serialize(settings, BTreeProfilerSettings.JsonOptions), BTreeProfilerSettings.JsonOptions);
		Assert.That(roundTrip.Profiles.Single().Spec, Is.EqualTo(profiles.Single().Spec));
		using var published = JsonDocument.Parse(JsonSerializer.Serialize(settings, BTreeProfilerSettings.JsonOptions));
		Assert.That(published.RootElement.GetProperty(nameof(BTreeProfilerSettings.Profiles))[0].GetProperty("Order").GetInt32(), Is.EqualTo(32));
		Assert.That(published.RootElement.GetProperty(nameof(BTreeProfilerSettings.Profiles))[0].TryGetProperty("Implementation", out _), Is.False);
	}

	[Test]
	public void ObjectSpaceProfileJsonPreservesNewAndLegacySpecifications([Values] bool legacy, [Values] bool lowerCase, [Values] BTreeType type) {
		var root = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(root);
		var path = Path.Combine(root, "profiles.json");
		var profiles = ObjectSpaceProfile.CreateSuite("baseline", ObjectSpaceLabStorage.Memory);
		profiles.Single().Configuration.IndexBTreeSpec = new(type, 32);
		var json = JsonNode.Parse(JsonSerializer.Serialize(profiles, ProfilerSettings.JsonOptions));
		if (legacy) {
			json[0][nameof(ObjectSpaceProfile.Configuration)].AsObject().Remove(nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec));
			json[0][nameof(ObjectSpaceProfile.Configuration)]["IndexBTreeOrder"] = 32;
			json[0][nameof(ObjectSpaceProfile.Configuration)]["IndexBTreeImplementation"] = type.ToString();
		}
		File.WriteAllText(path, lowerCase ? json.ToJsonString().ToLowerInvariant() : json.ToJsonString());
		var settings = ProfilerSettings.Parse(new[] { "--config", path, "--show-config" }, out _, out _);
		Assert.That(settings.Profiles.Single().Configuration.IndexBTreeSpec, Is.EqualTo(profiles.Single().Configuration.IndexBTreeSpec));
		var roundTrip = JsonSerializer.Deserialize<ProfilerSettings>(JsonSerializer.Serialize(settings, ProfilerSettings.JsonOptions), ProfilerSettings.JsonOptions);
		Assert.That(roundTrip.Profiles.Single().Configuration.IndexBTreeSpec, Is.EqualTo(profiles.Single().Configuration.IndexBTreeSpec));
	}

	[Test]
	public void CliOrderOnlyPreservesTheConfiguredType([Values] BTreeType type) {
		var root = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(root);
		var path = Path.Combine(root, "profiles.json");
		var profiles = ObjectSpaceProfile.CreateSuite("baseline", ObjectSpaceLabStorage.Memory);
		profiles.Single().Configuration.IndexBTreeSpec = new(type, 32);
		File.WriteAllText(path, JsonSerializer.Serialize(profiles, ProfilerSettings.JsonOptions));
		var settings = ProfilerSettings.Parse(new[] { "--config", path, "--order", "8", "--show-config" }, out _, out _);
		Assert.That(settings.Profiles.Single().Configuration.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(type, 8)));
		Assert.That(profiles.Single().Configuration.IndexBTreeSpec, Is.EqualTo(new BTreeSpec(type, 32)));
	}

	[Test]
	public void OrderOnlyProfilesRetainTheirOrderWithoutATypeOverride([Values] BTreeFamily family) {
		var settings = BTreeProfilerSettings.Parse(new[] { "--family", family.ToString(), "--order", "8", "--show-config" }, out _, out _);
		var profile = settings.Profiles.Single();
		Assert.That(profile.Spec, Is.Null);
		Assert.That(profile.Order, Is.EqualTo(8));
		Assert.That(profile.EffectiveType, Is.EqualTo(family == BTreeFamily.BTree ? BTreeType.BTree : BTreeType.BTreePlus));
		var roundTrip = JsonSerializer.Deserialize<BTreeProfilerSettings>(JsonSerializer.Serialize(settings, BTreeProfilerSettings.JsonOptions), BTreeProfilerSettings.JsonOptions);
		Assert.That(roundTrip.Profiles.Single().Spec, Is.Null);
		Assert.That(roundTrip.Profiles.Single().Order, Is.EqualTo(8));

		var objectSpaces = ProfilerSettings.Parse(new[] { "--order", "8", "--show-config" }, out _, out _);
		Assert.That(objectSpaces.Profiles.Single().Configuration.IndexBTreeSpec, Is.Null);
		Assert.That(objectSpaces.Profiles.Single().Configuration.IndexBTreeOrder, Is.EqualTo(8));
		var spaceRoundTrip = JsonSerializer.Deserialize<ProfilerSettings>(JsonSerializer.Serialize(objectSpaces, ProfilerSettings.JsonOptions), ProfilerSettings.JsonOptions);
		Assert.That(spaceRoundTrip.Profiles.Single().Configuration.IndexBTreeSpec, Is.Null);
		Assert.That(spaceRoundTrip.Profiles.Single().Configuration.IndexBTreeOrder, Is.EqualTo(8));
	}

	[Test]
	public void ModernProfileRejectsANullTypeInsideAFullSpecification([Values] bool objectSpace) {
		var root = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(root);
		var path = Path.Combine(root, "profiles.json");
		File.WriteAllText(path, objectSpace
			? """[{"Name":"invalid","Configuration":{"IndexBTreeSpec":{"Type":null,"Order":8}}}]"""
			: """[{"Name":"invalid","Spec":{"Type":null,"Order":8}}]""");
		if (objectSpace)
			Assert.That(() => ProfilerSettings.Parse(new[] { "--config", path, "--show-config" }, out _, out _), Throws.TypeOf<JsonException>());
		else
			Assert.That(() => BTreeProfilerSettings.Parse(new[] { "--config", path, "--show-config" }, out _, out _), Throws.TypeOf<JsonException>());
	}

	[Test]
	public void ProfileInputsRejectConflictingLegacyAndCurrentTreeFields([Values] bool objectSpace, [Values] bool lowerCase) {
		var root = Tools.FileSystem.GetTempEmptyDirectory(true);
		using var cleanup = Tools.Scope.DeleteDirOnDispose(root);
		var path = Path.Combine(root, "profiles.json");
		var text = objectSpace
			? """[{"Name":"mixed","Configuration":{"IndexBTreeSpec":{"Type":"BTree","Order":32},"IndexBTreeOrder":8}}]"""
			: """[{"Name":"mixed","Spec":{"Type":"BTree","Order":32},"Order":8}]""";
		File.WriteAllText(path, lowerCase ? text.ToLowerInvariant() : text);
		if (objectSpace)
			Assert.That(() => ProfilerSettings.Parse(new[] { "--config", path, "--show-config" }, out _, out _), Throws.ArgumentException);
		else
			Assert.That(() => BTreeProfilerSettings.Parse(new[] { "--config", path, "--show-config" }, out _, out _), Throws.ArgumentException);
	}
}
