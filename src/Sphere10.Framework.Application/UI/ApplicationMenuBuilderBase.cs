// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Application.UI;

public abstract class ApplicationMenuBuilderBase<TItem, TMenu> where TItem : IApplicationMenuItem where TMenu : IApplicationMenu {
	private readonly List<TItem> _items = new();
	private string _id;

	protected string Id => _id ?? Text;

	protected string Text { get; private set; }

	protected string Icon { get; private set; }

	public virtual TMenu Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Text), "Menu text is required.");
		return CreateMenu(Array.AsReadOnly(_items.ToArray()));
	}

	protected void SetId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
	}

	protected void SetText(string text) {
		Guard.ArgumentNotNull(text, nameof(text));
		Text = text;
	}

	protected void SetIcon(string icon) => Icon = icon;

	protected void AddItemDefinition(TItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		_items.Add(item);
	}

	protected abstract TMenu CreateMenu(IReadOnlyList<TItem> items);
}
