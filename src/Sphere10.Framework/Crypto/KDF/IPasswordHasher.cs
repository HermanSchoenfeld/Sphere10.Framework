// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Hashes and validates username/password pairs as a service, so a host registers one hasher and a test can observe or replace
/// the validations its sign-in code performs. <see cref="PasswordHasher"/> implements it with the static password-hash API.
/// </summary>
public interface IPasswordHasher {

	/// <summary>Creates a salted hash of the pair in the hasher's current format.</summary>
	string CreateHash(string username, string password);

	/// <summary>Validates the pair against a stored hash, returning false for a wrong pair or a malformed hash.</summary>
	bool ValidatePassword(string username, string password, string passwordHash);

	/// <summary>Whether a stored hash should be replaced by <see cref="CreateHash"/> after the next successful validation.</summary>
	bool NeedsRehash(string passwordHash);

}
