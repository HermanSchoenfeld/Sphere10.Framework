// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public class MenuBuilder {
	private readonly List<IApplicationMenuItem> _items = new();
	private string _id;
	private string _text;
	private string _icon;

	public MenuBuilder WithId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
		return this;
	}

	public MenuBuilder WithText(string text) {
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		_text = text;
		return this;
	}

	public MenuBuilder WithIcon(string icon) {
		_icon = icon;
		return this;
	}

	public MenuBuilder AddItem(IApplicationMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		_items.Add(item);
		return this;
	}

	public MenuBuilder AddScreenItem<TScreen>(string id, string text, ScreenActivationMode activationMode = ScreenActivationMode.SingleInstance,
		IReadOnlyDictionary<string, object> parameters = null
	) where TScreen : IComponent, IApplicationScreen => AddScreenItem(id, text, typeof(TScreen), activationMode, parameters);

	public MenuBuilder AddScreenItem<TScreen>(string text) where TScreen : IComponent, IApplicationScreen =>
		AddScreenItem<TScreen>(typeof(TScreen).Name, text);

	public MenuBuilder AddScreenItem(string id, string text, Type screenType, ScreenActivationMode activationMode = ScreenActivationMode.SingleInstance,
		IReadOnlyDictionary<string, object> parameters = null
	) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		return AddItem(new ShowScreenMenuItem {
			Id = id, Title = text, ScreenType = screenType, ActivationMode = activationMode,
			Parameters = parameters ?? new Dictionary<string, object>()
		});
	}

	public MenuBuilder AddActionItem(string id, string text, Func<IServiceProvider, CancellationToken, Task> action) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		return AddItem(new ActionMenuItem { Id = id, Title = text, AsyncAction = action });
	}

	public MenuBuilder AddActionItem(string id, string text, Action action) {
		Guard.ArgumentNotNull(action, nameof(action));
		return AddActionItem(id, text, (_, cancellationToken) => {
			cancellationToken.ThrowIfCancellationRequested();
			action();
			return Task.CompletedTask;
		});
	}

	public MenuBuilder ConfigureItem(Action<MenuItemBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new MenuItemBuilder();
		configure(builder);
		return AddItem(builder.Build());
	}

	public ApplicationMenu Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(_text), "Menu text is required.");
		return ApplicationBlockSnapshot.CreateMenu(new ApplicationMenu { Id = _id ?? _text, Text = _text, Icon = _icon, Items = _items });
	}
}

