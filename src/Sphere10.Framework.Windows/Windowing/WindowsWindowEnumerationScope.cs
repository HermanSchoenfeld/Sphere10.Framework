// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>The native window families included in a desktop inventory.</summary>
[Flags]
public enum WindowsWindowEnumerationScope {
	TopLevel = 1,
	Descendants = 2,
	MessageOnly = 4,
	All = TopLevel | Descendants | MessageOnly
}
