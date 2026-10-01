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

public class MenuItemBuilder {
	private string _id;
	private string _title;
	private string _icon;
	private Type _screenType;
	private ScreenActivationMode _activationMode;
	private IReadOnlyDictionary<string, object> _parameters = new Dictionary<string, object>();
	private Func<IServiceProvider, CancellationToken, Task> _action;

	public MenuItemBuilder WithId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
		return this;
	}

	public MenuItemBuilder WithText(string text) {
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		_title = text;
		return this;
	}

	public MenuItemBuilder WithTitle(string title) => WithText(title);

	public MenuItemBuilder WithIcon(string icon) {
		_icon = icon;
		return this;
	}

	public MenuItemBuilder WithScreen<TScreen>() where TScreen : IComponent, IApplicationScreen => WithScreen(typeof(TScreen));

	public MenuItemBuilder WithScreen(Type screenType) {
		ApplicationBlockSnapshot.ValidateScreenType(screenType);
		Guard.Ensure(_action == null, "A menu item cannot contain both a screen and an action.");
		_screenType = screenType;
		return this;
	}

	public MenuItemBuilder AsSingleInstance() {
		_activationMode = ScreenActivationMode.SingleInstance;
		return this;
	}

	public MenuItemBuilder AsMultiInstance() {
		_activationMode = ScreenActivationMode.MultiInstance;
		return this;
	}

	public MenuItemBuilder WithParameters(IReadOnlyDictionary<string, object> parameters) {
		Guard.ArgumentNotNull(parameters, nameof(parameters));
		_parameters = new Dictionary<string, object>(parameters);
		return this;
	}

	public MenuItemBuilder WithAction(Func<IServiceProvider, CancellationToken, Task> action) {
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(_screenType == null, "A menu item cannot contain both a screen and an action.");
		_action = action;
		return this;
	}

	public ApplicationMenuItem Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(_title), "Menu item text is required.");
		Guard.Ensure(_screenType != null || _action != null, "A screen or action is required.");
		if (_screenType != null)
			return new ShowScreenMenuItem {
				Id = _id ?? _screenType.Name, Title = _title, Icon = _icon, ScreenType = _screenType,
				ActivationMode = _activationMode, Parameters = _parameters
			};
		return new ActionMenuItem { Id = _id ?? _title, Title = _title, Icon = _icon, AsyncAction = _action };
	}
}

