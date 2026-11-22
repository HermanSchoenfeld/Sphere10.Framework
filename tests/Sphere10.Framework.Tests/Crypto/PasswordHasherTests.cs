// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture, Parallelizable(ParallelScope.Children)]
public class PasswordHasherTests {
	private const string Username = "ALICE@EXAMPLE.TEST";
	private const string Password = "Example-Password-42";

	// Computed independently (Python hashlib.pbkdf2_hmac) for "ALICE@EXAMPLE.TEST:Example-Password-42": PBKDF2-HMAC-SHA512,
	// 210000 iterations, salt bytes 0..31, 32-byte hash. Hashes stored in this format must keep validating.
	private const string CurrentFormatVector = "SP10-PBKDF2-SHA512:210000:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=:oHognQNvzNs44Ez4eOHSshwWMtw8Du4EIDvQNuZYGFM=";

	// The same pair in the legacy format: PBKDF2-HMAC-SHA1, 1000 iterations, salt bytes 100..123, 24-byte hash.
	private const string LegacyFormatVector = "1000:ZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXp7:SMuheFFbL1lZiV6Pq1nPPkI57pmoEZch";

	[Test]
	public void ANewHashRoundTripsInTheCurrentFormat() {
		var hash = PasswordHasher.CreateHash(Username, Password);
		Assert.That(hash, Does.StartWith("SP10-PBKDF2-SHA512:210000:"));
		Assert.That(hash.Split(':'), Has.Length.EqualTo(4));
		Assert.That(PasswordHasher.ValidatePassword(Username, Password, hash), Is.True);
		Assert.That(PasswordHasher.NeedsRehash(hash), Is.False);
	}

	[Test]
	public void NewHashesOfOnePairUseDifferentSalts()
		=> Assert.That(PasswordHasher.CreateHash(Username, Password), Is.Not.EqualTo(PasswordHasher.CreateHash(Username, Password)));

	[Test]
	public void ASecretHashRoundTrips() {
		var hash = Tools.Crypto.PasswordHash(Password);
		Assert.That(Tools.Crypto.ValidatePassword(Password, hash), Is.True);
		Assert.That(Tools.Crypto.ValidatePassword(Password + "!", hash), Is.False);
	}

	[Test]
	public void AStoredCurrentFormatHashValidates() {
		Assert.That(PasswordHasher.ValidatePassword(Username, Password, CurrentFormatVector), Is.True);
		Assert.That(PasswordHasher.NeedsRehash(CurrentFormatVector), Is.False);
	}

	[Test]
	public void AStoredLegacyHashValidatesAndNeedsRehash() {
		Assert.That(PasswordHasher.ValidatePassword(Username, Password, LegacyFormatVector), Is.True);
		Assert.That(PasswordHasher.NeedsRehash(LegacyFormatVector), Is.True);
	}

	[TestCase(CurrentFormatVector)]
	[TestCase(LegacyFormatVector)]
	public void AWrongPasswordFails(string hash) {
		Assert.That(PasswordHasher.ValidatePassword(Username, Password + "x", hash), Is.False);
		Assert.That(PasswordHasher.ValidatePassword("BOB@EXAMPLE.TEST", Password, hash), Is.False);
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase("not-a-hash")]
	[TestCase("1000:not base64:AAAA")]
	[TestCase("SP10-PBKDF2-SHA512:210000:AAAA:AAAA")]
	[TestCase("SP10-PBKDF2-SHA512:1000:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=:oHognQNvzNs44Ez4eOHSshwWMtw8Du4EIDvQNuZYGFM=")]
	[TestCase("SP10-PBKDF2-SHA1:210000:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=:oHognQNvzNs44Ez4eOHSshwWMtw8Du4EIDvQNuZYGFM=")]
	[TestCase("0:ZGVmZ2hpamtsbW5vcHFyc3R1dnd4eXp7:SMuheFFbL1lZiV6Pq1nPPkI57pmoEZch")]
	public void AMalformedOrWeakenedHashFailsWithoutThrowing(string hash)
		=> Assert.That(PasswordHasher.ValidatePassword(Username, Password, hash), Is.False);

	[Test]
	public void NullCredentialsFail() {
		Assert.That(PasswordHasher.ValidatePassword(null, Password, CurrentFormatVector), Is.False);
		Assert.That(PasswordHasher.ValidatePassword(Username, null, CurrentFormatVector), Is.False);
	}

	[Test]
	public void ThePlaceholderIsCurrentAndMatchesNoPassword() {
		Assert.That(PasswordHasher.NeedsRehash(PasswordHasher.PlaceholderHash), Is.False);
		Assert.That(PasswordHasher.ValidatePassword(Username, Password, PasswordHasher.PlaceholderHash), Is.False);
		Assert.That(PasswordHasher.ValidatePassword(string.Empty, string.Empty, PasswordHasher.PlaceholderHash), Is.False);
	}

	[TestCase(null, true)]
	[TestCase("", true)]
	[TestCase(LegacyFormatVector, true)]
	[TestCase("SP10-PBKDF2-SHA512:100000:AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8=:oHognQNvzNs44Ez4eOHSshwWMtw8Du4EIDvQNuZYGFM=", true)]
	[TestCase(CurrentFormatVector, false)]
	public void NeedsRehashReportsHashesOtherThanTheCurrentFormatAndIterations(string hash, bool expected)
		=> Assert.That(PasswordHasher.NeedsRehash(hash), Is.EqualTo(expected));

	[Test]
	public void TheServiceForwardsToTheStaticApi() {
		IPasswordHasher hasher = new PasswordHasher();
		var hash = hasher.CreateHash(Username, Password);
		Assert.That(hasher.ValidatePassword(Username, Password, hash), Is.True);
		Assert.That(hasher.ValidatePassword(Username, Password, LegacyFormatVector), Is.True);
		Assert.That(hasher.ValidatePassword(Username, "wrong", hash), Is.False);
		Assert.That(hasher.NeedsRehash(hash), Is.False);
		Assert.That(hasher.NeedsRehash(LegacyFormatVector), Is.True);
	}
}
