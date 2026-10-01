// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class BlazorApplicationMenuBuilder : ApplicationMenuBuilderBase<IBlazorApplicationMenuItem, BlazorApplicationMenu> {
	public BlazorApplicationMenuBuilder WithId(string id) {
		SetId(id);
		return this;
	}

	public BlazorApplicationMenuBuilder WithText(string text) {
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		SetText(text);
		return this;
	}

	public BlazorApplicationMenuBuilder WithIcon(string icon) {
		SetIcon(icon);
		return this;
	}

	public BlazorApplicationMenuBuilder AddItem(IBlazorApplicationMenuItem item) {
		AddItemDefinition(item);
		return this;
	}

	public BlazorApplicationMenuBuilder AddScreenItem<TScreen>(string id, string text, ScreenActivationMode activationMode = ScreenActivationMode.SingleInstance,
		IReadOnlyDictionary<string, object> parameters = null
	) where TScreen : IComponent, IBlazorApplicationScreen => AddScreenItem(id, text, typeof(TScreen), activationMode, parameters);

	public BlazorApplicationMenuBuilder AddScreenItem<TScreen>(string text) where TScreen : IComponent, IBlazorApplicationScreen =>
		AddScreenItem<TScreen>(typeof(TScreen).Name, text);

	public BlazorApplicationMenuBuilder AddScreenItem(string id, string text, Type screenType, ScreenActivationMode activationMode = ScreenActivationMode.SingleInstance,
		IReadOnlyDictionary<string, object> parameters = null
	) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		return AddItem(new BlazorScreenMenuItem {
			Id = id, Title = text, ScreenType = screenType, ActivationMode = activationMode,
			Parameters = parameters ?? new Dictionary<string, object>()
		});
	}

	public BlazorApplicationMenuBuilder AddActionItem(string id, string text, Func<IServiceProvider, CancellationToken, Task> action) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		return AddItem(new BlazorActionMenuItem { Id = id, Title = text, AsyncAction = action });
	}

	public BlazorApplicationMenuBuilder AddActionItem(string id, string text, Action action) {
		Guard.ArgumentNotNull(action, nameof(action));
		return AddActionItem(id, text, (_, cancellationToken) => {
			cancellationToken.ThrowIfCancellationRequested();
			action();
			return Task.CompletedTask;
		});
	}

	public BlazorApplicationMenuBuilder ConfigureItem(Action<BlazorApplicationMenuItemBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorApplicationMenuItemBuilder();
		configure(builder);
		return AddItem(builder.Build());
	}

	protected override BlazorApplicationMenu CreateMenu(IReadOnlyList<IBlazorApplicationMenuItem> items) =>
		BlazorApplicationBlockSnapshot.CreateMenu(new BlazorApplicationMenu { Id = Id, Text = Text, Icon = Icon, Items = items.ToArray() });
}
