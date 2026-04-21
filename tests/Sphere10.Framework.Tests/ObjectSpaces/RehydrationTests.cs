// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using Sphere10.Framework.ObjectSpaces;
using NUnit.Framework;
using Sphere10.Framework;
using System.Net.Mail;

namespace Sphere10.Framework.Tests.ObjectSpaces;

/// <summary>
/// Mirror of GeneralTests but with rehydration logic. Tests perform operations on object space A,
/// flush and capture bytes, dispose A, reload as object space B, and assert on B to discover rehydration issues.
/// </summary>
[TestFixture]
public class RehydrationTests {

    #region Empty

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void Empty_Rehydration(TestTraits testTraits) {
        var objectSpaceBytes = Array.Empty<byte>();

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            // Don't add anything - just create an empty object space
            Validate(objectSpace);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            Validate(rehydrated);
        }

        void Validate(ObjectSpace objectSpace) {
            Assert.That(objectSpace.Count<Account>(), Is.EqualTo(0));
            Assert.That(objectSpace.Count<Identity>(), Is.EqualTo(0));
        }
    }

    #endregion

    #region Save

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void Save_Rehydration(TestTraits testTraits) {
        var objectSpaceBytes = Array.Empty<byte>();
        var account = TestsHelper.CreateAccount();
        var expectedName = account.Name;
        var expectedNumber = account.UniqueNumber;
        var expectedQuantity = account.Quantity;

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            Validate(rehydrated);
        }

        void Validate(ObjectSpace objectSpace) {
            Assert.That(objectSpace.Count<Account>(), Is.EqualTo(1));
            var rehydratedAccount = objectSpace.Get<Account>(0);
            Assert.That(rehydratedAccount.Name, Is.EqualTo(expectedName));
            Assert.That(rehydratedAccount.UniqueNumber, Is.EqualTo(expectedNumber));
            Assert.That(rehydratedAccount.Quantity, Is.EqualTo(expectedQuantity));
        }
    }

    #endregion

    #region Delete

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void Delete_CreateOneDelete_Rehydration(TestTraits testTraits) {
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random(31337);
        var account = TestsHelper.CreateAccount(rng);

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account);
            objectSpace.Delete(account);
            Validate(objectSpace);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            Validate(rehydrated);
        }

        void Validate(ObjectSpace objectSpace) {
            Assert.That(objectSpace.Count<Account>(), Is.EqualTo(0));
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void Delete_CreateTwoDeleteFirst_Rehydration(TestTraits testTraits) {
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random(31337);
        var account1 = TestsHelper.CreateAccount(rng);
        var account2 = TestsHelper.CreateAccount(rng);
        var expected2Name = account2.Name;

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Save(account2);
            objectSpace.Delete(account1);
            Validate(objectSpace);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            Validate(rehydrated);
        }

		void Validate(ObjectSpace objectSpace) {
			Assert.That(objectSpace.Count<Account>(), Is.EqualTo(1));
			var remainingAccount = objectSpace.Get<Account>(1);   // account 2 is still at index 1 since account 1 was REAPED
			Assert.That(remainingAccount.Name, Is.EqualTo(expected2Name));
		}
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void Delete_CreateTwoWithReference_DeleteReferenced_Rehydration(TestTraits testTraits) {
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random(31337);
        var identity = new Identity {
            DSS = DSS.PQC_WAMSSharp,
            Key = Signers.DerivePublicKey(DSS.PQC_WAMSSharp, Signers.CreatePrivateKey(DSS.PQC_WAMSSharp, Hashers.Hash(CHF.SHA2_256, "test".ToAsciiByteArray())), 0).RawBytes
        };
        var account = TestsHelper.CreateAccount(rng);

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(identity);
            account.Identity = identity;
            objectSpace.Save(account);
            objectSpace.Delete(identity);
            Validate(objectSpace);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            Validate(rehydrated);
        }

        void Validate(ObjectSpace objectSpace) {
            Assert.That(objectSpace.Count<Identity>(), Is.EqualTo(0));
            Assert.That(objectSpace.Count<Account>(), Is.EqualTo(1));
        }
    }

    #endregion

    #region Clear

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void Clear_Rehydration(TestTraits testTraits) {
        var objectSpaceBytes = Array.Empty<byte>();
        var savedAccount = TestsHelper.CreateAccount();

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(savedAccount);
            objectSpace.Clear("I CONSENT TO CLEAR ALL DATA");
            Validate(objectSpace);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            Validate(rehydrated);
        }

        void Validate(ObjectSpace objectSpace) {
            foreach (var dim in objectSpace.Dimensions)
                Assert.That(dim.Container.ObjectStream.Count, Is.EqualTo(0));
        }
    }

    #endregion

    #region Indexes - Unique Member (Checksummed)

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_Checksummed_GetViaIndex_Rehydration(TestTraits testTraits) {
        // note: string based property will use a checksum-based index since not constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random();
        var account1 = TestsHelper.CreateAccount(rng);
        account1.Name = "alpha";
        var account2 = TestsHelper.CreateAccount(rng);
        account2.Name = "beta";

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Save(account2);
            Validate(objectSpace);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            Validate(rehydrated);
        }

        void Validate(ObjectSpace objectSpace) {
            var fetch1 = objectSpace.Get((Account x) => x.Name, "alpha");
            Assert.That(fetch1.Name, Is.EqualTo("alpha"));

            var fetch2 = objectSpace.Get((Account x) => x.Name, "beta");
            Assert.That(fetch2.Name, Is.EqualTo("beta"));

            Assert.That(() => objectSpace.Get((Account x) => x.Name, "gamma"), Throws.InvalidOperationException);
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_Checksummed_ProhibitsDuplicate_ViaAdd_Rehydration(TestTraits testTraits) {
        // note: string based property will use a checksum-based index since not constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random();
        var account1 = TestsHelper.CreateAccount(rng);
        account1.Name = "alpha";

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var account2 = TestsHelper.CreateAccount(rng);
            account2.Name = "alpha";
            Assert.That(() => rehydrated.Save(account2), Throws.InvalidOperationException);
            if (testTraits.HasFlag(TestTraits.PersistentIgnorant))
                rehydrated.AutoSave = false;
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_Checksummed_ProhibitsDuplicate_ViaUpdate_1_Rehydration(TestTraits testTraits) {
        // note: string based property will use a checksum-based index since not constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random();
        var account1 = TestsHelper.CreateAccount(rng);
        account1.Name = "alpha";
        var account2 = TestsHelper.CreateAccount(rng);
        account2.Name = "beta";

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Save(account2);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var rehydratedAccount1 = rehydrated.Get((Account x) => x.Name, "alpha");
            rehydratedAccount1.Name = "beta";
            Assert.That(() => rehydrated.Save(rehydratedAccount1), Throws.InvalidOperationException);
            if (testTraits.HasFlag(TestTraits.PersistentIgnorant))
                rehydrated.AutoSave = false;
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_Checksummed_ProhibitsDuplicate_ViaUpdate_2_Rehydration(TestTraits testTraits) {
        // note: string based property will use a checksum-based index since not constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random();
        var account1 = TestsHelper.CreateAccount(rng);
        account1.Name = "alpha";
        var account2 = TestsHelper.CreateAccount(rng);
        account2.Name = "beta";

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Save(account2);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var rehydratedAccount2 = rehydrated.Get((Account x) => x.Name, "beta");
            rehydratedAccount2.Name = "alpha";
            Assert.That(() => rehydrated.Save(rehydratedAccount2), Throws.InvalidOperationException);
            if (testTraits.HasFlag(TestTraits.PersistentIgnorant))
                rehydrated.AutoSave = false;
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_Checksummed_AllowsUpdate_ThrowsNothing_Rehydration(TestTraits testTraits) {
        // note: string based property will use a checksum-based index since not constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random();
        var account1 = TestsHelper.CreateAccount(rng);
        account1.Name = "alpha";

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var rehydratedAccount1 = rehydrated.Get((Account x) => x.Name, "alpha");
            Assert.That(() => rehydrated.Save(rehydratedAccount1), Throws.Nothing);
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_Checksummed_SaveThenDeleteThenSave_ThrowsNothing_Rehydration(TestTraits testTraits) {
        // note: string based property will use a checksum-based index since not constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random();
        var account1 = TestsHelper.CreateAccount(rng);
        account1.Name = "alpha";
        var account2 = TestsHelper.CreateAccount(rng);
        account2.Name = "beta";

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Save(account2);
            objectSpace.Delete(account1);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var account3 = TestsHelper.CreateAccount(rng);
            account3.Name = "alpha";
            Assert.That(() => rehydrated.Save(account3), Throws.Nothing);
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_Checksummed_IgnoreNullPolicy_Rehydration(TestTraits testTraits) {
        // note: string based property will use a checksum-based index since not constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random();
        var account1 = TestsHelper.CreateAccount(rng);
        account1.Name = null;

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits, nullPolicy: IndexNullPolicy.IgnoreNull)) {
            objectSpace.Save(account1);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) }, IndexNullPolicy.IgnoreNull)) {
            var account2 = TestsHelper.CreateAccount(rng);
            account2.Name = null;
            Assert.That(() => rehydrated.Save(account2), Throws.Nothing);
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_Checksummed_IndexNullValue_Rehydration(TestTraits testTraits) {
        // note: string based property will use a checksum-based index since not constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random();
        var account1 = TestsHelper.CreateAccount(rng);
        account1.Name = null;

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits, nullPolicy: IndexNullPolicy.IndexNullValue)) {
            objectSpace.Save(account1);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) }, IndexNullPolicy.IndexNullValue)) {
            var account2 = TestsHelper.CreateAccount(rng);
            account2.Name = null;
            Assert.That(() => rehydrated.Save(account2), Throws.InvalidOperationException);
            if (testTraits.HasFlag(TestTraits.PersistentIgnorant))
                rehydrated.AutoSave = false;
        }
    }

    #endregion

    #region Indexes - Unique Member

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_GetViaIndex_Rehydration(TestTraits testTraits) {
        // note: long based property will index the property value (not checksum) since constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random();
        var account1 = TestsHelper.CreateAccount(rng);
        account1.Name = "alpha";
		account1.UniqueNumber = 1;
        var account2 = TestsHelper.CreateAccount(rng);
		account2.Name = "beta";
        account2.UniqueNumber = 2;

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Save(account2);
            Validate(objectSpace);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            Validate(rehydrated);
        }

        void Validate(ObjectSpace objectSpace) {
            var fetch1 = objectSpace.Get((Account x) => x.UniqueNumber, 1L);
            Assert.That(fetch1.Name, Is.EqualTo("alpha"));
			Assert.That(fetch1.UniqueNumber, Is.EqualTo(1));

            var fetch2 = objectSpace.Get((Account x) => x.UniqueNumber, 2L);
			Assert.That(fetch2.Name, Is.EqualTo("beta"));
            Assert.That(fetch2.UniqueNumber, Is.EqualTo(2));

            Assert.That(() => objectSpace.Get((Account x) => x.UniqueNumber, 3L), Throws.InvalidOperationException);
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_ProhibitsDuplicate_ViaAdd_Rehydration(TestTraits testTraits) {
        // note: long based property will index the property value (not checksum) since constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random(31337);
        var account1 = TestsHelper.CreateAccount(rng);
        var uniqueNum = account1.UniqueNumber;

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var account2 = TestsHelper.CreateAccount(rng);
            account2.UniqueNumber = uniqueNum;
            Assert.That(() => rehydrated.Save(account2), Throws.InvalidOperationException);
            if (testTraits.HasFlag(TestTraits.PersistentIgnorant))
                rehydrated.AutoSave = false;
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_ProhibitsDuplicate_ViaUpdate_1_Rehydration(TestTraits testTraits) {
        // note: long based property will index the property value (not checksum) since constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random(31337);
        var account1 = TestsHelper.CreateAccount(rng);
        var account2 = TestsHelper.CreateAccount(rng);

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Save(account2);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var rehydratedAccount1 = rehydrated.Get<Account>(0);
            var rehydratedAccount2 = rehydrated.Get<Account>(1);
            rehydratedAccount1.UniqueNumber = rehydratedAccount2.UniqueNumber;

            Assert.That(() => rehydrated.Save(rehydratedAccount1), Throws.InvalidOperationException);
            if (testTraits.HasFlag(TestTraits.PersistentIgnorant))
                rehydrated.AutoSave = false;
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_ProhibitsDuplicate_ViaUpdate_2_Rehydration(TestTraits testTraits) {
        // note: long based property will index the property value (not checksum) since constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random(31337);
        var account1 = TestsHelper.CreateAccount(rng);
        var account2 = TestsHelper.CreateAccount(rng);

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Save(account2);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var rehydratedAccount1 = rehydrated.Get<Account>(0);
            var rehydratedAccount2 = rehydrated.Get<Account>(1);
            rehydratedAccount2.UniqueNumber = rehydratedAccount1.UniqueNumber;

            Assert.That(() => rehydrated.Save(rehydratedAccount2), Throws.InvalidOperationException);
            if (testTraits.HasFlag(TestTraits.PersistentIgnorant))
                rehydrated.AutoSave = false;
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_AllowsUpdate_ThrowsNothing_Rehydration(TestTraits testTraits) {
        // note: long based property will index the property value (not checksum) since constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random(31337);
        var account1 = TestsHelper.CreateAccount(rng);

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var rehydratedAccount1 = rehydrated.Get<Account>(0);
            Assert.That(() => rehydrated.Save(rehydratedAccount1), Throws.Nothing);
        }
    }

    [Test]
    [TestCaseSource(typeof(TestsHelper), nameof(TestsHelper.MemoryMappedTestCases))]
    public void UniqueMember_SaveThenDeleteThenSave_ThrowsNothing_Rehydration(TestTraits testTraits) {
        // note: long based property will index the property value (not checksum) since constant length key
        var objectSpaceBytes = Array.Empty<byte>();
        var rng = new Random(31337);
        var account1 = TestsHelper.CreateAccount(rng);
        var account2 = TestsHelper.CreateAccount(rng);
        account1.UniqueNumber = 1;
        account2.UniqueNumber = 2;

        using (var objectSpace = TestsHelper.CreateObjectSpace(testTraits)) {
            objectSpace.Save(account1);
            objectSpace.Save(account2);
            objectSpace.Delete(account1);
            objectSpace.Flush();
            objectSpaceBytes = objectSpace.Streams.RootStream.ToArray();
        }

        using (var rehydrated = TestsHelper.CreateObjectSpace(testTraits, new Dictionary<string, object> { ["stream"] = new MemoryStream(objectSpaceBytes) })) {
            var account3 = TestsHelper.CreateAccount(rng);
            account3.UniqueNumber = 1;
            Assert.That(() => rehydrated.Save(account3), Throws.Nothing);
        }
    }

    #endregion

}
