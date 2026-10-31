using System;
using System.Runtime.Serialization;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// Flags describing optional behaviors for an object space.
/// </summary>
[Flags]
public enum ObjectSpaceTraits {
	/// <summary>No optional ObjectSpace behavior flags are enabled.</summary>
	[EnumMember(Value = "none")]
	None = 0,

	/// <summary>
	/// Enables merkle-tree indexing for integrity verification.
	/// </summary>
	[EnumMember(Value = "merkleized")]
	Merkleized = 1 << 0,

	/// <summary>
	/// Automatically persists tracked changes without manual save calls.
	/// </summary>
	[EnumMember(Value = "autosave")]
	AutoSave = 1 << 1,

	/// <summary>
	/// Enables reference-counting garbage collection for non-root dimension objects.
	/// Objects in non-root dimensions with zero incoming references are automatically collected.
	/// </summary>
	[EnumMember(Value = "garbagecollect")]
	GarbageCollect = 1 << 2,

	/// <summary>
	/// Stores the ObjectSpace configuration and factory, and metadata for each dimension's ObjectStream.
	/// </summary>
	[EnumMember(Value = "metadata")]
	MetaData = 1 << 3,

	/// <summary>
	/// Uses big-endian metadata and storage payloads. Otherwise little-endian encoding is used.
	/// </summary>
	[EnumMember(Value = "bigendian")]
	BigEndian = 1 << 4,

}

