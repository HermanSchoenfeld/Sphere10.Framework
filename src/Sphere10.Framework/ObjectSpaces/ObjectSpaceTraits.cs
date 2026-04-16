using System;
using System.Runtime.Serialization;

namespace Sphere10.Framework.ObjectSpaces;

/// <summary>
/// Flags describing optional behaviors for an object space.
/// </summary>
[Flags]
public enum ObjectSpaceTraits {
	[EnumMember(Value = "none")]
	None = 0,

	[EnumMember(Value = "merkleized")]
	/// <summary>
	/// Enables merkle-tree indexing for integrity verification.
	/// </summary>
	Merkleized = 1 << 0,

	[EnumMember(Value = "autosave")]
	/// <summary>
	/// Automatically persists tracked changes without manual save calls.
	/// </summary>
	AutoSave = 1 << 1,

	[EnumMember(Value = "garbagecollect")]
	/// <summary>
	/// Enables reference-counting garbage collection for non-root dimension objects.
	/// Objects in non-root dimensions with zero incoming references are automatically collected.
	/// </summary>
	GarbageCollect = 1 << 2,

}

