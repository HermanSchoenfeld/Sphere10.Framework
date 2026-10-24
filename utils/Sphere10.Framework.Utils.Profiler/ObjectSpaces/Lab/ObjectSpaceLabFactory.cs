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
using System.Text.Json.Serialization;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

public static class ObjectSpaceLabFactory {
	private const int SchemaVersion = 4;
	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		Converters = { new JsonStringEnumConverter() }
	};

	public static ObjectSpace Open(ObjectSpaceLabConfiguration configuration, string filePath = null, string pagesDirectory = null, MemoryStream memoryStream = null) {
		Guard.ArgumentNotNull(configuration, nameof(configuration));
		configuration = configuration.Clone();
		configuration.Validate().ThrowOnFailure();
		var builder = new ObjectSpaceBuilder()
			.UsingHashFunction(configuration.HashFunction)
			.WithClusterSize(configuration.ClusterSize)
			.WithPageSize(configuration.PageSize)
			.WithMaxMemory(configuration.MaxMemory)
			.WithClusteredStreamsPolicy(configuration.Policy)
			.WithEndianness(configuration.Endianness);
		if (configuration.IndexBTreeSpec == null)
			builder.WithIndexBTreeOrder(configuration.IndexBTreeOrder);
		else
			builder.WithIndexBTreeSpec(configuration.IndexBTreeSpec);
		if (configuration.AutoSave)
			builder.AutoSave();
		var dimension = builder.AddDimension<ObjectSpaceLabRecord>(ignoreAnnotations: true)
			.WithIdentifier(record => record.Id)
			.WithChangeTrackingVia(record => record.Dirty)
			.OptimizeAssumingAverageItemSize(configuration.AverageRecordSizeBytes);
		if (configuration.IndexProfile == ObjectSpaceLabIndexProfile.Full) {
			dimension
				.WithUniqueIndexOn(record => record.Code)
				.WithIndexOn(record => record.Category);
		}
		dimension.Done();
		if (configuration.SchemaProfile == ObjectSpaceLabSchemaProfile.DeveloperLab) {
			var groups = builder.AddDimension<ObjectSpaceLabGroup>(ignoreAnnotations: true)
				.WithIdentifier(group => group.Id)
				.WithChangeTrackingVia(group => group.Dirty)
				.OptimizeAssumingAverageItemSize(configuration.AverageRecordSizeBytes);
			if (configuration.IndexProfile == ObjectSpaceLabIndexProfile.Full)
				groups.WithUniqueIndexOn(group => group.Code);
			groups.Done();
			var events = builder.AddDimension<ObjectSpaceLabEvent>(ignoreAnnotations: true)
				.WithIdentifier(item => item.Id)
				.WithChangeTrackingVia(item => item.Dirty)
				.OptimizeAssumingAverageItemSize(configuration.AverageRecordSizeBytes);
			if (configuration.IndexProfile == ObjectSpaceLabIndexProfile.Full) {
				events
					.WithIndexOn(item => item.RecordId)
					.WithIndexOn(item => item.Kind);
			}
			events.Done();
		}
		if (configuration.Merkleized)
			builder.Merkleized();

		if (configuration.Storage == ObjectSpaceLabStorage.Memory) {
			Guard.Argument(filePath == null && pagesDirectory == null, nameof(filePath), "Memory storage does not accept file paths.");
			return builder.UseMemoryStream(memoryStream).Build();
		}

		Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
		Guard.Argument(memoryStream == null, nameof(memoryStream), "File storage does not accept a memory stream.");
		filePath = Path.GetFullPath(filePath);
		var manifestPath = GetManifestPath(filePath);
		if (File.Exists(manifestPath)) {
			var recorded = ReadConfiguration(filePath);
			Guard.Ensure(JsonSerializer.Serialize(recorded, JsonOptions) == JsonSerializer.Serialize(configuration, JsonOptions),
				"The requested configuration differs from this lab file's recorded layout. Read its configuration or create a new workspace.");
		} else {
			Guard.Ensure(!File.Exists(filePath) || new FileInfo(filePath).Length == 0,
				"An existing ObjectSpace lab file requires its .objectspace.json configuration sidecar. Unknown storage will not be opened.");
		}
		Tools.FileSystem.CreateDirectory(Path.GetDirectoryName(filePath));
		pagesDirectory ??= filePath + ".pages";
		Tools.FileSystem.CreateDirectory(pagesDirectory);
		var space = builder
			.UseFile(filePath)
			.KeepUncommittedPagesAt(pagesDirectory)
			.Build();
		try {
			space.Load();
			if (!File.Exists(manifestPath)) {
				using var manifestStream = new FileStream(manifestPath, FileMode.CreateNew, FileAccess.Write);
				JsonSerializer.Serialize(manifestStream, new LabManifest { SchemaVersion = SchemaVersion, Configuration = configuration }, JsonOptions);
			}
			return space;
		} catch {
			space.Dispose();
			throw;
		}
	}

	public static ObjectSpaceLabConfiguration ReadConfiguration(string filePath) {
		Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
		using var stream = new FileStream(GetManifestPath(filePath), FileMode.Open, FileAccess.Read, FileShare.Read);
		using var document = JsonDocument.Parse(stream);
		Guard.Ensure(document.RootElement.TryGetProperty(nameof(LabManifest.Configuration), out var configuration) && configuration.ValueKind == JsonValueKind.Object,
			"The ObjectSpace lab manifest requires a complete Configuration object.");
		var schemaVersion = 0;
		Guard.Ensure(document.RootElement.TryGetProperty(nameof(LabManifest.SchemaVersion), out var version) && version.TryGetInt32(out schemaVersion)
			&& schemaVersion is 1 or 2 or 3 or SchemaVersion, "Unsupported ObjectSpace lab manifest schema.");
		configuration.TryGetProperty(nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec), out var specification);
		var hasSpecification = specification.ValueKind == JsonValueKind.Object;
		foreach (var property in typeof(ObjectSpaceLabConfiguration).GetProperties().Where(property => property.CanWrite
			&& (schemaVersion != 1 || property.Name != nameof(ObjectSpaceLabConfiguration.SchemaProfile))
			&& property.Name != nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec)
			&& (!hasSpecification || property.Name != nameof(ObjectSpaceLabConfiguration.IndexBTreeOrder))))
			Guard.Ensure(configuration.TryGetProperty(property.Name, out _), $"The ObjectSpace lab manifest is missing configuration field '{property.Name}'.");
		BTreeSpec legacySpec = null;
		if (schemaVersion < 4) {
			var legacyOrder = 0;
			Guard.Ensure(configuration.TryGetProperty("IndexBTreeOrder", out var order) && order.TryGetInt32(out legacyOrder),
				"The ObjectSpace lab manifest is missing configuration field 'IndexBTreeOrder'.");
			var hasType = configuration.TryGetProperty("IndexBTreeImplementation", out var implementation);
			Guard.Ensure(schemaVersion < 3 || hasType, "The ObjectSpace lab manifest is missing configuration field 'IndexBTreeImplementation'.");
			var legacyType = hasType ? implementation.Deserialize<BTreeType?>(JsonOptions) : null;
			Guard.Ensure(schemaVersion >= 3 || !legacyType.HasValue,
				"Version 1 and 2 manifests only support the historical mixed B-tree implementations.");
			Guard.Ensure(!configuration.TryGetProperty(nameof(ObjectSpaceLabConfiguration.IndexBTreeSpec), out _),
				"Version 1, 2 and 3 manifests require the legacy B-tree configuration fields.");
			if (legacyType.HasValue)
				legacySpec = new BTreeSpec(legacyType.Value, legacyOrder);
		} else {
			Guard.Ensure(!hasSpecification || specification.TryGetProperty(nameof(BTreeSpec.Type), out _)
				&& specification.TryGetProperty(nameof(BTreeSpec.Order), out _), "The ObjectSpace lab manifest requires a complete IndexBTreeSpec object.");
			Guard.Ensure(specification.ValueKind is JsonValueKind.Object or JsonValueKind.Null or JsonValueKind.Undefined,
				"The ObjectSpace lab manifest requires a complete IndexBTreeSpec object.");
			Guard.Ensure(!configuration.TryGetProperty("IndexBTreeImplementation", out _)
				&& (!hasSpecification || !configuration.TryGetProperty(nameof(ObjectSpaceLabConfiguration.IndexBTreeOrder), out var order)
					|| order.GetInt32() == specification.GetProperty(nameof(BTreeSpec.Order)).GetInt32()),
				"The index B-tree specification conflicts with legacy configuration fields.");
		}
		var manifest = document.RootElement.Deserialize<LabManifest>(JsonOptions);
		Guard.Ensure(manifest?.Configuration != null, "Incomplete ObjectSpace lab manifest.");
		if (legacySpec != null)
			manifest.Configuration.IndexBTreeSpec = legacySpec;
		if (schemaVersion == 1) {
			Guard.Ensure(!configuration.TryGetProperty(nameof(ObjectSpaceLabConfiguration.SchemaProfile), out var profile)
				|| profile.GetString() == nameof(ObjectSpaceLabSchemaProfile.PayloadOnly), "Version 1 only supports the original payload dimension.");
			manifest.Configuration.SchemaProfile = ObjectSpaceLabSchemaProfile.PayloadOnly;
		}
		manifest.Configuration.Validate().ThrowOnFailure();
		Guard.Ensure(manifest.Configuration.Storage == ObjectSpaceLabStorage.File, "A file workspace must declare file storage.");
		return manifest.Configuration.Clone();
	}

	public static ObjectSpaceLabRecord CreateRecord(long id, int payloadBytes = 128) {
		Guard.ArgumentGT(id, 0, nameof(id));
		Guard.ArgumentGTE(payloadBytes, 0, nameof(payloadBytes));
		var payload = Tools.Collection.GenerateArray(payloadBytes, index => (byte)((id + index) % 251));
		return new ObjectSpaceLabRecord {
			Id = id,
			Code = $"REC-{id:D8}",
			Category = (int)((id - 1) % 16),
			Balance = id * 1.25m,
			Payload = payload,
			Notes = $"Deterministic record {id}"
		};
	}

	public static ObjectSpaceLabGroup CreateGroup(int id) => new() { Id = id, Code = $"GRP-{id:D4}", Name = $"Group {id}" };

	public static ObjectSpaceLabEvent CreateEvent(long id, long recordId = 0) => new() {
		Id = id, RecordId = recordId, Kind = (int)(id % 4), TimestampTicks = DateTime.UnixEpoch.AddSeconds(id).Ticks, Message = $"Event {id}"
	};

	public static void Seed(ObjectSpace space, int count, int payloadBytes = 128) {
		Guard.ArgumentNotNull(space, nameof(space));
		Guard.ArgumentGTE(count, 0, nameof(count));
		using (space.EnterAccessScope()) {
			Guard.Ensure(space.Count<ObjectSpaceLabRecord>() == 0, "Seed requires an empty ObjectSpace.");
			for (var index = 0; index < count; index++) {
				var record = CreateRecord(index + 1L, payloadBytes);
				space.AcceptNew(record);
				space.Save(record);
			}
		}
	}

	public static void Complete(ObjectSpace space) {
		Guard.ArgumentNotNull(space, nameof(space));
		using (space.EnterAccessScope()) {
			if (space is FileObjectSpace file)
				file.Commit();
			else
				space.Flush();
		}
	}

	public static string GetManifestPath(string filePath) => Path.GetFullPath(filePath) + ".objectspace.json";

	private sealed class LabManifest {
		public int SchemaVersion { get; set; }

		public ObjectSpaceLabConfiguration Configuration { get; set; }
	}
}
