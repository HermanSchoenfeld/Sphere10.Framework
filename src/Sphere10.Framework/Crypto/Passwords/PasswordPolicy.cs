// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// The rules a chosen password must meet: a minimum length, the character classes it must contain and whether it may contain
/// whitespace. <see cref="Validate"/> returns a <see cref="Result"/> with one error message per broken rule, so a caller can
/// show every reason at once. The defaults (12 or more characters with a digit, a lower case and an upper case letter, no
/// whitespace) are the usual policy; a host configures another with the init properties, and a subclass adds rules by
/// overriding <see cref="Validate"/>. <see cref="PasswordGenerator"/> generates passwords that meet a policy.
/// </summary>
public class PasswordPolicy {

	/// <summary>The default policy, whose values are the property defaults.</summary>
	public static PasswordPolicy Default { get; } = new();

	/// <summary>The fewest characters a password may have.</summary>
	public int MinimumLength { get; init; } = 12;

	/// <summary>Whether a password needs at least one digit.</summary>
	public bool RequiresDigit { get; init; } = true;

	/// <summary>Whether a password needs at least one lower case letter.</summary>
	public bool RequiresLowerCase { get; init; } = true;

	/// <summary>Whether a password needs at least one upper case letter.</summary>
	public bool RequiresUpperCase { get; init; } = true;

	/// <summary>Whether a password needs at least one symbol, a character that is neither a letter, a digit nor whitespace.</summary>
	public bool RequiresSymbol { get; init; }

	/// <summary>Whether a password may contain spaces, tabs or other whitespace.</summary>
	public bool AllowsWhitespace { get; init; }

	/// <summary>Checks a password against the policy; a null password is checked as empty.</summary>
	/// <returns>A successful result, or one error per broken rule.</returns>
	public virtual Result Validate(string password) {
		password ??= string.Empty;
		var result = Result.Default;
		if (RequiresDigit && !password.Any(char.IsDigit))
			result.AddError("Password needs at least 1 digit");
		if (RequiresLowerCase && !password.Any(char.IsLower))
			result.AddError("Password needs at least 1 lower case letter");
		if (RequiresUpperCase && !password.Any(char.IsUpper))
			result.AddError("Password needs at least 1 upper case letter");
		if (RequiresSymbol && !password.Any(IsSymbol))
			result.AddError("Password needs at least 1 symbol");
		if (!AllowsWhitespace && password.Any(char.IsWhiteSpace))
			result.AddError("Password cannot have spaces, tabs or other whitespace");
		if (password.Length < MinimumLength)
			result.AddError($"Passwords need to contain {MinimumLength} or more characters");
		return result;
	}

	/// <summary>Whether a character counts as a symbol for <see cref="RequiresSymbol"/>.</summary>
	public static bool IsSymbol(char character) => !char.IsLetterOrDigit(character) && !char.IsWhiteSpace(character);
}
