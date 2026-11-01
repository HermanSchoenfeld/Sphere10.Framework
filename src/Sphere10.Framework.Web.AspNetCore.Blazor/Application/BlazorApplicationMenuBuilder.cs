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

public class BlazorApplicationMenuBuilder : ApplicationMenuBuilderBase<BlazorApplicationMenuBuilder, IBlazorApplicationMenuItem, BlazorApplicationMenuItemBuilder, BlazorApplicationMenu> {
	private string _id;

	public BlazorApplicationMenuBuilder WithId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
		return this;
	}

	public BlazorApplicationMenuBuilder WithIcon(string icon) {
		SetIcon(icon);
		return this;
	}

	/// <summary>Adds a command for the owning block's first registration of the screen type.</summary>
	public BlazorApplicationMenuBuilder AddShowScreenItem<TScreen>(string text = null, string id = null, string icon = null)
		where TScreen : IComponent, IBlazorApplicationScreen {
		return AddShowScreenItem(typeof(TScreen), text, id, icon);
	}

	/// <summary>Adds a runtime-type command for the first matching registration in its owning block.</summary>
	public BlazorApplicationMenuBuilder AddShowScreenItem(Type screenType, string text = null, string id = null, string icon = null) {
		return AddShowScreenItem(ApplicationScreenRegistration.ForType(screenType), text, id, icon);
	}

	/// <summary>Adds presentation for an exact registration without creating or configuring a screen.</summary>
	public BlazorApplicationMenuBuilder AddShowScreenItem(ApplicationScreenRegistration registration, string text = null, string id = null, string icon = null) {
		Guard.ArgumentNotNull(registration, nameof(registration));
		Tools.UI.ValidateScreenType(registration.ScreenType, typeof(IBlazorApplicationScreen));
		return AddItem(new BlazorScreenMenuItem { Id = id, Title = text, Icon = icon, Registration = registration });
	}

	/// <summary>References an existing block-owned definition; the block resolves its graph-local copy.</summary>
	public BlazorApplicationMenuBuilder AddShowScreenItem(ApplicationScreenDefinition definition, string text = null, string id = null, string icon = null) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		return AddShowScreenItem(definition.Registration, text, id, icon);
	}

	public BlazorApplicationMenuBuilder AddActionItem(string id, string text, Func<IServiceProvider, CancellationToken, Task> action) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(!string.IsNullOrWhiteSpace(text), "Menu item text is required.");
		return AddItem(new BlazorActionMenuItem { Id = id, Title = text, AsyncAction = action });
	}

	public BlazorApplicationMenuBuilder AddActionItem(string id, string text, Action action) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(!string.IsNullOrWhiteSpace(text), "Menu item text is required.");
		return AddItem(new BlazorActionMenuItem { Id = id, Title = text, Action = action });
	}

	protected override void ValidateText(string text) => Guard.ArgumentNotNullOrEmpty(text, nameof(text));

	protected override BlazorApplicationMenu CreateMenu(IReadOnlyList<IBlazorApplicationMenuItem> items) =>
		BlazorApplicationBlockSnapshot.CreateBuilderMenu(new BlazorApplicationMenu(ownsItems: false) { Id = _id ?? Text, Text = Text, Icon = Icon, Items = items.ToArray() });
}
