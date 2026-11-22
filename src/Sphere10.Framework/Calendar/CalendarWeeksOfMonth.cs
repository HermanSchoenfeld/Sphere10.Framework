// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;

namespace Sphere10.Framework;

/// <summary>A set of weeks within a month, counted from its first day. <see cref="Last"/> is the final seven days.</summary>
[Flags]
public enum CalendarWeeksOfMonth {
	None = 0,
	First = 1,
	Second = 2,
	Third = 4,
	Fourth = 8,
	Last = 16
}
