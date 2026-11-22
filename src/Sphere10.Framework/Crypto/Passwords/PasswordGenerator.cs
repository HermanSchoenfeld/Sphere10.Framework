// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.Maths;

namespace Sphere10.Framework;

/// <summary>
/// Generates cryptographically random passwords that meet a <see cref="PasswordPolicy"/>, for accounts whose password nobody
/// types (invited users who choose their own through a reset link, machine accounts) and for suggested passwords. Each
/// character class the policy requires appears at least once; the remaining characters are drawn uniformly from letters and
/// digits, and from symbols too when the policy requires one. Generated passwords never contain whitespace.
/// </summary>
public static class PasswordGenerator {
	public const int DefaultLength = 24;
	private const string LowerCase = "abcdefghijklmnopqrstuvwxyz";
	private const string UpperCase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
	private const string Digits = "0123456789";
	private const string Symbols = "!#$%&*+-=?@^_~";

	/// <summary>Generates a password of <paramref name="length"/> characters that meets <see cref="PasswordPolicy.Default"/>.</summary>
	public static string Generate(int length = DefaultLength) => Generate(PasswordPolicy.Default, length);

	/// <summary>
	/// Generates a password that meets <paramref name="policy"/>, of <paramref name="length"/> characters or the policy's minimum
	/// length when that is longer.
	/// </summary>
	public static string Generate(PasswordPolicy policy, int length = DefaultLength) {
		Guard.ArgumentNotNull(policy, nameof(policy));
		Guard.ArgumentGT(length, 0, nameof(length));
		var required = new List<string>();
		if (policy.RequiresLowerCase)
			required.Add(LowerCase);
		if (policy.RequiresUpperCase)
			required.Add(UpperCase);
		if (policy.RequiresDigit)
			required.Add(Digits);
		if (policy.RequiresSymbol)
			required.Add(Symbols);
		var alphabet = LowerCase + UpperCase + Digits + (policy.RequiresSymbol ? Symbols : string.Empty);
		var characters = new char[Math.Max(Math.Max(length, policy.MinimumLength), required.Count)];
		var random = new SystemCRNG();
		for (var i = 0; i < characters.Length; i++) {
			var source = i < required.Count ? required[i] : alphabet;
			characters[i] = source[NextIndex(random, source.Length)];
		}
		// Shuffle (Fisher-Yates) so the required characters do not always lead the password.
		for (var i = characters.Length - 1; i > 0; i--) {
			var j = NextIndex(random, i + 1);
			(characters[i], characters[j]) = (characters[j], characters[i]);
		}
		return new string(characters);
	}

	// A uniform index below count: values from the incomplete last block of 2^32 are rejected, so no index is favoured.
	private static int NextIndex(IRandomNumberGenerator random, int count) {
		var limit = uint.MaxValue - uint.MaxValue % (uint)count;
		uint value;
		do {
			value = random.NextUInt32();
		} while (value >= limit);
		return (int)(value % (uint)count);
	}
}
