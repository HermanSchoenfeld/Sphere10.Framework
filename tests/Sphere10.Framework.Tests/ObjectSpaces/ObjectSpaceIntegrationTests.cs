// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using BenchmarkDotNet.Attributes;
using NUnit.Framework;
using Sphere10.Framework.ObjectSpaces;
using System;
using System.Collections.Generic;
using System.Linq;
using SB = Sphere10.Framework.Tests.SafeBox;

namespace Sphere10.Framework.Tests.ObjectSpaces;

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
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var id = os.New<SB.Identity>();
			id.Name = "generic-id";
			id.PublicKey = new byte[] { 0xAA, 0xBB };
			os.Save(id);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Identity>(), Is.EqualTo(1));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_LookupByUniqueName(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var id = os.New<SB.Identity>();
			id.Name = "bob";
			id.PublicKey = new byte[] { 0x01 };
			os.Save(id);
			var fetched = os.Get((SB.Identity x) => x.Name, "bob");
			Assert.That(fetched, Is.SameAs(id));
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			var fetched = objectSpace.Get((SB.Identity x) => x.Name, "bob");
			Assert.That(fetched.Name, Is.EqualTo("bob"));
			Assert.That(fetched.PublicKey, Is.EqualTo(new byte[] { 0x01 }));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_UniqueNameProhibitsDuplicate(TestTraits traits) {
		using var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits);
		var id1 = os.New<SB.Identity>();
		id1.Name = "dup";
		os.Save(id1);
		Assert.That(() => {
			var id2 = os.New<SB.Identity>();
			id2.Name = "dup";
			os.Save(id2);
		}, Throws.InvalidOperationException);
		if (traits.HasFlag(TestTraits.PersistentIgnorant))
			os.AutoSave = false;
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_Delete(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var id = os.New<SB.Identity>();
			id.Name = "todelete";
			os.Save(id);
			Assert.That(os.Count<SB.Identity>(), Is.EqualTo(1));
			os.Delete(id);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		if (!traits.HasFlag(TestTraits.Merklized)) {
			using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
			Validate(hydratedOS);
		}
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Identity>(), Is.EqualTo(0));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Identity_Update(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var id = os.New<SB.Identity>();
			id.Name = "updatable";
			id.PublicKey = new byte[] { 0x01 };
			os.Save(id);
			id.PublicKey = new byte[] { 0x02, 0x03 };
			os.Save(id);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			var fetched = objectSpace.Get((SB.Identity x) => x.Name, "updatable");
			Assert.That(fetched.PublicKey, Is.EqualTo(new byte[] { 0x02, 0x03 }));
		}
	}

	#endregion
	#region User CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void User_CreateAndSave(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var user = os.New<SB.User>();
			user.Name = "alice";
			user.PublicKey = new byte[] { 0xAA };
			user.Email = "alice@example.com";
			user.DisplayName = "Alice";
			os.Save(user);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.User>(), Is.EqualTo(1));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void User_LookupByName(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var user = os.New<SB.User>();
			user.Name = "carol";
			user.Email = "carol@example.com";
			user.DisplayName = "Carol";
			os.Save(user);
			var fetched = os.Get((SB.User x) => x.Name, "carol");
			Assert.That(fetched, Is.SameAs(user));
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			var fetched = objectSpace.Get((SB.User x) => x.Name, "carol");
			Assert.That(fetched.Email, Is.EqualTo("carol@example.com"));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void User_Update(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var user = os.New<SB.User>();
			user.Name = "dave";
			user.DisplayName = "Before";
			user.Email = "dave@old.com";
			os.Save(user);
			user.DisplayName = "After";
			user.Email = "dave@new.com";
			os.Save(user);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			var fetched = objectSpace.Get((SB.User x) => x.Name, "dave");
			Assert.That(fetched.DisplayName, Is.EqualTo("After"));
			Assert.That(fetched.Email, Is.EqualTo("dave@new.com"));
		}
	}

	#endregion
	#region Group with polymorphic members

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Group_WithUserMembers_SaveAndLookup(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var user1 = os.New<SB.User>();
			user1.Name = "user1";
			user1.Email = "user1@example.com";
			os.Save(user1);
			var user2 = os.New<SB.User>();
			user2.Name = "user2";
			user2.Email = "user2@example.com";
			os.Save(user2);
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
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.User>(), Is.EqualTo(2));
			Assert.That(objectSpace.Count<SB.Group>(), Is.EqualTo(1));
			var group = objectSpace.Get((SB.Group x) => x.Name, "admins");
			Assert.That(group.Members, Is.Not.Null);
			Assert.That(group.Members.Length, Is.EqualTo(2));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Group_WithMixedMemberTypes(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var plainId = os.New<SB.Identity>();
			plainId.Name = "service-account";
			plainId.PublicKey = new byte[] { 0xFF };
			os.Save(plainId);
			var user = os.New<SB.User>();
			user.Name = "human-user";
			user.Email = "human@example.com";
			os.Save(user);
			var innerGroup = os.New<SB.Group>();
			innerGroup.Name = "inner-team";
			innerGroup.Members = new SB.Identity[] { user };
			os.Save(innerGroup);
			var outerGroup = os.New<SB.Group>();
			outerGroup.Name = "all-access";
			outerGroup.Members = new SB.Identity[] { plainId, user, innerGroup };
			os.Save(outerGroup);
			Assert.That(os.Count<SB.Identity>(), Is.EqualTo(1));
			Assert.That(os.Count<SB.User>(), Is.EqualTo(1));
			Assert.That(os.Count<SB.Group>(), Is.EqualTo(2));
			var fetched = os.Get((SB.Group x) => x.Name, "all-access");
			Assert.That(fetched.Members.Length, Is.EqualTo(3));
			Assert.That(fetched.Members[0], Is.SameAs(plainId));
			Assert.That(fetched.Members[0], Is.TypeOf<SB.Identity>());
			Assert.That(fetched.Members[1], Is.SameAs(user));
			Assert.That(fetched.Members[1], Is.TypeOf<SB.User>());
			Assert.That(fetched.Members[2], Is.SameAs(innerGroup));
			Assert.That(fetched.Members[2], Is.TypeOf<SB.Group>());
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Identity>(), Is.EqualTo(1));
			Assert.That(objectSpace.Count<SB.User>(), Is.EqualTo(1));
			Assert.That(objectSpace.Count<SB.Group>(), Is.EqualTo(2));
			var outer = objectSpace.Get((SB.Group x) => x.Name, "all-access");
			Assert.That(outer.Members.Length, Is.EqualTo(3));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Group_EmptyMembers(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var group = os.New<SB.Group>();
			group.Name = "empty-group";
			group.Members = Array.Empty<SB.Identity>();
			os.Save(group);
			var fetched = os.Get((SB.Group x) => x.Name, "empty-group");
			Assert.That(fetched.Members, Is.Not.Null);
			Assert.That(fetched.Members.Length, Is.EqualTo(0));
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Group>(), Is.EqualTo(1));
			var group = objectSpace.Get((SB.Group x) => x.Name, "empty-group");
			Assert.That(group.Members, Is.Not.Null);
			Assert.That(group.Members.Length, Is.EqualTo(0));
		}
	}

	#endregion
	#region Account CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_CreateAndSave(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
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
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Account>(), Is.EqualTo(1));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_LookupByAccountNumber(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
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
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			var fetched = objectSpace.Get((SB.Account x) => x.AccountNumber, 2000L);
			Assert.That(fetched.Name, Is.EqualTo("Checking"));
			Assert.That(fetched.Balance, Is.EqualTo(50m));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_Update(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var owner = os.New<SB.Identity>();
			owner.Name = "owner3";
			owner.PublicKey = new byte[] { 0x02 };
			os.Save(owner);
			var acc = os.New<SB.Account>();
			acc.AccountNumber = 3000;
			acc.Name = "Joint";
			acc.Balance = 200m;
			acc.Owner = owner;
			os.Save(acc);
			acc.Balance = 999m;
			os.Save(acc);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			var fetched = objectSpace.Get((SB.Account x) => x.AccountNumber, 3000L);
			Assert.That(fetched.Balance, Is.EqualTo(999m));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Account_Delete(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var owner = os.New<SB.Identity>();
			owner.Name = "owner4";
			owner.PublicKey = new byte[] { 0x03 };
			os.Save(owner);
			var acc = os.New<SB.Account>();
			acc.AccountNumber = 4000;
			acc.Name = "ToDelete";
			acc.Balance = 0m;
			acc.Owner = owner;
			os.Save(acc);
			Assert.That(os.Count<SB.Account>(), Is.EqualTo(1));
			os.Delete(acc);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Account>(), Is.EqualTo(0));
		}
	}

	#endregion
	#region Permission CRUD

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Permission_CreateAndLookup(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var user = os.New<SB.User>();
			user.Name = "perm-user";
			user.Email = "perm@example.com";
			os.Save(user);
			var acc = os.New<SB.Account>();
			acc.AccountNumber = 5000;
			acc.Name = "PermTarget";
			acc.Balance = 0m;
			acc.Owner = user;
			os.Save(acc);
			var perm = os.New<SB.Permission>();
			perm.PermissionName = "send-permission";
			perm.Description = "Can send funds";
			perm.GrantedTo = user;
			os.Save(perm);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Permission>(), Is.EqualTo(1));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Permission_Update(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var user = os.New<SB.User>();
			user.Name = "perm-user2";
			user.Email = "perm2@example.com";
			os.Save(user);
			var acc = os.New<SB.Account>();
			acc.AccountNumber = 6000;
			acc.Name = "PermTarget2";
			acc.Balance = 0m;
			acc.Owner = user;
			os.Save(acc);
			var perm = os.New<SB.Permission>();
			perm.PermissionName = "send-permission";
			perm.Description = "Can send funds";
			perm.PermissionName = "limited-perm";
			os.Save(perm);
			perm.GrantedTo = user;
			os.Save(perm);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Permission>(), Is.EqualTo(1));
		}
	}

	#endregion
	#region Block and Transaction

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Block_WithTransactions(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var sender = os.New<SB.Identity>();
			sender.Name = "sender";
			sender.PublicKey = new byte[] { 0x10 };
			os.Save(sender);
			var senderAcc = os.New<SB.Account>();
			senderAcc.AccountNumber = 7000;
			senderAcc.Name = "SenderAcc";
			senderAcc.Balance = 500m;
			senderAcc.Owner = sender;
			os.Save(senderAcc);
			var receiverAcc = os.New<SB.Account>();
			receiverAcc.AccountNumber = 7001;
			receiverAcc.Name = "ReceiverAcc";
			receiverAcc.Balance = 0m;
			receiverAcc.Owner = sender;
			os.Save(receiverAcc);
			var tx = os.New<SB.Transaction>();
			tx.Sender = senderAcc;
			tx.Receiver = receiverAcc;
			tx.Amount = 42m;
			tx.TxHash = "tx-001";
			os.Save(tx);
			var block = os.New<SB.Block>();
			block.Height = 1;
			block.Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			block.PreviousBlockHash = System.Text.Encoding.UTF8.GetBytes("genesis");
			block.Transactions = new[] { tx };
			os.Save(block);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Block>(), Is.EqualTo(1));
			Assert.That(objectSpace.Count<SB.Transaction>(), Is.EqualTo(1));
		}
	}

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Transaction_BackRefToBlock(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var miner = os.New<SB.Identity>();
			miner.Name = "miner1";
			miner.PublicKey = new byte[] { 0x20 };
			os.Save(miner);
			var acc1 = os.New<SB.Account>();
			acc1.AccountNumber = 8000;
			acc1.Name = "Acc1";
			acc1.Balance = 100m;
			acc1.Owner = miner;
			os.Save(acc1);
			var acc2 = os.New<SB.Account>();
			acc2.AccountNumber = 8001;
			acc2.Name = "Acc2";
			acc2.Balance = 0m;
			acc2.Owner = miner;
			os.Save(acc2);
			var tx = os.New<SB.Transaction>();
			tx.Sender = acc1;
			tx.Receiver = acc2;
			tx.Amount = 10m;
			tx.TxHash = "tx-backref";
			os.Save(tx);
			var block = os.New<SB.Block>();
			block.Height = 2;
			block.Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
			block.PreviousBlockHash = System.Text.Encoding.UTF8.GetBytes("hash1");
			block.Transactions = new[] { tx };
			os.Save(block);
			tx.OwnerBlock = block;
			os.Save(tx);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Transaction>(), Is.EqualTo(1));
			Assert.That(objectSpace.Count<SB.Block>(), Is.EqualTo(1));
		}
	}

	#endregion
	#region Complex graph loop

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void ComplexGraph_MultipleAccountsAndTransactions(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var users = new List<SB.User>();
			for (int i = 0; i < 3; i++) {
				var u = os.New<SB.User>();
				u.Name = $"user-{i}";
				u.Email = $"user{i}@graph.com";
				os.Save(u);
				users.Add(u);
			}
			var accounts = new List<SB.Account>();
			for (int i = 0; i < 3; i++) {
				var a = os.New<SB.Account>();
				a.AccountNumber = 9000 + i;
				a.Name = $"Acc-{i}";
				a.Balance = (i + 1) * 100m;
				a.Owner = users[i];
				os.Save(a);
				accounts.Add(a);
			}
			var txList = new List<SB.Transaction>();
			for (int i = 0; i < 3; i++) {
				var tx = os.New<SB.Transaction>();
				tx.Sender = accounts[i];
				tx.Receiver = accounts[(i + 1) % 3];
				tx.Amount = 5m;
				tx.TxHash = $"tx-{i}";
				os.Save(tx);
				txList.Add(tx);
			}
			var block = os.New<SB.Block>();
			block.Height = 10;
			block.Timestamp = DateTime.UtcNow;
			block.PreviousBlockHash = System.Text.Encoding.UTF8.GetBytes("prevhash");
			block.Transactions = txList.ToArray();
			os.Save(block);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.User>(), Is.EqualTo(3));
			Assert.That(objectSpace.Count<SB.Account>(), Is.EqualTo(3));
			Assert.That(objectSpace.Count<SB.Transaction>(), Is.EqualTo(3));
			Assert.That(objectSpace.Count<SB.Block>(), Is.EqualTo(1));
		}
	}

	#endregion

	#region User/Group/Permission hierarchy

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void Hierarchy_UserGroupPermission(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var admin = os.New<SB.User>();
			admin.Name = "admin";
			admin.Email = "admin@hierarchy.com";
			os.Save(admin);
			var viewer = os.New<SB.User>();
			viewer.Name = "viewer";
			viewer.Email = "viewer@hierarchy.com";
			os.Save(viewer);
			var group = os.New<SB.Group>();
			group.Name = "staff";
			group.Members = new SB.Identity[] { admin, viewer };
			os.Save(group);
			var acc = os.New<SB.Account>();
			acc.AccountNumber = 10000;
			acc.Name = "SharedAccount";
			acc.Balance = 1000m;
			acc.Owner = admin;
			os.Save(acc);
			var adminPerm = os.New<SB.Permission>();
			adminPerm.PermissionName = "admin-access";
			adminPerm.Description = "Full admin access";
			adminPerm.GrantedTo = admin;
			os.Save(adminPerm);
			var viewerPerm = os.New<SB.Permission>();
			viewerPerm.PermissionName = "viewer-access";
			viewerPerm.Description = "Read-only access";
			viewerPerm.GrantedTo = admin;
			os.Save(viewerPerm);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.User>(), Is.EqualTo(2));
			Assert.That(objectSpace.Count<SB.Group>(), Is.EqualTo(1));
			Assert.That(objectSpace.Count<SB.Account>(), Is.EqualTo(1));
			Assert.That(objectSpace.Count<SB.Permission>(), Is.EqualTo(2));
		}
	}

	#endregion

	#region Insert/Update/Delete loop

	[Test]
	[TestCaseSource(typeof(SafeBoxTestHelper), nameof(SafeBoxTestHelper.AllTestCases))]
	public void InsertUpdateDeleteLoop(TestTraits traits) {
		var objectSpaceBytes = Array.Empty<byte>();
		using (var os = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits)) {
			var ids = new List<SB.Identity>();
			for (int i = 0; i < 5; i++) {
				var id = os.New<SB.Identity>();
				id.Name = $"loop-{i}";
				id.PublicKey = new byte[] { (byte)i };
				os.Save(id);
				ids.Add(id);
			}
			Assert.That(os.Count<SB.Identity>(), Is.EqualTo(5));
			ids[0].PublicKey = new byte[] { 0xFF, 0xFE };
			os.Save(ids[0]);
			os.Delete(ids[4]);
			ids.RemoveAt(4);
			Validate(os);
			os.Flush();
			objectSpaceBytes = os.Streams.RootStream.ToArray();
		}
		using var hydratedOS = SafeBoxTestHelper.CreateSafeBoxObjectSpace(traits, new System.IO.MemoryStream(objectSpaceBytes));
		Validate(hydratedOS);
		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<SB.Identity>(), Is.EqualTo(4));
			var first = objectSpace.Get((SB.Identity x) => x.Name, "loop-0");
			Assert.That(first.PublicKey, Is.EqualTo(new byte[] { 0xFF, 0xFE }));
		}
	}

	#endregion
}
