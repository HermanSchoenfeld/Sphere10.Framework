// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Data;

/// <summary>
/// Binary data not valid exception
/// </summary>
[Serializable]
public class BinaryDataSetInvalidException : ApplicationException {
	/// <summary>
	/// Constructor
	/// </summary>
	public BinaryDataSetInvalidException()
		: base("Binary data not valid") {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="innerException"></param>
	public BinaryDataSetInvalidException(Exception innerException)
		: base("Binary data not valid", innerException) {
	}
}
