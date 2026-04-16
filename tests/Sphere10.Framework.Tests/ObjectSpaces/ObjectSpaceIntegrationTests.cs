// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;
using SB = Sphere10.Framework.Tests.SafeBox;

namespace Sphere10.Framework.Tests.ObjectSpaces;

/// <summary>
/// Comprehensive ObjectSpace integration tests using the PascalCoin SafeBox-inspired model.
/// Covers individual CRUD per dimension type, cross-dimension references, unique key lookups,
/// polymorphic Identity/User/Group hierarchy with real subclass arrays,
/// back-references (Block ↔ Transaction), and complex object graph creation loops
/// with count/key verification and insert/update/delete iterations.
/// </summary>
[TestFixture]
public class ObjectSpaceIntegrationTests {

	#region Activation

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Activation_DoesNotThrow(TestTraits traits) {
		Assert.That(() => { using var _ = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits); }, Throws.Nothing);
	}

	#endregion

	#region Identity CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_CreateAndSave(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id = os.New<SB.Identity>();
		id.Name = "generic-id";
		id.PublicKey = new byte[] { 0xAA, 0xBB };
		os.Save(id);
		Assert.That(os.Count<SB.Identity>(), Is.EqualTo(1));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_LookupByUniqueName(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id = os.New<SB.Identity>();
		id.Name = "bob";
		id.PublicKey = new byte[] { 0x01 };
		os.Save(id);

		var fetched = os.Get((SB.Identity x) => x.Name, "bob");
		Assert.That(fetched, Is.SameAs(id));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_UniqueNameProhibitsDuplicate(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id1 = os.New<SB.Identity>();
		id1.Name = "dup";
		os.Save(id1);

		var id2 = os.New<SB.Identity>();
		id2.Name = "dup";
		Assert.That(() => os.Save(id2), Throws.InvalidOperationException);
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_Delete(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id = os.New<SB.Identity>();
		id.Name = "todelete";
		os.Save(id);
		Assert.That(os.Count<SB.Identity>(), Is.EqualTo(1));

		os.Delete(id);
		Assert.That(os.Count<SB.Identity>(), Is.EqualTo(0));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_Update(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id = os.New<SB.Identity>();
		id.Name = "updatable";
		id.PublicKey = new byte[] { 0x01 };
		os.Save(id);

		id.PublicKey = new byte[] { 0x02, 0x03 };
		os.Save(id);

		var fetched = os.Get((SB.Identity x) => x.Name, "updatable");
		Assert.That(fetched.PublicKey, Is.EqualTo(new byte[] { 0x02, 0x03 }));
	}

	#endregion

	#region User CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void User_CreateAndSave(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var user = os.New<SB.User>();
		user.Name = "alice";
		user.PublicKey = new byte[] { 0xAA };
		user.Email = "alice@example.com";
		user.DisplayName = "Alice";
		os.Save(user);
		Assert.That(os.Count<SB.User>(), Is.EqualTo(1));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void User_LookupByName(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var user = os.New<SB.User>();
		user.Name = "carol";
		user.Email = "carol@example.com";
		user.DisplayName = "Carol";
		os.Save(user);

		var fetched = os.Get((SB.User x) => x.Name, "carol");
		Assert.That(fetched, Is.SameAs(user));
		Assert.That(fetched.Email, Is.EqualTo("carol@example.com"));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void User_Update(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var user = os.New<SB.User>();
		user.Name = "dave";
		user.DisplayName = "Before";
		user.Email = "dave@old.com";
		os.Save(user);

		user.DisplayName = "After";
		user.Email = "dave@new.com";
		os.Save(user);

		var fetched = os.Get((SB.User x) => x.Name, "dave");
		Assert.That(fetched.DisplayName, Is.EqualTo("After"));
		Assert.That(fetched.Email, Is.EqualTo("dave@new.com"));
	}

	#endregion

	#region Group with polymorphic members

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Group_WithUserMembers_SaveAndLookup(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		// Create user identities
		var user1 = os.New<SB.User>();
		user1.Name = "user1";
		user1.Email = "user1@example.com";
		os.Save(user1);

		var user2 = os.New<SB.User>();
		user2.Name = "user2";
		user2.Email = "user2@example.com";
		os.Save(user2);

		// Create a group with user members (polymorphic: Identity[] containing User instances)
		var group = os.New<SB.Group>();
		group.Name = "admins";
		group.Members = new SB.Identity[] { user1, user2 };
		os.Save(group);

		Assert.That(os.Count<SB.User>(), Is.EqualTo(2));
		Assert.That(os.Count<SB.Group>(), Is.EqualTo(1));

		var fetched = os.Get((SB.Group x) => x.Name, "admins");
		Assert.That(fetched.Members, Is.Not.Null);
		Assert.That(fetched.Members.Length, Is.EqualTo(2));
		Assert.That(fetched.Members[0], Is.SameAs(user1));
		Assert.That(fetched.Members[1], Is.SameAs(user2));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Group_WithMixedMemberTypes(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		// Create a plain identity
		var plainId = os.New<SB.Identity>();
		plainId.Name = "service-account";
		plainId.PublicKey = new byte[] { 0xFF };
		os.Save(plainId);

		// Create a user
		var user = os.New<SB.User>();
		user.Name = "human-user";
		user.Email = "human@example.com";
		os.Save(user);

		// Create a nested group
		var innerGroup = os.New<SB.Group>();
		innerGroup.Name = "inner-team";
		innerGroup.Members = new SB.Identity[] { user };
		os.Save(innerGroup);

		// Create an outer group with all three member types
		var outerGroup = os.New<SB.Group>();
		outerGroup.Name = "all-access";
		outerGroup.Members = new SB.Identity[] { plainId, user, innerGroup };
		os.Save(outerGroup);

		Assert.That(os.Count<SB.Identity>(), Is.EqualTo(1));
		Assert.That(os.Count<SB.User>(), Is.EqualTo(1));
		Assert.That(os.Count<SB.Group>(), Is.EqualTo(2));

		var fetched = os.Get((SB.Group x) => x.Name, "all-access");
		Assert.That(fetched.Members.Length, Is.EqualTo(3));

		// Verify the types of each member
		Assert.That(fetched.Members[0], Is.SameAs(plainId));
		Assert.That(fetched.Members[0], Is.TypeOf<SB.Identity>());
		Assert.That(fetched.Members[1], Is.SameAs(user));
		Assert.That(fetched.Members[1], Is.TypeOf<SB.User>());
		Assert.That(fetched.Members[2], Is.SameAs(innerGroup));
		Assert.That(fetched.Members[2], Is.TypeOf<SB.Group>());
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Group_EmptyMembers(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var group = os.New<SB.Group>();
		group.Name = "empty-group";
		group.Members = Array.Empty<SB.Identity>();
		os.Save(group);

		var fetched = os.Get((SB.Group x) => x.Name, "empty-group");
		Assert.That(fetched.Members, Is.Not.Null);
		Assert.That(fetched.Members.Length, Is.EqualTo(0));
	}

	#endregion

	#region Account CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_CreateAndSave(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var owner = os.New<SB.User>();
		owner.Name = "owner1";
		owner.Email = "owner@example.com";
		os.Save(owner);

		var acc = os.New<SB.Account>();
		acc.AccountNumber = 1000;
		acc.Name = "Savings";
		acc.Balance = 100.50m;
		acc.Owner = owner;
		os.Save(acc);

		Assert.That(os.Count<SB.Account>(), Is.EqualTo(1));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_LookupByAccountNumber(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var owner = os.New<SB.Identity>();
		owner.Name = "owner2";
		owner.PublicKey = new byte[] { 0x01 };
		os.Save(owner);

		var acc = os.New<SB.Account>();
		acc.AccountNumber = 2000;
		acc.Name = "Checking";
		acc.Balance = 50m;
		acc.Owner = owner;
		os.Save(acc);

		var fetched = os.Get((SB.Account x) => x.AccountNumber, 2000L);
		Assert.That(fetched, Is.SameAs(acc));
		Assert.That(fetched.Owner, Is.SameAs(owner));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_UniqueAccountNumberProhibitsDuplicate(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var owner = os.New<SB.Identity>();
		owner.Name = "ownerdup";
		os.Save(owner);

		var acc1 = os.New<SB.Account>();
		acc1.AccountNumber = 9999;
		acc1.Name = "A";
		acc1.Owner = owner;
		os.Save(acc1);

		var acc2 = os.New<SB.Account>();
		acc2.AccountNumber = 9999;
		acc2.Name = "B";
		acc2.Owner = owner;
		Assert.That(() => os.Save(acc2), Throws.InvalidOperationException);
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_OwnedByGroup(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		// Owner is a Group (subclass of Identity) — tests polymorphic Owner reference
		var group = os.New<SB.Group>();
		group.Name = "treasury-group";
		group.Members = Array.Empty<SB.Identity>();
		os.Save(group);

		var acc = os.New<SB.Account>();
		acc.AccountNumber = 5000;
		acc.Name = "Treasury";
		acc.Balance = 1_000_000m;
		acc.Owner = group;
		os.Save(acc);

		var fetched = os.Get((SB.Account x) => x.AccountNumber, 5000L);
		Assert.That(fetched.Owner, Is.SameAs(group));
		Assert.That(fetched.Owner, Is.TypeOf<SB.Group>());
	}

	#endregion

	#region Permission CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Permission_CreateAndLookup(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var group = os.New<SB.Group>();
		group.Name = "editors";
		group.Members = Array.Empty<SB.Identity>();
		os.Save(group);

		var perm = os.New<SB.Permission>();
		perm.PermissionName = "edit.posts";
		perm.Description = "Can edit posts";
		perm.GrantedTo = group;
		os.Save(perm);

		Assert.That(os.Count<SB.Permission>(), Is.EqualTo(1));
		var fetched = os.Get((SB.Permission x) => x.PermissionName, "edit.posts");
		Assert.That(fetched.GrantedTo, Is.SameAs(group));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Permission_GrantedToUser(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var user = os.New<SB.User>();
		user.Name = "superadmin";
		user.Email = "admin@example.com";
		os.Save(user);

		var perm = os.New<SB.Permission>();
		perm.PermissionName = "admin.all";
		perm.Description = "Full admin access";
		perm.GrantedTo = user;
		os.Save(perm);

		var fetched = os.Get((SB.Permission x) => x.PermissionName, "admin.all");
		Assert.That(fetched.GrantedTo, Is.SameAs(user));
		Assert.That(fetched.GrantedTo, Is.TypeOf<SB.User>());
	}

	#endregion

	#region Block & Transaction CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Block_CreateEmpty(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var block = os.New<SB.Block>();
		block.Height = 0;
		block.Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		block.PreviousBlockHash = new byte[32];
		block.Transactions = Array.Empty<SB.Transaction>();
		os.Save(block);

		Assert.That(os.Count<SB.Block>(), Is.EqualTo(1));
		var fetched = os.Get((SB.Block x) => x.Height, 0L);
		Assert.That(fetched.Transactions, Is.Not.Null);
		Assert.That(fetched.Transactions.Length, Is.EqualTo(0));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Transaction_WithBackRefToBlock(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		// Create accounts
		var owner = os.New<SB.User>();
		owner.Name = "txowner";
		owner.Email = "txowner@example.com";
		os.Save(owner);

		var sender = os.New<SB.Account>();
		sender.AccountNumber = 100;
		sender.Name = "SenderAcc";
		sender.Balance = 1000m;
		sender.Owner = owner;
		os.Save(sender);

		var receiver = os.New<SB.Account>();
		receiver.AccountNumber = 200;
		receiver.Name = "ReceiverAcc";
		receiver.Balance = 0m;
		receiver.Owner = owner;
		os.Save(receiver);

		// Create block first (without transactions)
		var block = os.New<SB.Block>();
		block.Height = 1;
		block.Timestamp = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
		block.PreviousBlockHash = new byte[32];

		// Create transaction with back-reference to block
		var tx = os.New<SB.Transaction>();
		tx.TxHash = "tx-001";
		tx.Amount = 50m;
		tx.Sender = sender;
		tx.Receiver = receiver;
		tx.OwnerBlock = block;
		os.Save(tx);

		// Now set the block's transaction array and save
		block.Transactions = new[] { tx };
		os.Save(block);

		Assert.That(os.Count<SB.Block>(), Is.EqualTo(1));
		Assert.That(os.Count<SB.Transaction>(), Is.EqualTo(1));

		// Verify cross-references
		var fetchedTx = os.Get((SB.Transaction x) => x.TxHash, "tx-001");
		Assert.That(fetchedTx.Sender, Is.SameAs(sender));
		Assert.That(fetchedTx.Receiver, Is.SameAs(receiver));
		Assert.That(fetchedTx.OwnerBlock, Is.SameAs(block));

		var fetchedBlock = os.Get((SB.Block x) => x.Height, 1L);
		Assert.That(fetchedBlock.Transactions, Is.Not.Null);
		Assert.That(fetchedBlock.Transactions.Length, Is.EqualTo(1));
		Assert.That(fetchedBlock.Transactions[0], Is.SameAs(tx));
	}

	#endregion

	#region Complex Object Graph Integration Loop

	/// <summary>
	/// Creates a complex object graph in a loop: each iteration adds a block with transactions,
	/// new accounts, users, and groups. Verifies dimension counts and key lookups after each iteration.
	/// </summary>
	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.MemoryMappedTestCases))]
	public void IntegrationLoop_ComplexObjectGraph(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		const int Iterations = 5;
		const int TxPerBlock = 3;

		// Track expected counts
		var expectedUsers = 0;
		var expectedAccounts = 0;
		var expectedBlocks = 0;
		var expectedTransactions = 0;
		var expectedGroups = 0;

		// Pre-create a genesis block
		var genesisBlock = os.New<SB.Block>();
		genesisBlock.Height = 0;
		genesisBlock.Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		genesisBlock.PreviousBlockHash = new byte[32];
		genesisBlock.Transactions = Array.Empty<SB.Transaction>();
		os.Save(genesisBlock);
		expectedBlocks++;

		// Pre-create a global admin group
		var adminGroup = os.New<SB.Group>();
		adminGroup.Name = "global-admins";
		adminGroup.Members = Array.Empty<SB.Identity>();
		os.Save(adminGroup);
		expectedGroups++;

		for (var i = 0; i < Iterations; i++) {
			// Create two user identities per iteration
			var user1 = os.New<SB.User>();
			user1.Name = $"user-{i}-a";
			user1.Email = $"user{i}a@test.com";
			user1.DisplayName = $"User {i}A";
			os.Save(user1);
			expectedUsers++;

			var user2 = os.New<SB.User>();
			user2.Name = $"user-{i}-b";
			user2.Email = $"user{i}b@test.com";
			os.Save(user2);
			expectedUsers++;

			// Create two accounts per iteration (one per user)
			var acc1 = os.New<SB.Account>();
			acc1.AccountNumber = i * 1000 + 1;
			acc1.Name = $"Account-{i}-a";
			acc1.Balance = (i + 1) * 100m;
			acc1.Owner = user1;
			os.Save(acc1);
			expectedAccounts++;

			var acc2 = os.New<SB.Account>();
			acc2.AccountNumber = i * 1000 + 2;
			acc2.Name = $"Account-{i}-b";
			acc2.Balance = (i + 1) * 50m;
			acc2.Owner = user2;
			os.Save(acc2);
			expectedAccounts++;

			// Create a block with transactions
			var block = os.New<SB.Block>();
			block.Height = i + 1;
			block.Timestamp = new DateTime(2024, 1, 2 + i, 0, 0, 0, DateTimeKind.Utc);
			block.PreviousBlockHash = new byte[32];

			var txs = new SB.Transaction[TxPerBlock];
			for (var t = 0; t < TxPerBlock; t++) {
				var tx = os.New<SB.Transaction>();
				tx.TxHash = $"tx-{i}-{t}";
				tx.Amount = (t + 1) * 10m;
				tx.Sender = acc1;
				tx.Receiver = acc2;
				tx.OwnerBlock = block;
				os.Save(tx);
				txs[t] = tx;
				expectedTransactions++;
			}

			block.Transactions = txs;
			os.Save(block);
			expectedBlocks++;

			// Verify counts after each iteration
			Assert.That(os.Count<SB.User>(), Is.EqualTo(expectedUsers), $"User count mismatch at iteration {i}");
			Assert.That(os.Count<SB.Account>(), Is.EqualTo(expectedAccounts), $"Account count mismatch at iteration {i}");
			Assert.That(os.Count<SB.Block>(), Is.EqualTo(expectedBlocks), $"Block count mismatch at iteration {i}");
			Assert.That(os.Count<SB.Transaction>(), Is.EqualTo(expectedTransactions), $"Transaction count mismatch at iteration {i}");

			// Lookup by unique keys
			var fetchedUser = os.Get((SB.User x) => x.Name, $"user-{i}-a");
			Assert.That(fetchedUser, Is.SameAs(user1));

			var fetchedAcc = os.Get((SB.Account x) => x.AccountNumber, (long)(i * 1000 + 1));
			Assert.That(fetchedAcc, Is.SameAs(acc1));
			Assert.That(fetchedAcc.Owner, Is.SameAs(user1));

			var fetchedBlock = os.Get((SB.Block x) => x.Height, (long)(i + 1));
			Assert.That(fetchedBlock, Is.SameAs(block));
			Assert.That(fetchedBlock.Transactions.Length, Is.EqualTo(TxPerBlock));

			var fetchedTx = os.Get((SB.Transaction x) => x.TxHash, $"tx-{i}-0");
			Assert.That(fetchedTx.OwnerBlock, Is.SameAs(block));
			Assert.That(fetchedTx.Sender, Is.SameAs(acc1));
		}

		// Final totals
		Assert.That(os.Count<SB.User>(), Is.EqualTo(Iterations * 2), "Final user count");
		Assert.That(os.Count<SB.Group>(), Is.EqualTo(1), "Final group count");
		Assert.That(os.Count<SB.Account>(), Is.EqualTo(Iterations * 2), "Final account count");
		Assert.That(os.Count<SB.Block>(), Is.EqualTo(1 + Iterations), "Final block count");
		Assert.That(os.Count<SB.Transaction>(), Is.EqualTo(Iterations * TxPerBlock), "Final transaction count");

		// Flush should not throw
		Assert.That(() => os.Flush(), Throws.Nothing);
	}

	#endregion

	#region User/Group/Permission Hierarchy

	/// <summary>
	/// Tests the full user/group/permission hierarchy with polymorphic group membership.
	/// Groups contain actual Identity-typed arrays with User and Group instances.
	/// </summary>
	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.MemoryMappedTestCases))]
	public void IntegrationLoop_UserGroupPermissions(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		const int GroupCount = 3;
		const int UsersPerGroup = 4;

		var allUsers = new SB.User[GroupCount * UsersPerGroup];
		var allGroups = new SB.Group[GroupCount];

		// Create users
		for (var g = 0; g < GroupCount; g++) {
			for (var u = 0; u < UsersPerGroup; u++) {
				var idx = g * UsersPerGroup + u;
				var user = os.New<SB.User>();
				user.Name = $"user-g{g}-u{u}";
				user.Email = $"g{g}u{u}@test.com";
				os.Save(user);
				allUsers[idx] = user;
			}
		}

		// Create groups with user members (polymorphic Identity[] containing User instances)
		for (var g = 0; g < GroupCount; g++) {
			var group = os.New<SB.Group>();
			group.Name = $"group-{g}";
			group.Members = Enumerable.Range(0, UsersPerGroup)
				.Select(u => (SB.Identity)allUsers[g * UsersPerGroup + u])
				.ToArray();
			os.Save(group);
			allGroups[g] = group;
		}

		// Create permissions linked to groups
		for (var g = 0; g < GroupCount; g++) {
			var perm = os.New<SB.Permission>();
			perm.PermissionName = $"perm-{g}";
			perm.Description = $"Permission for group {g}";
			perm.GrantedTo = allGroups[g];
			os.Save(perm);
		}

		// Verify counts
		Assert.That(os.Count<SB.User>(), Is.EqualTo(GroupCount * UsersPerGroup));
		Assert.That(os.Count<SB.Group>(), Is.EqualTo(GroupCount));
		Assert.That(os.Count<SB.Permission>(), Is.EqualTo(GroupCount));

		// Verify group membership by key lookup
		for (var g = 0; g < GroupCount; g++) {
			var fetchedGroup = os.Get((SB.Group x) => x.Name, $"group-{g}");
			Assert.That(fetchedGroup.Members, Is.Not.Null);
			Assert.That(fetchedGroup.Members.Length, Is.EqualTo(UsersPerGroup));

			// Each member should be the same reference-equal User instance
			for (var u = 0; u < UsersPerGroup; u++) {
				var expectedUser = allUsers[g * UsersPerGroup + u];
				Assert.That(fetchedGroup.Members[u], Is.SameAs(expectedUser));
				Assert.That(fetchedGroup.Members[u], Is.TypeOf<SB.User>());
			}

			// Permission should reference the same group instance
			var fetchedPerm = os.Get((SB.Permission x) => x.PermissionName, $"perm-{g}");
			Assert.That(fetchedPerm.GrantedTo, Is.SameAs(fetchedGroup));
		}

		// Flush should not throw
		Assert.That(() => os.Flush(), Throws.Nothing);
	}

	#endregion

	#region Insert/Update/Delete Iteration Test

	/// <summary>
	/// Stress-tests the ObjectSpace with repeated insert, update, and delete operations
	/// across all dimension types. Modeled after the AssertEx.DictionaryIntegrationTest pattern:
	/// each iteration adds, modifies, and removes objects, then verifies counts and key lookups.
	/// </summary>
	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.MemoryMappedTestCases))]
	public void IntegrationLoop_InsertUpdateDelete(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		const int Iterations = 10;
		var rng = new Random(31337);

		// Track live objects by their unique key
		var liveUsers = new Dictionary<string, SB.User>();
		var liveAccounts = new Dictionary<long, SB.Account>();
		var liveGroups = new Dictionary<string, SB.Group>();
		var livePermissions = new Dictionary<string, SB.Permission>();
		var nextAccountNumber = 1L;

		for (var i = 0; i < Iterations; i++) {

			// ── INSERT phase: add 2–4 users and corresponding accounts ──────
			var toInsert = rng.Next(2, 5);
			for (var j = 0; j < toInsert; j++) {
				var userName = $"iter{i}-user{j}";
				var user = os.New<SB.User>();
				user.Name = userName;
				user.Email = $"{userName}@test.com";
				user.DisplayName = $"User {i}.{j}";
				os.Save(user);
				liveUsers[userName] = user;

				var acc = os.New<SB.Account>();
				acc.AccountNumber = nextAccountNumber++;
				acc.Name = $"Acc-{userName}";
				acc.Balance = rng.Next(1, 10000);
				acc.Owner = user;
				os.Save(acc);
				liveAccounts[acc.AccountNumber] = acc;
			}

			// ── INSERT groups referencing random users ───────────────────────
			if (liveUsers.Count >= 2) {
				var groupName = $"iter{i}-group";
				var group = os.New<SB.Group>();
				group.Name = groupName;
				// Pick 1–3 random live users as members
				var memberCount = Math.Min(rng.Next(1, 4), liveUsers.Count);
				group.Members = liveUsers.Values
					.OrderBy(_ => rng.Next())
					.Take(memberCount)
					.Cast<SB.Identity>()
					.ToArray();
				os.Save(group);
				liveGroups[groupName] = group;

				// Optionally add a permission for this group
				if (rng.Next(2) == 0) {
					var permName = $"perm-{groupName}";
					var perm = os.New<SB.Permission>();
					perm.PermissionName = permName;
					perm.Description = $"Perm for {groupName}";
					perm.GrantedTo = group;
					os.Save(perm);
					livePermissions[permName] = perm;
				}
			}

			// ── UPDATE phase: update some account balances ──────────────────
			var toUpdate = Math.Min(rng.Next(1, 3), liveAccounts.Count);
			var accountsToUpdate = liveAccounts.Values.OrderBy(_ => rng.Next()).Take(toUpdate).ToArray();
			foreach (var acc in accountsToUpdate) {
				acc.Balance += rng.Next(-500, 500);
				os.Save(acc);
			}

			// ── UPDATE phase: update some user display names ────────────────
			var usersToUpdate = Math.Min(rng.Next(0, 3), liveUsers.Count);
			var selectedUsers = liveUsers.Values.OrderBy(_ => rng.Next()).Take(usersToUpdate).ToArray();
			foreach (var usr in selectedUsers) {
				usr.DisplayName = $"Updated-{i}-{rng.Next(1000)}";
				os.Save(usr);
			}

			// ── DELETE phase: delete some accounts and their owners ──────
			var toDelete = Math.Min(rng.Next(0, 4), liveAccounts.Count);
			var accountsToDelete = liveAccounts.Values.OrderBy(_ => rng.Next()).Take(toDelete).ToArray();
			foreach (var acc in accountsToDelete) {
				os.Delete(acc);
				liveAccounts.Remove(acc.AccountNumber);
			}

			// ── DELETE phase: delete some permissions ────────────────────────
			if (livePermissions.Count > 0 && rng.Next(3) == 0) {
				var permToDelete = livePermissions.Values.OrderBy(_ => rng.Next()).First();
				os.Delete(permToDelete);
				livePermissions.Remove(permToDelete.PermissionName);
			}

			// ── VERIFY phase: counts match ──────────────────────────────────
			Assert.That(os.Count<SB.User>(), Is.EqualTo(liveUsers.Count), $"User count mismatch at iteration {i}");
			Assert.That(os.Count<SB.Account>(), Is.EqualTo(liveAccounts.Count), $"Account count mismatch at iteration {i}");
			Assert.That(os.Count<SB.Group>(), Is.EqualTo(liveGroups.Count), $"Group count mismatch at iteration {i}");
			Assert.That(os.Count<SB.Permission>(), Is.EqualTo(livePermissions.Count), $"Permission count mismatch at iteration {i}");

			// ── VERIFY phase: key lookups ───────────────────────────────────
			foreach (var kvp in liveUsers) {
				var fetched = os.Get((SB.User x) => x.Name, kvp.Key);
				Assert.That(fetched, Is.SameAs(kvp.Value), $"User '{kvp.Key}' lookup failed at iteration {i}");
			}

			foreach (var kvp in liveAccounts) {
				var fetched = os.Get((SB.Account x) => x.AccountNumber, kvp.Key);
				Assert.That(fetched, Is.SameAs(kvp.Value), $"Account {kvp.Key} lookup failed at iteration {i}");
			}

			foreach (var kvp in liveGroups) {
				var fetched = os.Get((SB.Group x) => x.Name, kvp.Key);
				Assert.That(fetched, Is.SameAs(kvp.Value), $"Group '{kvp.Key}' lookup failed at iteration {i}");
			}

			foreach (var kvp in livePermissions) {
				var fetched = os.Get((SB.Permission x) => x.PermissionName, kvp.Key);
				Assert.That(fetched, Is.SameAs(kvp.Value), $"Permission '{kvp.Key}' lookup failed at iteration {i}");
			}

			// ── VERIFY phase: updated values are correct ────────────────────
			foreach (var acc in accountsToUpdate) {
				if (liveAccounts.ContainsKey(acc.AccountNumber)) {
					var fetched = os.Get((SB.Account x) => x.AccountNumber, acc.AccountNumber);
					Assert.That(fetched.Balance, Is.EqualTo(acc.Balance), $"Account {acc.AccountNumber} balance mismatch after update at iteration {i}");
				}
			}

			// Clear everything halfway through to exercise bulk deletion
			if (i == Iterations / 2) {
				// Delete all permissions first (they reference groups)
				foreach (var perm in livePermissions.Values.ToArray())
					os.Delete(perm);
				livePermissions.Clear();

				// Delete all groups
				foreach (var grp in liveGroups.Values.ToArray())
					os.Delete(grp);
				liveGroups.Clear();

				// Delete all accounts (they reference users)
				foreach (var acc in liveAccounts.Values.ToArray())
					os.Delete(acc);
				liveAccounts.Clear();

				// Delete all users
				foreach (var usr in liveUsers.Values.ToArray())
					os.Delete(usr);
				liveUsers.Clear();

				Assert.That(os.Count<SB.User>(), Is.EqualTo(0), "User count should be 0 after mid-test clear");
				Assert.That(os.Count<SB.Account>(), Is.EqualTo(0), "Account count should be 0 after mid-test clear");
				Assert.That(os.Count<SB.Group>(), Is.EqualTo(0), "Group count should be 0 after mid-test clear");
				Assert.That(os.Count<SB.Permission>(), Is.EqualTo(0), "Permission count should be 0 after mid-test clear");

				// Reset account number counter
				nextAccountNumber = 1;
			}
		}

		// Final flush should not throw
		Assert.That(() => os.Flush(), Throws.Nothing);
	}

	#endregion
}
