// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Generators;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests.SafeBox;

/// <summary>
/// PascalCoin SafeBox-inspired object model for ObjectSpace integration testing.
/// Exercises cross-dimension references, polymorphic arrays, back-references,
/// unique indexes, and a proper User/Group/Identity class hierarchy.
/// </summary>

// ── SafeBoxObject (common base) ─────────────────────────────────────────────
/// <summary>
/// Common base class for all SafeBox domain objects.
/// Contains the transient change-tracking flag used by ObjectSpace.
/// The <see cref="AutoDirtyAttribute"/> causes the source generator to implement
/// every <c>partial</c> property so that setting a new value automatically marks
/// the object dirty.
/// </summary>
[AutoDirty(nameof(Dirty))]
public abstract partial class SafeBoxObject {
	[Transient]
	public bool Dirty { get; set; }
}

// ── Identity hierarchy ──────────────────────────────────────────────────────

/// <summary>
/// Base identity dimension. A plain identity has a unique name and public key.
/// <see cref="User"/> and <see cref="Group"/> are subclasses stored in their own dimensions.
/// </summary>
[Root]
public partial class Identity : SafeBoxObject {
	[Identity]
	public partial string Name { get; set; }

	public partial byte[] PublicKey { get; set; }
}

/// <summary>
/// A user identity — extends <see cref="Identity"/> with email and display name.
/// Stored in its own dimension so the framework can track it independently.
/// </summary>
[Root]
public partial class User : Identity {
	public partial string Email { get; set; }

	public partial string DisplayName { get; set; }
}

/// <summary>
/// A group identity — extends <see cref="Identity"/> with a polymorphic array of member
/// identities. Each member in <see cref="Members"/> may be an <see cref="Identity"/>,
/// <see cref="User"/>, or another <see cref="Group"/>.
/// </summary>
[Root]
public partial class Group : Identity {
	/// <summary>
	/// Polymorphic array of member identities. Each element can be an <see cref="Identity"/>,
	/// <see cref="User"/>, or <see cref="Group"/>, exercising cross-dimension reference
	/// serialization within a base-typed array.
	/// </summary>
	public partial Identity[] Members { get; set; }
}

// ── Permission ──────────────────────────────────────────────────────────────
/// <summary>
/// Permission granted to an identity (usually a group). Cross-dimension reference to
/// the <see cref="Identity"/> hierarchy.
/// </summary>
public partial class Permission : SafeBoxObject {
	[Identity]
	public partial string PermissionName { get; set; }

	public partial string Description { get; set; }

	/// <summary>The identity (usually a group) this permission is granted to.</summary>
	public partial Identity GrantedTo { get; set; }
}

// ── Account ─────────────────────────────────────────────────────────────────
/// <summary>
/// PascalCoin-style account with balance, unique account number, and an owning identity.
/// </summary>
[Root]
public partial class Account : SafeBoxObject {
	[Identity]
	public partial long AccountNumber { get; set; }

	public partial string Name { get; set; }

	public partial decimal Balance { get; set; }

	/// <summary>The identity that owns this account (may be a User, Group, or plain Identity).</summary>
	public partial Identity Owner { get; set; }
}

// ── Transaction ─────────────────────────────────────────────────────────────
/// <summary>
/// A single value transfer between two accounts, belonging to a block.
/// Contains a back-reference to its owner block.
/// </summary>
public partial class Transaction : SafeBoxObject {
	[Identity]
	public partial string TxHash { get; set; }

	public partial decimal Amount { get; set; }

	/// <summary>Sender account reference (cross-dimension).</summary>
	public partial Account Sender { get; set; }

	/// <summary>Receiver account reference (cross-dimension).</summary>
	public partial Account Receiver { get; set; }

	/// <summary>Back-reference to the block that contains this transaction.</summary>
	public partial Block OwnerBlock { get; set; }
}

// ── Block ───────────────────────────────────────────────────────────────────
/// <summary>
/// A block containing an array of transactions, chained by hash.
/// The Transaction[] exercises serialization of arrays of external-reference dimension objects.
/// </summary>
[Root]
public partial class Block : SafeBoxObject {
	[Identity]
	public partial long Height { get; set; }

	public partial DateTime Timestamp { get; set; }

	public partial byte[] PreviousBlockHash { get; set; }

	/// <summary>Array of transactions in this block (each is a dimension object).</summary>
	public partial Transaction[] Transactions { get; set; }
}
