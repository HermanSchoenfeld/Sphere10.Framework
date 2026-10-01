// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

public class ApplicationBlockBuilder {
	private readonly List<IApplicationMenu> _menus = new();
	private string _id;
	private string _name;
	private int _position;
	private string _iconUrl;
	private string _tooltip;
	private Type _defaultScreen;
	private string _defaultScreenTitle;

	public ApplicationBlockBuilder WithId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
		return this;
	}

	public ApplicationBlockBuilder WithName(string name) {
		Guard.ArgumentNotNullOrEmpty(name, nameof(name));
		_name = name;
		return this;
	}

	public ApplicationBlockBuilder WithTitle(string title) => WithName(title);

	public ApplicationBlockBuilder WithPosition(int position) {
		_position = position;
		return this;
	}

	public ApplicationBlockBuilder WithIconUrl(string iconUrl) {
		_iconUrl = iconUrl;
		return this;
	}

	public ApplicationBlockBuilder WithTooltip(string tooltip) {
		_tooltip = tooltip;
		return this;
	}

	public ApplicationBlockBuilder WithDefaultScreen<TScreen>(string title = null) where TScreen : IComponent, IApplicationScreen =>
		WithDefaultScreen(typeof(TScreen), title);

	public ApplicationBlockBuilder WithDefaultScreen(Type screenType, string title = null) {
		ApplicationBlockSnapshot.ValidateScreenType(screenType);
		_defaultScreen = screenType;
		_defaultScreenTitle = title;
		return this;
	}

	public ApplicationBlockBuilder AddMenu(Action<MenuBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new MenuBuilder();
		configure(builder);
		return AddMenu(builder.Build());
	}

	public ApplicationBlockBuilder AddMenu(IApplicationMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		_menus.Add(menu);
		return this;
	}

	public ApplicationBlock Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(_name), "Block name is required.");
		return ApplicationBlockSnapshot.Create(new ApplicationBlock {
			Id = _id ?? _name, Title = _name, Position = _position, IconUrl = _iconUrl, Tooltip = _tooltip,
			DefaultScreen = _defaultScreen, DefaultScreenTitle = _defaultScreenTitle, Menus = _menus
		});
	}
}

