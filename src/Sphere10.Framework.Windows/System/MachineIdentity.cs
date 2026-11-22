// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sphere10.Framework.Windows;

/// <summary>
/// The identifier composition shared by the Windows <see cref="IMachineIdentifierGenerator"/> implementations: normalises
/// named hardware values, drops firmware placeholders and hashes the rest into upper-case SHA-256 hexadecimal.
/// </summary>
/// <remarks>
/// Each value is trimmed and upper-cased; empty values, known placeholders and all-zero or all-F identifiers are dropped.
/// The remaining values become one <c>Name=Value</c> line each, deduplicated and ordered by ordinal comparison so that WMI
/// enumeration order never changes the result, and the lines joined by line feeds are hashed with <see cref="CHF.SHA2_256"/>.
/// Identifiers that services store depend on this composition, so it never changes.
/// </remarks>
internal static class MachineIdentity {
	internal sealed record Component(string Name, string Value);

	private static readonly HashSet<string> PlaceholderValues = new(StringComparer.Ordinal) {
		"TO BE FILLED BY O.E.M.", "DEFAULT STRING", "NONE", "NOT SPECIFIED", "NOT APPLICABLE", "N/A", "SYSTEM SERIAL NUMBER", "BASE BOARD SERIAL NUMBER"
	};

	/// <summary>Normalises, filters, orders and hashes identity values into the upper-case hexadecimal identifier.</summary>
	/// <exception cref="InvalidOperationException">No value remains after placeholders are dropped.</exception>
	public static string ComputeIdentifier(IEnumerable<Component> components) {
		// Keep each distinct meaningful value once, in an order that does not depend on how WMI enumerated it.
		var lines = components
			.Select(component => component with { Value = Normalize(component.Value) })
			.Where(component => !IsPlaceholder(component.Value))
			.Select(component => $"{component.Name}={component.Value}")
			.Distinct(StringComparer.Ordinal)
			.OrderBy(line => line, StringComparer.Ordinal)
			.ToArray();
		Guard.Ensure(lines.Length > 0, "No machine identity source yielded a value.");

		// The identifier is the SHA-256 of the lines, so it reveals none of the hardware values.
		var digest = Hashers.Hash(CHF.SHA2_256, Encoding.UTF8.GetBytes(string.Join("\n", lines)));
		return digest.ToHexString(true).ToUpperInvariant();
	}

	public static string Normalize(string value) => value?.Trim().ToUpperInvariant() ?? string.Empty;

	/// <summary>Whether a normalised value is empty, a known firmware placeholder, or an all-zero or all-F identifier.</summary>
	public static bool IsPlaceholder(string normalizedValue) {
		if (normalizedValue.Length == 0 || PlaceholderValues.Contains(normalizedValue))
			return true;

		// Unset UUIDs, serial numbers and MAC addresses are all zeros or all Fs once their separators are ignored.
		var digits = normalizedValue.Where(character => character is not ('-' or ':' or ' ')).ToArray();
		return digits.Length == 0 || digits.All(character => character == '0') || digits.All(character => character == 'F');
	}

}
