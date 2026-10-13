// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces;

public sealed class ObjectSpaceProfile {
	public string Name { get; set; }

	public ObjectSpaceLabConfiguration Configuration { get; set; }

	public string ComparisonGroup => $"{Configuration.Storage}/{Configuration.SchemaProfile}/{Configuration.IndexProfile}/checks-{Configuration.Policy.HasFlag(ClusteredStreamsPolicy.IntegrityChecks)}/"
		+ (Configuration.Merkleized ? $"merkle-{Configuration.HashFunction}" : "no-merkle");

	public void Validate() {
		Guard.ArgumentNotNullOrWhitespace(Name, nameof(Name));
		Guard.Argument(Name.All(character => char.IsAsciiLetterOrDigit(character) || character == '-'), nameof(Name), "Use letters, digits and hyphens in profile names.");
		Guard.ArgumentNotNull(Configuration, nameof(Configuration));
		Configuration.Validate().ThrowOnFailure();
	}

	public static ObjectSpaceProfile[] CreateSuite(string suite, ObjectSpaceLabStorage storage) {
		Guard.Argument(suite is "baseline" or "framework-defaults" or "btree" or "btree-refinement" or "clusters" or "pages" or "cache" or "policies" or "features" or "hashes" or "indexes" or "endianness", nameof(suite), $"Unknown suite '{suite}'.");
		// Sweep one factor at a time before investigating interactions with custom profiles.
		var profiles = new List<ObjectSpaceProfile>();
		void Add(string name, Action<ObjectSpaceLabConfiguration> configure = null) {
			var configuration = new ObjectSpaceLabConfiguration {
				Storage = storage,
				IndexProfile = ObjectSpaceLabIndexProfile.Full,
				IndexBTreeOrder = 64,
				ClusterSize = 4096,
				AverageRecordSizeBytes = 256,
				PageSize = 262144,
				MaxMemory = 16777216,
				Policy = ClusteredStreamsPolicy.Default,
				Endianness = Endianness.LittleEndian
			};
			configure?.Invoke(configuration);
			profiles.Add(new ObjectSpaceProfile { Name = name, Configuration = configuration });
		}
		switch (suite) {
			case "baseline":
				Add("baseline");
				break;
			case "framework-defaults":
				Add("framework-defaults", configuration => {
					configuration.IndexBTreeOrder = Sphere10FrameworkDefaults.ObjectSpaceIndexBTreeOrder;
					configuration.ClusterSize = storage == ObjectSpaceLabStorage.Memory
						? Sphere10FrameworkDefaults.MemoryObjectSpaceClusterSize : Sphere10FrameworkDefaults.FileObjectSpaceClusterSize;
					configuration.AverageRecordSizeBytes = storage == ObjectSpaceLabStorage.Memory
						? Sphere10FrameworkDefaults.MemoryObjectSpaceDimensionClusterSize : Sphere10FrameworkDefaults.FileObjectSpaceDimensionClusterSize;
					configuration.Policy = storage == ObjectSpaceLabStorage.Memory
						? Sphere10FrameworkDefaults.MemoryObjectSpaceContainerPolicy : Sphere10FrameworkDefaults.FileObjectSpaceContainerPolicy;
					configuration.PageSize = Sphere10FrameworkDefaults.ObjectSpacePageSize;
					configuration.MaxMemory = Sphere10FrameworkDefaults.ObjectSpaceMaxMemory;
					configuration.Endianness = Sphere10FrameworkDefaults.Endianness;
					configuration.HashFunction = Sphere10FrameworkDefaults.HashFunction;
					configuration.AutoSave = false;
					configuration.Merkleized = false;
				});
				break;
			case "btree":
				foreach (var order in new[] { 8, 16, 32, 64, 128, 256, 512 })
					Add($"order-{order}", configuration => configuration.IndexBTreeOrder = order);
				break;
			case "btree-refinement":
				foreach (var order in new[] { 169, 170 })
					Add($"order-{order}", configuration => configuration.IndexBTreeOrder = order);
				break;
			case "clusters":
				foreach (var bytes in new[] { 256, 1024, 4096, 8192 })
					Add($"outer-{bytes}", configuration => configuration.ClusterSize = bytes);
				foreach (var bytes in new[] { 256, 1024, 4096, 8192 })
					Add($"inner-{bytes}", configuration => configuration.AverageRecordSizeBytes = bytes);
				break;
			case "pages":
				Guard.Argument(storage == ObjectSpaceLabStorage.File, nameof(storage), "The pages suite requires file storage.");
				foreach (var bytes in new[] { 4096, 16384, 65536, 262144, 1048576 })
					Add($"page-{bytes}", configuration => configuration.PageSize = bytes);
				break;
			case "cache":
				Guard.Argument(storage == ObjectSpaceLabStorage.File, nameof(storage), "The cache suite requires file storage.");
				foreach (var bytes in new[] { 262144, 1048576, 4194304, 16777216, 67108864 })
					Add($"cache-{bytes}", configuration => configuration.MaxMemory = bytes);
				break;
			case "policies":
				Add("default");
				Add("fast-allocate", configuration => configuration.Policy = ClusteredStreamsPolicy.Default | ClusteredStreamsPolicy.FastAllocate);
				Add("performance", configuration => configuration.Policy = ClusteredStreamsPolicy.Performance);
				Add("headers-only", configuration => configuration.Policy = ClusteredStreamsPolicy.CacheClusterHeaders | ClusteredStreamsPolicy.IntegrityChecks);
				Add("descriptors-only", configuration => configuration.Policy = ClusteredStreamsPolicy.CacheDescriptors | ClusteredStreamsPolicy.IntegrityChecks);
				Add("no-caches", configuration => configuration.Policy = ClusteredStreamsPolicy.IntegrityChecks);
				break;
			case "features":
				Add("explicit-save");
				Add("autosave", configuration => configuration.AutoSave = true);
				Add("merkle", configuration => configuration.Merkleized = true);
				Add("merkle-autosave", configuration => { configuration.Merkleized = true; configuration.AutoSave = true; });
				break;
			case "hashes":
				foreach (var hashFunction in new[] { CHF.SHA2_256, CHF.Blake2b_256, CHF.Blake2b_128 })
					Add(hashFunction.ToString().ToLowerInvariant().Replace('_', '-'), configuration => {
						configuration.HashFunction = hashFunction;
						configuration.Merkleized = true;
					});
				break;
			case "indexes":
				Add("identifier-only", configuration => configuration.IndexProfile = ObjectSpaceLabIndexProfile.IdentifierOnly);
				Add("full-indexes");
				break;
			case "endianness":
				Add("little-endian");
				Add("big-endian", configuration => configuration.Endianness = Endianness.BigEndian);
				break;
		}
		return profiles.ToArray();
	}
}
