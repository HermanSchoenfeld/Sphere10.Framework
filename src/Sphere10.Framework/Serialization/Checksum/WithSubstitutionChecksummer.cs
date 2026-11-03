// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

public class WithSubstitutionChecksummer<TItem> : ItemChecksummerDecorator<TItem> {
	public WithSubstitutionChecksummer(IItemChecksummer<TItem> innerChecksummer, int reservedChecksumValue, int substitutionChecksumValue)
		: base(innerChecksummer) {
		ReservedChecksum = reservedChecksumValue;
		SubstitutionChecksum = substitutionChecksumValue;
	}

	public int ReservedChecksum { get; }

	public int SubstitutionChecksum { get; }

	public override int CalculateChecksum(TItem item) {
		var checksum = base.CalculateChecksum(item);
		if (checksum == ReservedChecksum) {
			return SubstitutionChecksum;
		}
		return checksum;
	}	
}

