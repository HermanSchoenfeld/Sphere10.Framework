// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public class ActionChecksum<TItem> : IItemChecksummer<TItem> {
	private readonly Func<TItem, int> _actionChecksum;

	public ActionChecksum(Func<TItem, int> actionChecksum) {
		_actionChecksum = actionChecksum;
	}

	public int CalculateChecksum(TItem item) => _actionChecksum(item);

}

