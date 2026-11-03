// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

public class OldNewEventArgs<T> : EventArgs {
	public OldNewEventArgs(T oldValue, T newValue) {
		OldValue = oldValue;
		NewValue = newValue;
	}

	public T OldValue { get; protected set; } = default(T);

	public T NewValue { get; protected set; } = default(T);
}


public delegate void OldNewEventHandler<T>(object sender, OldNewEventArgs<T> e);

