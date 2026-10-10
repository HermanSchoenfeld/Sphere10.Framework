// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Application.UI;

/// <summary>Reusable menu metadata and item storage, independent of controls and images.</summary>
public abstract class ApplicationMenuBase<TItem> : IApplicationMenu<TItem>
	where TItem : class, IApplicationMenuItem {
	private readonly List<TItem> _items = new();

	public virtual IApplicationBlock Parent { get; set; }

	public virtual string Text { get; set; } = string.Empty;

	/// <summary>Returns a membership snapshot; mutate the menu through AddItem and RemoveItem.</summary>
	public virtual TItem[] Items => _items.ToArray();

	protected TItem[] ItemCollection => _items.ToArray();

	/// <summary>Facades override this to bind children to the public menu.</summary>
	protected virtual IApplicationMenu ItemOwner => this;

	/// <summary>Presentation projections borrow existing commands without changing their ownership.</summary>
	protected virtual bool OwnsItems => true;

	public virtual void AddItem(TItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		Guard.Argument(!_items.Exists(candidate => ReferenceEquals(candidate, item)), nameof(item), "The item is already in this menu.");
		if (OwnsItems) {
			Guard.Argument(item.Parent == null || ReferenceEquals(item.Parent, ItemOwner), nameof(item), "The item already belongs to another menu.");
			item.Parent = ItemOwner;
			if (item is IScreenMenuItem screen)
				screen.ScreenDefinition?.AddActivator(item);
		}
		_items.Add(item);
	}

	public virtual bool ContainsItem(TItem item) => _items.Exists(candidate => ReferenceEquals(candidate, item));

	public virtual void RemoveItem(TItem item) {
		var index = _items.FindIndex(candidate => ReferenceEquals(candidate, item));
		if (index < 0)
			return;
		_items.RemoveAt(index);
		if (OwnsItems && ReferenceEquals(item.Parent, ItemOwner)) {
			if (item is IScreenMenuItem screen)
				screen.ScreenDefinition?.RemoveActivator(item);
			item.Parent = null;
		}
	}
}
