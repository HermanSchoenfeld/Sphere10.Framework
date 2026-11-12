// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Data.Common;
using NHibernate.Engine;
using NHibernate.Type;

namespace Sphere10.Framework.Data.NHibernate;

/// <summary>Reads decimal columns through the database provider's decimal conversion.</summary>
[Serializable]
public class DecimalColumnType : DecimalType {
	// NHibernate's object conversion can receive a double from SQLite NUMERIC columns.
	// .NET 11 preserves that binary approximation; GetDecimal retains the provider's decimal text interpretation.
	public override object Get(DbDataReader reader, int index, ISessionImplementor session) => reader.GetDecimal(index);
}
