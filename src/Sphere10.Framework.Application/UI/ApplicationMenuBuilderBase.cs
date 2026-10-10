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

public abstract class ApplicationMenuBuilderBase<TItem, TMenu> : IApplicationUIBuilder<TMenu>
	where TItem : IApplicationMenuItem
	where TMenu : IApplicationMenu {
	private readonly List<TItem> _items = new();

	protected string Text { get; private set; }

	protected string Icon { get; private set; }

	public virtual TMenu Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Text), "Menu text is required.");
		return CreateMenu(Array.AsReadOnly(_items.ToArray()));
	}

	protected void SetText(string text) {
		ValidateText(text);
		Text = text;
		OnConfigurationChanged(nameof(Text));
	}

	protected void SetIcon(string icon) {
		Icon = icon;
		OnConfigurationChanged(nameof(Icon));
	}

	protected void AddItemInternal(TItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		_items.Add(item);
		OnItemAdded(item);
	}

	protected virtual void OnConfigurationChanged(string propertyName) {
	}

	protected virtual void OnItemAdded(TItem item) {
	}

	protected virtual void ValidateText(string text) => Guard.ArgumentNotNull(text, nameof(text));

	protected abstract TMenu CreateMenu(IReadOnlyList<TItem> items);
}

/// <summary>Shared menu metadata and item-builder composition.</summary>
public abstract class ApplicationMenuBuilderBase<TSelf, TItem, TItemBuilder, TMenu> : ApplicationMenuBuilderBase<TItem, TMenu>
	where TSelf : class
	where TItem : IApplicationMenuItem
	where TMenu : IApplicationMenu
	where TItemBuilder : IApplicationUIBuilder<TItem>, new() {
	public TSelf WithText(string text) {
		SetText(text);
		return (TSelf)(object)this;
	}

	public TSelf AddItem(TItem item) {
		AddItemInternal(item);
		return (TSelf)(object)this;
	}

	public TSelf ConfigureItem(Action<TItemBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new TItemBuilder();
		configure(builder);
		return AddItem(builder.Build());
	}
}
