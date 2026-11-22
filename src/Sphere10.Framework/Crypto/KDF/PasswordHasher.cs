// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Sphere10.Framework;

/// <summary>
/// Salted PBKDF2 password hashes. New hashes use PBKDF2-SHA512 in the format
/// <c>SP10-PBKDF2-SHA512:&lt;iterations&gt;:&lt;salt base64&gt;:&lt;hash base64&gt;</c> with a 32-byte salt and hash; the legacy
/// PBKDF2-SHA1 format <c>&lt;iterations&gt;:&lt;salt base64&gt;:&lt;hash base64&gt;</c> still validates, and <see cref="NeedsRehash"/>
/// tells a caller to replace a stored hash with a current one after the next successful validation.
/// </summary>
/// <remarks>
/// The static members are the API. An instance implements <see cref="IPasswordHasher"/> by forwarding to them, so a host can
/// register the hasher as a service and a test can observe or replace it. A username/password pair is hashed as the secret
/// <c>username:password</c>, so the same pair always derives the same hash for a given salt in either format.
/// </remarks>
public class PasswordHasher : IPasswordHasher {
	// The following constants may be changed without breaking existing hashes: each hash records its own iteration count,
	// and a hash made with fewer iterations than PBKDF2_ITERATIONS reports NeedsRehash.
	public const string FORMAT_PREFIX = "SP10-PBKDF2-SHA512";
	public const int SALT_BYTE_SIZE = 32;
	public const int HASH_BYTE_SIZE = 32;
	public const int PBKDF2_ITERATIONS = 210000;

	// Bounds on the iteration count a stored hash may claim, so a tampered hash cannot make validation unboundedly slow and a
	// current-format hash cannot claim a weak count.
	private const int MinIterations = 100000;
	private const int MaxIterations = 1000000;
	private const int LegacyMaxIterations = 1000000;

	/// <summary>
	/// A well-formed hash in the current format and parameters that no real password derives to. Validating against it costs the
	/// same as validating a stored hash, so a caller can reject an unknown account without a timing difference.
	/// </summary>
	public static string PlaceholderHash { get; } = Format(PBKDF2_ITERATIONS, new byte[SALT_BYTE_SIZE], new byte[HASH_BYTE_SIZE]);

	/// <summary>Creates a salted hash of the username/password pair in the current format.</summary>
	public static string CreateHash(string username, string password) {
		Guard.ArgumentNotNull(username, nameof(username));
		Guard.ArgumentNotNull(password, nameof(password));
		return CreateHash($"{username}:{password}");
	}

	/// <summary>Creates a salted hash of the secret in the current format.</summary>
	public static string CreateHash(string secret) {
		Guard.ArgumentNotNull(secret, nameof(secret));
		var salt = Tools.Crypto.GenerateCryptographicallyRandomBytes(SALT_BYTE_SIZE);
		var hash = PBKDF2.DeriveKey(Encoding.UTF8.GetBytes(secret), salt, PBKDF2_ITERATIONS, HASH_BYTE_SIZE, HashAlgorithmName.SHA512);
		return Format(PBKDF2_ITERATIONS, salt, hash);
	}

	/// <summary>Validates a username/password pair against a hash in the current or the legacy format.</summary>
	/// <returns>True if the pair derives the hash; false for a wrong pair, a null argument or a malformed hash.</returns>
	public static bool ValidatePassword(string username, string password, string correctHash)
		=> username != null && password != null && ValidatePassword($"{username}:{password}", correctHash);

	/// <summary>Validates a secret against a hash in the current or the legacy format.</summary>
	/// <returns>True if the secret derives the hash; false for a wrong secret, a null argument or a malformed hash.</returns>
	public static bool ValidatePassword(string secret, string correctHash) {
		if (secret == null || string.IsNullOrWhiteSpace(correctHash))
			return false;
		try {
			var parts = correctHash.Split(':');
			switch (parts.Length) {
				case 3: {
					if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var legacyIterations) || legacyIterations < 1 || legacyIterations > LegacyMaxIterations)
						return false;
					var legacySalt = Convert.FromBase64String(parts[1]);
					var legacyHash = Convert.FromBase64String(parts[2]);
					if (legacyHash.Length == 0)
						return false;
					return Tools.Crypto.FixedTimeEquals(PBKDF2.DeriveKey(secret, legacySalt, legacyIterations, legacyHash.Length), legacyHash);
				}
				case 4: {
					if (parts[0] != FORMAT_PREFIX || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) || iterations < MinIterations || iterations > MaxIterations)
						return false;
					var salt = Convert.FromBase64String(parts[2]);
					var hash = Convert.FromBase64String(parts[3]);
					if (salt.Length != SALT_BYTE_SIZE || hash.Length != HASH_BYTE_SIZE)
						return false;
					return Tools.Crypto.FixedTimeEquals(PBKDF2.DeriveKey(Encoding.UTF8.GetBytes(secret), salt, iterations, hash.Length, HashAlgorithmName.SHA512), hash);
				}
				default:
					return false;
			}
		} catch (FormatException) {
			return false;
		} catch (ArgumentException) {
			return false;
		}
	}

	/// <summary>Whether a stored hash should be replaced by a current one: true unless it is in the current format and iteration count.</summary>
	public static bool NeedsRehash(string hash)
		=> hash == null || !hash.StartsWith(FORMAT_PREFIX + ":" + PBKDF2_ITERATIONS.ToString(CultureInfo.InvariantCulture) + ":", StringComparison.Ordinal);

	/// <inheritdoc />
	string IPasswordHasher.CreateHash(string username, string password) => CreateHash(username, password);

	/// <inheritdoc />
	bool IPasswordHasher.ValidatePassword(string username, string password, string passwordHash) => ValidatePassword(username, password, passwordHash);

	/// <inheritdoc />
	bool IPasswordHasher.NeedsRehash(string passwordHash) => NeedsRehash(passwordHash);

	private static string Format(int iterations, byte[] salt, byte[] hash)
		=> string.Join(":", FORMAT_PREFIX, iterations.ToString(CultureInfo.InvariantCulture), Convert.ToBase64String(salt), Convert.ToBase64String(hash));
}
