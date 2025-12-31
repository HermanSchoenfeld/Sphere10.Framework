// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Data.NHibernate;

/// <summary>
/// Cusomize this class to control how the busiess object checksum is computed
/// </summary>
public class BufferChecksumCalculator {
	private const int MurMur3HashSeed = 37;
	public int ComputeChecksum(byte[] data) {
		// TODO: change to MurMur3(Blake2(data)) or MurMur3(MD5(data))
		return data.GetMurMurHash3(MurMur3HashSeed);
	}
}

