// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

public class ObjectSpaceLabConfiguration {

	[Category("Schema"), DisplayName("Dimensions"), Description("PayloadOnly preserves the profiler baseline. DeveloperLab adds Groups and Events with scalar relationships and their own indexes.")]
	public ObjectSpaceLabSchemaProfile SchemaProfile { get; set; } = ObjectSpaceLabSchemaProfile.PayloadOnly;

	[Browsable(false), Category("Storage"), DisplayName("Backend"), Description("Memory uses a caller-owned or created stream. File adds staged pages, commit, rollback and durable reopen.")]
	public ObjectSpaceLabStorage Storage { get; set; } = ObjectSpaceLabStorage.Memory;

	[Category("Indexes"), DisplayName("Index profile"), Description("IdentifierOnly indexes Id. Full also indexes unique Code and nonunique Category; compare their lookup and write costs.")]
	public ObjectSpaceLabIndexProfile IndexProfile { get; set; } = ObjectSpaceLabIndexProfile.Full;

	[Category("Indexes"), DisplayName("B-tree order"), Description("Maximum node child count, minimum 3, default 64. Larger order trades tree depth against node search, serialization and space. Preserve it on reopen.")]
	public int IndexBTreeOrder { get; set; } = Sphere10FrameworkDefaults.IndexBTreeOrder;

	[Category("Storage"), DisplayName("Outer cluster (bytes)"), Description("Allocation unit in the outer clustered stream. Compare with payload size and fragmentation; preserve layout on reopen.")]
	public int ClusterSize { get; set; } = 4096;

	[Category("Storage"), DisplayName("Inner cluster hint (bytes)"), Description("Selects the dimension's actual cluster size after clipping to 256–8192 bytes. This is separate from the outer cluster setting.")]
	public int AverageRecordSizeBytes { get; set; } = 1024;

	[Category("File storage"), DisplayName("Page size (bytes)"), Description("Transactional file-page granularity, affecting I/O and commit amplification. Has no memory-backend effect.")]
	public long PageSize { get; set; } = 64 * 1024;

	[Category("File storage"), DisplayName("Page cache (bytes)"), Description("Must hold at least one page. This bounds the file-page cache, not the identity cache or total process memory.")]
	public long MaxMemory { get; set; } = 16 * 1024 * 1024;

	[Category("Storage"), DisplayName("Outer stream policy"), Description("Outer stream allocation, descriptor/header caching and integrity flags. Inner dimension containers use their own FastAllocate policy.")]
	public ClusteredStreamsPolicy Policy { get; set; } = ClusteredStreamsPolicy.Default;

	[Category("Storage"), DisplayName("Byte order"), Description("Serialized byte order. Retain the same setting when reopening stored bytes.")]
	public Endianness Endianness { get; set; } = Endianness.LittleEndian;

	[Category("Integrity"), DisplayName("Merkle integrity"), Description("Install per-dimension and root integrity attachments. Flush/commit settles the root; benchmark the additional mutation cost.")]
	public bool Merkleized { get; set; }

	[Category("Integrity"), DisplayName("Hash algorithm")]
	[Description("Merkle integrity digest, default SHA2_256. Preserve it on reopen. Current member checksum indexes select their own default digest independently.")]
	public CHF HashFunction { get; set; } = Sphere10FrameworkDefaults.HashFunction;

	[Category("Tracking"), DisplayName("Dirty-flag autosave"), Description("Flush/commit saves tracked dirty objects. Disabled mode requires explicit Save; cached reads alone do not demonstrate persisted values.")]
	public bool AutoSave { get; set; }

	[JsonIgnore]
	[Category("Storage"), DisplayName("Effective inner cluster"), Description("Actual dimension cluster size in bytes, after the average-size hint is clipped to the recommended bounds.")]
	public int EffectiveDimensionClusterSize => Tools.Values.ClipValue(AverageRecordSizeBytes, 256, 8192);

	public ObjectSpaceLabConfiguration Clone() => (ObjectSpaceLabConfiguration)MemberwiseClone();

	public Result Validate() {
		var result = Result.Success;
		if (!Enum.IsDefined(Storage))
			result.AddError("Unknown storage backend.");
		if (!Enum.IsDefined(SchemaProfile))
			result.AddError("Unknown schema profile.");
		if (!Enum.IsDefined(IndexProfile))
			result.AddError("Unknown index profile.");
		if (IndexBTreeOrder < 3)
			result.AddError("Index B-tree order must be at least 3.");
		if (ClusterSize < 1)
			result.AddError("Outer cluster size must be positive.");
		if (AverageRecordSizeBytes < 1)
			result.AddError("Average record size must be positive.");
		if (PageSize < 1)
			result.AddError("File page size must be positive.");
		if (MaxMemory < PageSize)
			result.AddError("File page-cache budget must hold at least one page.");
		var supportedPolicy = ClusteredStreamsPolicy.FastAllocate | ClusteredStreamsPolicy.Default;
		if ((Policy & ~supportedPolicy) != 0)
			result.AddError("Unknown clustered-stream policy flags.");
		if (!Enum.IsDefined(Endianness))
			result.AddError("Unknown endianness.");
		if (!Enum.IsDefined(HashFunction) || (int)HashFunction < 0 || (int)HashFunction >= byte.MaxValue || Hashers.GetDigestSizeBytes(HashFunction) <= 0)
			result.AddError("The hash algorithm must have a registered, positive, fixed-size digest.");
		return result;
	}
}
