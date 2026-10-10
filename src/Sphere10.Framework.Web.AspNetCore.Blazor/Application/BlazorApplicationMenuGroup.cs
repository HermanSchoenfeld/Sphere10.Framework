// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A group of action commands, rendered as a dropdown on application toolbars.</summary>
public sealed class BlazorApplicationMenuGroup : BlazorApplicationMenuItemBase {
	private readonly IBlazorApplicationMenuItem[] _items = Array.Empty<IBlazorApplicationMenuItem>();

	public IBlazorApplicationMenuItem[] Items {
		get => _items.ToArray();
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			_items = value.ToArray();
			if (Parent != null)
				foreach (var item in _items)
					if (item.Parent == null)
						item.Parent = Parent;
		}
	}

	protected override void OnParentChanged(IApplicationMenu previous, IApplicationMenu current) {
		base.OnParentChanged(previous, current);
		foreach (var item in _items)
			if (item.Parent == null || ReferenceEquals(item.Parent, previous))
				item.Parent = (IBlazorApplicationMenu)current;
	}
}
