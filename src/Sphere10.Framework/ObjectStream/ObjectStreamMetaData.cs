// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework;

/// <summary>The item type, serializer choice and flat index settings for an ObjectStream.</summary>
public sealed class ObjectStreamMetaData {
	// Stream-local construction settings and its optional role within an ObjectSpace.
	/// <summary>The CLR type stored by this ObjectStream.</summary>
	public Type ItemType { get; set; }

	/// <summary>The item's original registration code in the applicable serializer factory.</summary>
	public long ItemTypeCode { get; set; }

	/// <summary>Whether item writes preallocate their calculated serialized size.</summary>
	public bool PreAllocateOptimization { get; set; }

	/// <summary>Whether the dimension is a root for ObjectSpace garbage collection.</summary>
	public bool IsRoot { get; set; }

	/// <summary>The mapped boolean dirty property, or null when no member-based change tracking is configured.</summary>
	public string ChangeTrackingMemberName { get; set; }

	// Index order matches the reserved attachment streams in the persisted container.
	/// <summary>The attachment definitions in their persisted physical order.</summary>
	public Index[] Indexes { get; set; } = Array.Empty<Index>();

	/// <summary>A standalone stream's factory; dimensions share the factory stored by their ObjectSpace.</summary>
	public SerializerFactory SerializerFactory { get; set; }

	/// <summary>Construction settings for one member index, recyclable-slot store or Merkle attachment.</summary>
	public sealed class Index {
		/// <summary>The attachment implementation to construct.</summary>
		public ObjectSpaceDefinition.IndexType Kind { get; set; }

		/// <summary>The attachment's unique name within the ObjectStream.</summary>
		public string Name { get; set; }

		/// <summary>The indexed public member's name, or null for attachments without a member.</summary>
		public string MemberName { get; set; }

		/// <summary>How member indexes handle null keys.</summary>
		public IndexNullPolicy NullPolicy { get; set; }

		/// <summary>The complete member-index layout; recyclable-slot and Merkle attachments do not have a B-tree.</summary>
		public BTreeSpec BTreeSpec { get; set; }

		/// <summary>The digest algorithm for Merkle attachments; unused by other index kinds.</summary>
		public CHF HashFunction { get; set; }
	}
}
