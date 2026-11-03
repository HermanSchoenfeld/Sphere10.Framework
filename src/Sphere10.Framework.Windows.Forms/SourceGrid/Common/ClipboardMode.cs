// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

[Flags]
public enum ClipboardMode {
	None = 0,

	/// <summary>
	/// Copy using Ctrl+C
	/// </summary>
	Copy = 1,

	/// <summary>
	/// Cut using Ctrl+X
	/// </summary>
	Cut = 2,

	/// <summary>
	/// Paste using Ctrl+V
	/// </summary>
	Paste = 4,

	/// <summary>
	/// Delete using Del key
	/// </summary>
	Delete = 8,
	All = 15
}
