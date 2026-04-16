// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;

namespace Sphere10.Framework.Tests.ObjectSpaces;

/// <summary>
/// Comprehensive ObjectSpace integration tests using the PascalCoin SafeBox-inspired model.
/// Covers individual CRUD per dimension type, cross-dimension references, unique key lookups,
/// arrays of dimension objects, back-references (Block ↔ Transaction), and complex object graph
/// creation loops with count/key verification.
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
		var id = os.New<SafeBoxIdentity>();
		id.Name = "alice";
		id.PublicKey = new byte[] { 0xAA, 0xBB };
		id.IdentityType = SafeBoxIdentityType.User;
		id.Email = "alice@example.com";
		id.DisplayName = "Alice";
		os.Save(id);
		Assert.That(os.Count<SafeBoxIdentity>(), Is.EqualTo(1));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_LookupByUniqueName(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id = os.New<SafeBoxIdentity>();
		id.Name = "bob";
		id.PublicKey = new byte[] { 0x01 };
		id.IdentityType = SafeBoxIdentityType.Identity;
		os.Save(id);

		var fetched = os.Get((SafeBoxIdentity x) => x.Name, "bob");
		Assert.That(fetched, Is.SameAs(id));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_UniqueNameProhibitsDuplicate(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id1 = os.New<SafeBoxIdentity>();
		id1.Name = "dup";
		id1.IdentityType = SafeBoxIdentityType.Identity;
		os.Save(id1);

		var id2 = os.New<SafeBoxIdentity>();
		id2.Name = "dup";
		id2.IdentityType = SafeBoxIdentityType.User;
		Assert.That(() => os.Save(id2), Throws.InvalidOperationException);
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_Delete(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id = os.New<SafeBoxIdentity>();
		id.Name = "todelete";
		id.IdentityType = SafeBoxIdentityType.Identity;
		os.Save(id);
		Assert.That(os.Count<SafeBoxIdentity>(), Is.EqualTo(1));

		os.Delete(id);
		Assert.That(os.Count<SafeBoxIdentity>(), Is.EqualTo(0));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_Update(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id = os.New<SafeBoxIdentity>();
		id.Name = "updatable";
		id.DisplayName = "Before";
		id.IdentityType = SafeBoxIdentityType.User;
		os.Save(id);

		id.DisplayName = "After";
		os.Save(id);

		var fetched = os.Get((SafeBoxIdentity x) => x.Name, "updatable");
		Assert.That(fetched.DisplayName, Is.EqualTo("After"));
	}

	#endregion

	#region Identity Groups (array of dimension objects)

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Group_WithMembers_SaveAndLookup(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		// Create user identities
		var user1 = os.New<SafeBoxIdentity>();
		user1.Name = "user1";
		user1.IdentityType = SafeBoxIdentityType.User;
		user1.Email = "user1@example.com";
		os.Save(user1);

		var user2 = os.New<SafeBoxIdentity>();
		user2.Name = "user2";
		user2.IdentityType = SafeBoxIdentityType.User;
		os.Save(user2);

		// Create a group that stores member names (resolved via identity lookup)
		var group = os.New<SafeBoxIdentity>();
		group.Name = "admins";
		group.IdentityType = SafeBoxIdentityType.Group;
		group.MemberNames = new[] { "user1", "user2" };
		os.Save(group);

		Assert.That(os.Count<SafeBoxIdentity>(), Is.EqualTo(3));

		var fetched = os.Get((SafeBoxIdentity x) => x.Name, "admins");
		Assert.That(fetched.MemberNames, Is.Not.Null);
		Assert.That(fetched.MemberNames.Length, Is.EqualTo(2));
		Assert.That(fetched.MemberNames[0], Is.EqualTo("user1"));
		Assert.That(fetched.MemberNames[1], Is.EqualTo("user2"));

		// Resolve members by key lookup
		var resolvedUser1 = os.Get((SafeBoxIdentity x) => x.Name, fetched.MemberNames[0]);
		Assert.That(resolvedUser1, Is.SameAs(user1));
	}

	#endregion

	#region Account CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_CreateAndSave(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var owner = os.New<SafeBoxIdentity>();
		owner.Name = "owner1";
		owner.IdentityType = SafeBoxIdentityType.User;
		os.Save(owner);

		var acc = os.New<SafeBoxAccount>();
		acc.AccountNumber = 1000;
		acc.Name = "Savings";
		acc.Balance = 100.50m;
		acc.Owner = owner;
		os.Save(acc);

		Assert.That(os.Count<SafeBoxAccount>(), Is.EqualTo(1));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_LookupByAccountNumber(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var owner = os.New<SafeBoxIdentity>();
		owner.Name = "owner2";
		owner.IdentityType = SafeBoxIdentityType.Identity;
		os.Save(owner);

		var acc = os.New<SafeBoxAccount>();
		acc.AccountNumber = 2000;
		acc.Name = "Checking";
		acc.Balance = 50m;
		acc.Owner = owner;
		os.Save(acc);

		var fetched = os.Get((SafeBoxAccount x) => x.AccountNumber, 2000L);
		Assert.That(fetched, Is.SameAs(acc));
		Assert.That(fetched.Owner, Is.SameAs(owner));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_UniqueAccountNumberProhibitsDuplicate(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var owner = os.New<SafeBoxIdentity>();
		owner.Name = "ownerdup";
		owner.IdentityType = SafeBoxIdentityType.Identity;
		os.Save(owner);

		var acc1 = os.New<SafeBoxAccount>();
		acc1.AccountNumber = 9999;
		acc1.Name = "A";
		acc1.Owner = owner;
		os.Save(acc1);

		var acc2 = os.New<SafeBoxAccount>();
		acc2.AccountNumber = 9999;
		acc2.Name = "B";
		acc2.Owner = owner;
		Assert.That(() => os.Save(acc2), Throws.InvalidOperationException);
	}

	#endregion

	#region Permission CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Permission_CreateAndLookup(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var group = os.New<SafeBoxIdentity>();
		group.Name = "editors";
		group.IdentityType = SafeBoxIdentityType.Group;
		os.Save(group);

		var perm = os.New<SafeBoxPermission>();
		perm.PermissionName = "edit.posts";
		perm.Description = "Can edit posts";
		perm.GrantedTo = group;
		os.Save(perm);

		Assert.That(os.Count<SafeBoxPermission>(), Is.EqualTo(1));
		var fetched = os.Get((SafeBoxPermission x) => x.PermissionName, "edit.posts");
		Assert.That(fetched.GrantedTo, Is.SameAs(group));
	}

	#endregion

	#region Block & Transaction CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Block_CreateEmpty(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var block = os.New<SafeBoxBlock>();
		block.Height = 0;
		block.Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		block.PreviousBlockHash = new byte[32];
		block.Transactions = Array.Empty<SafeBoxTransaction>();
		os.Save(block);

		Assert.That(os.Count<SafeBoxBlock>(), Is.EqualTo(1));
		var fetched = os.Get((SafeBoxBlock x) => x.Height, 0L);
		Assert.That(fetched.Transactions, Is.Not.Null);
		Assert.That(fetched.Transactions.Length, Is.EqualTo(0));
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Transaction_WithBackRefToBlock(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		// Create accounts
		var owner = os.New<SafeBoxIdentity>();
		owner.Name = "txowner";
		owner.IdentityType = SafeBoxIdentityType.User;
		os.Save(owner);

		var sender = os.New<SafeBoxAccount>();
		sender.AccountNumber = 100;
		sender.Name = "SenderAcc";
		sender.Balance = 1000m;
		sender.Owner = owner;
		os.Save(sender);

		var receiver = os.New<SafeBoxAccount>();
		receiver.AccountNumber = 200;
		receiver.Name = "ReceiverAcc";
		receiver.Balance = 0m;
		receiver.Owner = owner;
		os.Save(receiver);

		// Create block first (without transactions)
		var block = os.New<SafeBoxBlock>();
		block.Height = 1;
		block.Timestamp = new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc);
		block.PreviousBlockHash = new byte[32];

		// Create transaction with back-reference to block
		var tx = os.New<SafeBoxTransaction>();
		tx.TxHash = "tx-001";
		tx.Amount = 50m;
		tx.Sender = sender;
		tx.Receiver = receiver;
		tx.OwnerBlock = block;
		os.Save(tx);

		// Now set the block's transaction array and save
		block.Transactions = new[] { tx };
		os.Save(block);

		Assert.That(os.Count<SafeBoxBlock>(), Is.EqualTo(1));
		Assert.That(os.Count<SafeBoxTransaction>(), Is.EqualTo(1));

		// Verify cross-references
		var fetchedTx = os.Get((SafeBoxTransaction x) => x.TxHash, "tx-001");
		Assert.That(fetchedTx.Sender, Is.SameAs(sender));
		Assert.That(fetchedTx.Receiver, Is.SameAs(receiver));
		Assert.That(fetchedTx.OwnerBlock, Is.SameAs(block));

		var fetchedBlock = os.Get((SafeBoxBlock x) => x.Height, 1L);
		Assert.That(fetchedBlock.Transactions, Is.Not.Null);
		Assert.That(fetchedBlock.Transactions.Length, Is.EqualTo(1));
		Assert.That(fetchedBlock.Transactions[0], Is.SameAs(tx));
	}

	#endregion

	#region Integration Loop

	/// <summary>
	/// Creates a complex object graph in a loop: each iteration adds a block with transactions,
	/// new accounts, and groups. Verifies dimension counts and key lookups after each iteration.
	/// </summary>
	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.MemoryMappedTestCases))]
	public void IntegrationLoop_ComplexObjectGraph(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		const int Iterations = 5;
		const int TxPerBlock = 3;

		// Track expected counts
		var expectedIdentities = 0;
		var expectedAccounts = 0;
		var expectedBlocks = 0;
		var expectedTransactions = 0;

		// Pre-create a genesis block
		var genesisBlock = os.New<SafeBoxBlock>();
		genesisBlock.Height = 0;
		genesisBlock.Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		genesisBlock.PreviousBlockHash = new byte[32];
		genesisBlock.Transactions = Array.Empty<SafeBoxTransaction>();
		os.Save(genesisBlock);
		expectedBlocks++;

		// Pre-create a global admin group
		var adminGroup = os.New<SafeBoxIdentity>();
		adminGroup.Name = "global-admins";
		adminGroup.IdentityType = SafeBoxIdentityType.Group;
		adminGroup.MemberNames = Array.Empty<string>();
		os.Save(adminGroup);
		expectedIdentities++;

		for (var i = 0; i < Iterations; i++) {
			// Create two user identities per iteration
			var user1 = os.New<SafeBoxIdentity>();
			user1.Name = $"user-{i}-a";
			user1.IdentityType = SafeBoxIdentityType.User;
			user1.Email = $"user{i}a@test.com";
			user1.DisplayName = $"User {i}A";
			os.Save(user1);
			expectedIdentities++;

			var user2 = os.New<SafeBoxIdentity>();
			user2.Name = $"user-{i}-b";
			user2.IdentityType = SafeBoxIdentityType.User;
			user2.Email = $"user{i}b@test.com";
			os.Save(user2);
			expectedIdentities++;

			// Create two accounts per iteration (one per user)
			var acc1 = os.New<SafeBoxAccount>();
			acc1.AccountNumber = i * 1000 + 1;
			acc1.Name = $"Account-{i}-a";
			acc1.Balance = (i + 1) * 100m;
			acc1.Owner = user1;
			os.Save(acc1);
			expectedAccounts++;

			var acc2 = os.New<SafeBoxAccount>();
			acc2.AccountNumber = i * 1000 + 2;
			acc2.Name = $"Account-{i}-b";
			acc2.Balance = (i + 1) * 50m;
			acc2.Owner = user2;
			os.Save(acc2);
			expectedAccounts++;

			// Create a block with transactions
			var block = os.New<SafeBoxBlock>();
			block.Height = i + 1;
			block.Timestamp = new DateTime(2024, 1, 2 + i, 0, 0, 0, DateTimeKind.Utc);
			block.PreviousBlockHash = new byte[32];

			var txs = new SafeBoxTransaction[TxPerBlock];
			for (var t = 0; t < TxPerBlock; t++) {
				var tx = os.New<SafeBoxTransaction>();
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
			Assert.That(os.Count<SafeBoxIdentity>(), Is.EqualTo(expectedIdentities), $"Identity count mismatch at iteration {i}");
			Assert.That(os.Count<SafeBoxAccount>(), Is.EqualTo(expectedAccounts), $"Account count mismatch at iteration {i}");
			Assert.That(os.Count<SafeBoxBlock>(), Is.EqualTo(expectedBlocks), $"Block count mismatch at iteration {i}");
			Assert.That(os.Count<SafeBoxTransaction>(), Is.EqualTo(expectedTransactions), $"Transaction count mismatch at iteration {i}");

			// Lookup by unique keys
			var fetchedUser = os.Get((SafeBoxIdentity x) => x.Name, $"user-{i}-a");
			Assert.That(fetchedUser, Is.SameAs(user1));

			var fetchedAcc = os.Get((SafeBoxAccount x) => x.AccountNumber, (long)(i * 1000 + 1));
			Assert.That(fetchedAcc, Is.SameAs(acc1));
			Assert.That(fetchedAcc.Owner, Is.SameAs(user1));

			var fetchedBlock = os.Get((SafeBoxBlock x) => x.Height, (long)(i + 1));
			Assert.That(fetchedBlock, Is.SameAs(block));
			Assert.That(fetchedBlock.Transactions.Length, Is.EqualTo(TxPerBlock));

			var fetchedTx = os.Get((SafeBoxTransaction x) => x.TxHash, $"tx-{i}-0");
			Assert.That(fetchedTx.OwnerBlock, Is.SameAs(block));
			Assert.That(fetchedTx.Sender, Is.SameAs(acc1));
		}

		// Final totals
		Assert.That(os.Count<SafeBoxIdentity>(), Is.EqualTo(1 + Iterations * 2), "Final identity count");
		Assert.That(os.Count<SafeBoxAccount>(), Is.EqualTo(Iterations * 2), "Final account count");
		Assert.That(os.Count<SafeBoxBlock>(), Is.EqualTo(1 + Iterations), "Final block count");
		Assert.That(os.Count<SafeBoxTransaction>(), Is.EqualTo(Iterations * TxPerBlock), "Final transaction count");

		// Flush should not throw
		Assert.That(() => os.Flush(), Throws.Nothing);
	}

	/// <summary>
	/// Tests the full user/group/permission hierarchy with nested group membership.
	/// </summary>
	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.MemoryMappedTestCases))]
	public void IntegrationLoop_UserGroupPermissions(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);

		const int GroupCount = 3;
		const int UsersPerGroup = 4;

		var allUsers = new SafeBoxIdentity[GroupCount * UsersPerGroup];
		var allGroups = new SafeBoxIdentity[GroupCount];

		// Create users
		for (var g = 0; g < GroupCount; g++) {
			for (var u = 0; u < UsersPerGroup; u++) {
				var idx = g * UsersPerGroup + u;
				var user = os.New<SafeBoxIdentity>();
				user.Name = $"user-g{g}-u{u}";
				user.IdentityType = SafeBoxIdentityType.User;
				user.Email = $"g{g}u{u}@test.com";
				os.Save(user);
				allUsers[idx] = user;
			}
		}

		// Create groups with member names
		for (var g = 0; g < GroupCount; g++) {
			var group = os.New<SafeBoxIdentity>();
			group.Name = $"group-{g}";
			group.IdentityType = SafeBoxIdentityType.Group;
			group.MemberNames = Enumerable.Range(0, UsersPerGroup)
				.Select(u => $"user-g{g}-u{u}")
				.ToArray();
			os.Save(group);
			allGroups[g] = group;
		}

		// Create permissions linked to groups
		for (var g = 0; g < GroupCount; g++) {
			var perm = os.New<SafeBoxPermission>();
			perm.PermissionName = $"perm-{g}";
			perm.Description = $"Permission for group {g}";
			perm.GrantedTo = allGroups[g];
			os.Save(perm);
		}

		// Verify counts
		Assert.That(os.Count<SafeBoxIdentity>(), Is.EqualTo(GroupCount * UsersPerGroup + GroupCount));
		Assert.That(os.Count<SafeBoxPermission>(), Is.EqualTo(GroupCount));

		// Verify group membership by key lookup
		for (var g = 0; g < GroupCount; g++) {
			var fetchedGroup = os.Get((SafeBoxIdentity x) => x.Name, $"group-{g}");
			Assert.That(fetchedGroup.IdentityType, Is.EqualTo(SafeBoxIdentityType.Group));
			Assert.That(fetchedGroup.MemberNames, Is.Not.Null);
			Assert.That(fetchedGroup.MemberNames.Length, Is.EqualTo(UsersPerGroup));

			// Resolve members by name and verify they match the original user objects
			for (var u = 0; u < UsersPerGroup; u++) {
				var expectedUser = allUsers[g * UsersPerGroup + u];
				var resolvedUser = os.Get((SafeBoxIdentity x) => x.Name, fetchedGroup.MemberNames[u]);
				Assert.That(resolvedUser, Is.SameAs(expectedUser));
			}

			// Permission should reference the same group instance
			var fetchedPerm = os.Get((SafeBoxPermission x) => x.PermissionName, $"perm-{g}");
			Assert.That(fetchedPerm.GrantedTo, Is.SameAs(fetchedGroup));
		}

		// Flush should not throw
		Assert.That(() => os.Flush(), Throws.Nothing);
	}

	#endregion
}
