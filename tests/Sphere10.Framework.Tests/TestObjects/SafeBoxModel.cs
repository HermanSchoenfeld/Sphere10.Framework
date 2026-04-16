// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests;

/// <summary>
/// PascalCoin SafeBox-inspired object model for ObjectSpace integration testing.
/// Exercises cross-dimension references, arrays of dimension objects, back-references,
/// unique indexes, and polymorphic identity hierarchies.
/// </summary>

// ── Identity (base concept) ─────────────────────────────────────────────────
/// <summary>
/// Base identity dimension: users, groups, and plain identities share this type.
/// IdentityType discriminator distinguishes subtypes stored in a single dimension.
/// </summary>
[Root]
public class SafeBoxIdentity {
	[Identity]
	public string Name { get; set; }

	public byte[] PublicKey { get; set; }

	public SafeBoxIdentityType IdentityType { get; set; }

	/// <summary>Email for users, null for plain identities / groups.</summary>
	public string Email { get; set; }

	/// <summary>Display name for users.</summary>
	public string DisplayName { get; set; }

	/// <summary>
	/// Member identity names when <see cref="IdentityType"/> is Group.
	/// Stores the Name strings of member identities (use ObjectSpace.Get to resolve).
	/// </summary>
	public string[] MemberNames { get; set; }

	[Transient]
	public bool Dirty { get; set; }
}

public enum SafeBoxIdentityType : byte {
	Identity = 0,
	User = 1,
	Group = 2
}

// ── Permission ──────────────────────────────────────────────────────────────
/// <summary>
/// Permission granted to a group (or identity). Links to the owning group/identity dimension.
/// </summary>
public class SafeBoxPermission {
	[Identity]
	public string PermissionName { get; set; }

	public string Description { get; set; }

	/// <summary>The identity (usually a group) this permission is granted to.</summary>
	public SafeBoxIdentity GrantedTo { get; set; }

	[Transient]
	public bool Dirty { get; set; }
}

// ── Account ─────────────────────────────────────────────────────────────────
/// <summary>
/// PascalCoin-style account with balance, unique account number, and an owning identity.
/// </summary>
[Root]
public class SafeBoxAccount {
	[Identity]
	public long AccountNumber { get; set; }

	public string Name { get; set; }

	public decimal Balance { get; set; }

	/// <summary>The identity that owns this account.</summary>
	public SafeBoxIdentity Owner { get; set; }

	[Transient]
	public bool Dirty { get; set; }
}

// ── Transaction ─────────────────────────────────────────────────────────────
/// <summary>
/// A single value transfer between two accounts, belonging to a block.
/// Contains a back-reference to its owner block.
/// </summary>
public class SafeBoxTransaction {
	[Identity]
	public string TxHash { get; set; }

	public decimal Amount { get; set; }

	/// <summary>Sender account reference (cross-dimension).</summary>
	public SafeBoxAccount Sender { get; set; }

	/// <summary>Receiver account reference (cross-dimension).</summary>
	public SafeBoxAccount Receiver { get; set; }

	/// <summary>Back-reference to the block that contains this transaction.</summary>
	public SafeBoxBlock OwnerBlock { get; set; }

	[Transient]
	public bool Dirty { get; set; }
}

// ── Block ───────────────────────────────────────────────────────────────────
/// <summary>
/// A block containing an array of transactions, chained by hash.
/// The Transaction[] exercises serialization of arrays of external-reference dimension objects.
/// </summary>
[Root]
public class SafeBoxBlock {
	[Identity]
	public long Height { get; set; }

	public DateTime Timestamp { get; set; }

	public byte[] PreviousBlockHash { get; set; }

	/// <summary>Array of transactions in this block (each is a dimension object).</summary>
	public SafeBoxTransaction[] Transactions { get; set; }

	[Transient]
	public bool Dirty { get; set; }
}
