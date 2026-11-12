// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using Sphere10.Framework.Application;
using Sphere10.Framework.Data;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>The existing standalone settings value used by the isolated user/global persistence example.</summary>
public class Test1Settings : SettingsObject {

	/// <summary>The database provider retained by the original standalone example.</summary>
	[DefaultValue(DBMSType.Sqlite)]
	public DBMSType DBMSType { get; set; }

	/// <summary>The standalone record's optional database connection draft.</summary>
	[DefaultValue(null)]
	public string ConnectionString { get; set; }

	/// <summary>The language value edited by this demonstration.</summary>
	[DefaultValue("en")]
	public string ActiveLanguage { get; set; }

	/// <summary>Whether this standalone record represents its first execution.</summary>
	[DefaultValue(true)]
	public bool FirstTimeRun { get; set; }

	/// <summary>The demonstration counter persisted by an explicit Save.</summary>
	[DefaultValue(0)]
	public int RunCount { get; set; }

	/// <summary>The initial timestamp supplied by the existing settings defaults.</summary>
	[DefaultDate(true, 0, 0, 0, 0, 0, 0, 0)]
	public DateTime FirstRunDate { get; set; }

	/// <summary>The encrypted standalone value, excluded from screen Options and logging.</summary>
	[DefaultValue("Hello!")]
	[EncryptedString]
	public string Secret { get; set; } = "Hello!";

}
