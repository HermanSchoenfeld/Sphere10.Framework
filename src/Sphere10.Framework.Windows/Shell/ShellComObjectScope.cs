// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;

namespace Sphere10.Framework.Windows;

/// <summary>Owns one Shell automation COM reference on its originating apartment.</summary>
internal sealed class ShellComObjectScope : SyncScope, IScope<object> {
	public ShellComObjectScope(object item) { Item = item; }

	public object Item { get; private set; }

	protected override void OnScopeEnd() {
		if (Item != null && Marshal.IsComObject(Item))
			Marshal.ReleaseComObject(Item);
		Item = null;
	}
}
