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
public class ApplicationMenu<TItem> : IApplicationMenu where TItem : class, IApplicationMenuItem {
	private string _id;
	private readonly List<TItem> _items = new();

	public virtual string Id {
		get => _id ?? Text;
		set => _id = value;
	}

	public virtual string Text { get; set; } = string.Empty;

	/// <summary>Returns a membership snapshot; mutate the menu through AddItem and RemoveItem.</summary>
	public virtual TItem[] Items => _items.ToArray();

	protected TItem[] ItemCollection => _items.ToArray();

	IApplicationMenuItem[] IApplicationMenu.Items => Items;

	public virtual void AddItem(TItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		_items.Add(item);
	}

	public virtual bool ContainsItem(TItem item) => _items.Contains(item);

	public virtual void RemoveItem(TItem item) => _items.Remove(item);
}
