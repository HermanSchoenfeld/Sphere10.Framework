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

namespace Sphere10.Framework.Utils.Profiler.BTrees;

public enum BTreeStorage {
	ManagedNodes,
	StreamMemory,
	FileStream,
	FilePaged,
	OSMemoryMapped,
	TransactionalFile
}

public enum BTreeFamily {
	BTree,
	BTreePlus,
	Lookup
}

public enum BTreeKeyShape {
	Int32,
	Int64,
	Digest32
}

public sealed class BTreeProfile {
	public string Name { get; set; }

	public BTreeStorage Storage { get; set; } = BTreeStorage.FileStream;

	public BTreeFamily Family { get; set; } = BTreeFamily.BTreePlus;

	public BTreeKeyShape KeyShape { get; set; }

	public int Order { get; set; } = 64;

	public long PageSize { get; set; } = 65536;

	public long CacheBytes { get; set; } = 16777216;

	public int FileBufferBytes { get; set; } = 4096;

	public Endianness Endianness { get; set; } = Endianness.LittleEndian;

	public int KeyBytes => KeyShape switch { BTreeKeyShape.Int32 => 4, BTreeKeyShape.Int64 => 8, _ => 32 };

	public int StoredKeyBytes => KeyBytes + (Family == BTreeFamily.Lookup ? sizeof(int) : 0);

	public int NodeRecordBytes => (Family == BTreeFamily.BTree ? 9 : 17) + Order * (StoredKeyBytes + sizeof(long)) + (Order + 1) * sizeof(long);

	public void Validate() {
		Guard.ArgumentNotNullOrWhitespace(Name, nameof(Name));
		Guard.Argument(Name.All(character => char.IsAsciiLetterOrDigit(character) || character == '-'), nameof(Name), "Use letters, digits and hyphens in profile names.");
		Guard.Argument(Enum.IsDefined(Storage) && Enum.IsDefined(Family) && Enum.IsDefined(KeyShape) && Enum.IsDefined(Endianness), nameof(Storage), "Unknown tree, storage or key layout.");
		Guard.ArgumentInRange(Order, 3, 16384, nameof(Order));
		Guard.ArgumentInRange(PageSize, 4096L, 16777216L, nameof(PageSize));
		Guard.ArgumentGTE(CacheBytes, PageSize, nameof(CacheBytes));
		Guard.ArgumentInRange(FileBufferBytes, 1, 1048576, nameof(FileBufferBytes));
	}

	public static BTreeProfile[] CreateSuite(string suite, BTreeStorage storage, BTreeFamily family, BTreeKeyShape keyShape) {
		var profiles = new List<BTreeProfile>();
		void Add(string name, int order = 64, BTreeStorage? backend = null, BTreeFamily? tree = null, long page = 65536, long cache = 16777216, int buffer = 4096) {
			profiles.Add(new BTreeProfile { Name = name, Storage = backend ?? storage, Family = tree ?? family, KeyShape = keyShape,
				Order = order, PageSize = page, CacheBytes = cache, FileBufferBytes = buffer });
		}
		switch (suite) {
			case "baseline": Add("baseline"); break;
			case "framework-defaults":
				var frameworkOrder = (storage == BTreeStorage.ManagedNodes, family) switch {
					(true, BTreeFamily.BTree) => Sphere10FrameworkDefaults.BTreeOrder,
					(true, BTreeFamily.BTreePlus) => Sphere10FrameworkDefaults.BTreePlusOrder,
					(true, BTreeFamily.Lookup) => Sphere10FrameworkDefaults.BTreeLookupOrder,
					(false, BTreeFamily.BTree) => Sphere10FrameworkDefaults.StreamMappedBTreeOrder,
					(false, BTreeFamily.BTreePlus) => Sphere10FrameworkDefaults.StreamMappedBTreePlusOrder,
					_ => Sphere10FrameworkDefaults.StreamMappedBTreeLookupOrder
				};
				Add("framework-defaults", frameworkOrder, page: Sphere10FrameworkDefaults.TransactionalPageSize,
					cache: Sphere10FrameworkDefaults.MaxMemoryPerCollection);
				break;
			case "wide-keys":
				Guard.Argument(storage != BTreeStorage.ManagedNodes && family is BTreeFamily.BTreePlus or BTreeFamily.Lookup && keyShape == BTreeKeyShape.Digest32,
					nameof(suite), "The wide-key preset requires a serialized B+ or lookup tree with Digest32 keys.");
				Add("wide-keys", family == BTreeFamily.BTreePlus
					? Sphere10FrameworkDefaults.StreamMappedWideKeyBTreePlusOrder : Sphere10FrameworkDefaults.StreamMappedWideKeyBTreeLookupOrder,
					page: Sphere10FrameworkDefaults.TransactionalPageSize, cache: Sphere10FrameworkDefaults.MaxMemoryPerCollection);
				break;
			case "order":
				foreach (var order in new[] { 8, 16, 32, 64, 128, 256, 512 })
					Add($"order-{order}", order);
				break;
			case "storage":
				foreach (var backend in Enum.GetValues<BTreeStorage>())
					Add(backend.ToString().ToLowerInvariant(), backend: backend);
				break;
			case "family":
				foreach (var tree in Enum.GetValues<BTreeFamily>())
					Add(tree.ToString().ToLowerInvariant(), tree: tree);
				break;
			case "pages":
				Guard.Argument(storage is BTreeStorage.FilePaged or BTreeStorage.TransactionalFile, nameof(storage), "Page profiles require a framework paged backend.");
				foreach (var page in new[] { 4096, 16384, 65536, 262144, 1048576 })
					Add($"page-{page}", page: page);
				break;
			case "cache":
				Guard.Argument(storage is BTreeStorage.FilePaged or BTreeStorage.TransactionalFile, nameof(storage), "Cache profiles require a framework paged backend.");
				foreach (var cache in new[] { 65536, 262144, 1048576, 4194304, 16777216 })
					Add($"cache-{cache}", cache: cache);
				break;
			case "buffers":
				Guard.Argument(storage == BTreeStorage.FileStream, nameof(storage), "Buffer profiles require FileStream.");
				foreach (var buffer in new[] { 1, 4096, 16384, 65536 })
					Add($"buffer-{buffer}", buffer: buffer);
				break;
			default: throw new ArgumentException($"Unknown B-tree suite '{suite}'.", nameof(suite));
		}
		return profiles.ToArray();
	}
}
