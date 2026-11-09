// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Data;

/// <summary>
/// Where a dialect places the fragment that <see cref="ISQLBuilder.EmitQueryResultLimit"/> writes within a SELECT statement.
/// </summary>
public enum SQLQueryResultLimitPosition {
	/// <summary>Directly after SELECT and before DISTINCT, as in Firebird's FIRST and SKIP.</summary>
	BeforeDistinct,

	/// <summary>After DISTINCT and before the selected columns, as in SQL Server's TOP.</summary>
	AfterDistinct,

	/// <summary>After the ORDER BY clause, as in LIMIT and OFFSET or OFFSET and FETCH.</summary>
	EndOfQuery
}
