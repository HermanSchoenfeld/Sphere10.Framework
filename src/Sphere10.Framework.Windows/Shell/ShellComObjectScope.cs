// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows;

/// <summary>Owns one Shell automation COM reference on its originating apartment.</summary>
internal readonly struct ShellComObjectScope : IScope<object> {
	private readonly object _item;

	public ShellComObjectScope(object item) {
		_item = item;
	}

	public object Item => _item;

	public void Dispose() {
		if (_item != null && Marshal.IsComObject(_item))
			Marshal.ReleaseComObject(_item);
	}

	public ValueTask DisposeAsync() => new(Task.Run(Dispose));
}
