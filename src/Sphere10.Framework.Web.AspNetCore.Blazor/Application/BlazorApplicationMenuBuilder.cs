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

	/// <summary>Adds an action using optional inline web presentation.</summary>
	public BlazorApplicationMenuBuilder AddItem(string text, Action action, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddItem(text, action, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds an action and captures its configuration definition before graph snapshots.</summary>
	public BlazorApplicationMenuBuilder AddItem(string text, Action action, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Argument(id == null || id.Length > 0, nameof(id), "A command ID cannot be empty.");
		Guard.Ensure(!string.IsNullOrWhiteSpace(text), "Menu item text is required.");
		return AddItem(new BlazorActionMenuItem {
			Id = id ?? text, Title = text, Icon = icon, IsVisible = isVisible, CanExecute = canExecute, Action = action
		}, out registration);
	}

	/// <summary>Adds an action using optional inline web presentation.</summary>
	public BlazorApplicationMenuBuilder AddItem(string text, Func<IServiceProvider, CancellationToken, Task> action, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddItem(text, action, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds an action and captures its configuration definition before graph snapshots.</summary>
	public BlazorApplicationMenuBuilder AddItem(string text, Func<IServiceProvider, CancellationToken, Task> action, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Argument(id == null || id.Length > 0, nameof(id), "A command ID cannot be empty.");
		Guard.Ensure(!string.IsNullOrWhiteSpace(text), "Menu item text is required.");
		return AddItem(new BlazorActionMenuItem {
			Id = id ?? text, Title = text, Icon = icon, IsVisible = isVisible, CanExecute = canExecute, AsyncAction = action
		}, out registration);
	}

	/// <summary>Adds navigation for the owning block's first matching screen registration.</summary>
	public BlazorApplicationMenuBuilder AddItem<TScreen>(string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null)
		where TScreen : IBlazorApplicationScreen {
		return AddItem(ApplicationScreenRegistration.ForType(typeof(TScreen)), text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds typed navigation and captures its configured command definition.</summary>
	public BlazorApplicationMenuBuilder AddItem<TScreen>(string text, out IBlazorApplicationMenuItem registration, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null)
		where TScreen : IBlazorApplicationScreen {
		return AddItem(ApplicationScreenRegistration.ForType(typeof(TScreen)), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds navigation without creating or changing a screen registration.</summary>
	public BlazorApplicationMenuBuilder AddItem(Type screenType, string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddItem(screenType, text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds navigation and captures its configured command definition.</summary>
	public BlazorApplicationMenuBuilder AddItem(Type screenType, string text, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddItem(ApplicationScreenRegistration.ForType(screenType), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds navigation without creating or changing a screen registration.</summary>
	public BlazorApplicationMenuBuilder AddItem(ApplicationScreenRegistration screen, string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddItem(screen, text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds navigation and captures its configured command definition.</summary>
	public BlazorApplicationMenuBuilder AddItem(ApplicationScreenRegistration screen, string text, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		Guard.ArgumentNotNull(screen, nameof(screen));
		Tools.UI.ValidateScreenType(screen.ScreenType, typeof(IBlazorApplicationScreen));
		Guard.Argument(id == null || id.Length > 0, nameof(id), "A command ID cannot be empty.");
		return AddItem(new BlazorScreenMenuItem {
			Id = id, Title = text, Icon = icon, IsVisible = isVisible, CanExecute = canExecute, Registration = screen
		}, out registration);
	}

	/// <summary>Adds navigation without creating or changing a screen registration.</summary>
	public BlazorApplicationMenuBuilder AddItem(ApplicationScreenDefinition definition, string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddItem(definition, text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds navigation and captures its configured command definition.</summary>
	public BlazorApplicationMenuBuilder AddItem(ApplicationScreenDefinition definition, string text, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		return AddItem(definition.Registration, text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds an action group using optional inline web presentation.</summary>
	public BlazorApplicationMenuBuilder AddItem(string text, IBlazorApplicationMenuItem[] children, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddItem(text, children, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds an action group and captures its independently configured definition.</summary>
	public BlazorApplicationMenuBuilder AddItem(string text, IBlazorApplicationMenuItem[] children, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddItem(item => {
			item
				.WithText(text)
				.WithChildren(children)
				.WithIcon(icon)
				.WithVisibility(isVisible);

			if (id != null)
				item.WithId(id);
			if (canExecute != null)
				item.EnabledWhen(canExecute);
		}, out registration);
	}


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
		return AddItem(registration, text, id, icon);
	}

	/// <summary>References an existing block-owned definition; the block resolves its graph-local copy.</summary>
	public BlazorApplicationMenuBuilder AddShowScreenItem(ApplicationScreenDefinition definition, string text = null, string id = null, string icon = null) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		return AddShowScreenItem(definition.Registration, text, id, icon);
	}

	public BlazorApplicationMenuBuilder AddActionItem(string id, string text, Func<IServiceProvider, CancellationToken, Task> action) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		return AddItem(text, action, id);
	}

	public BlazorApplicationMenuBuilder AddActionItem(string id, string text, Action action) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		return AddItem(text, action, id);
	}

	protected override void ValidateText(string text) => Guard.ArgumentNotNullOrEmpty(text, nameof(text));

	protected override BlazorApplicationMenu CreateMenu(IReadOnlyList<IBlazorApplicationMenuItem> items) =>
		BlazorApplicationBlockSnapshot.CreateBuilderMenu(new BlazorApplicationMenu(ownsItems: false) { Id = _id ?? Text, Text = Text, Icon = Icon, Items = items.ToArray() });
}
