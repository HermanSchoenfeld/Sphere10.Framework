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
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class BlazorApplicationBlockBuilder : ApplicationBlockBuilderBase<IBlazorApplicationMenu, BlazorApplicationBlock> {
	private readonly List<IBlazorApplicationMenuItem> _toolBarItems = new();

	public BlazorApplicationBlockBuilder WithId(string id) {
		SetId(id);
		return this;
	}

	public BlazorApplicationBlockBuilder WithName(string name) {
		Guard.ArgumentNotNullOrEmpty(name, nameof(name));
		SetName(name);
		return this;
	}

	public BlazorApplicationBlockBuilder WithTitle(string title) => WithName(title);

	public BlazorApplicationBlockBuilder WithPosition(int position) {
		SetPosition(position);
		return this;
	}

	public BlazorApplicationBlockBuilder WithIconUrl(string iconUrl) {
		SetIconUrl(iconUrl);
		return this;
	}

	public BlazorApplicationBlockBuilder WithTooltip(string tooltip) {
		SetTooltip(tooltip);
		return this;
	}

	public BlazorApplicationBlockBuilder WithDefaultScreen<TScreen>(string title = null, ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal) where TScreen : IComponent, IBlazorApplicationScreen =>
		WithDefaultScreen(typeof(TScreen), title, activationMode, screenKind);

	public BlazorApplicationBlockBuilder WithDefaultScreen(Type screenType, string title = null, ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal) {
		SetDefaultScreen(screenType, title, activationMode, screenKind);
		return this;
	}

	public BlazorApplicationBlockBuilder AddMenu(Action<BlazorApplicationMenuBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorApplicationMenuBuilder();
		configure(builder);
		return AddMenu(builder.Build());
	}

	public BlazorApplicationBlockBuilder AddMenu(IBlazorApplicationMenu menu) {
		AddMenuDefinition(menu);
		return this;
	}

	public BlazorApplicationBlockBuilder AddToolBarItem(IBlazorApplicationMenuItem item) {
		Guard.ArgumentNotNull(item, nameof(item));
		_toolBarItems.Add(item);
		return this;
	}

	public BlazorApplicationBlockBuilder AddToolBarItem(Action<BlazorApplicationMenuItemBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorApplicationMenuItemBuilder();
		configure(builder);
		return AddToolBarItem(builder.Build());
	}

	public BlazorApplicationBlockBuilder AddToolBarSeparator() => AddToolBarItem(new BlazorMenuSeparator());

	protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));

	protected override BlazorApplicationBlock CreateBlock(IReadOnlyList<IBlazorApplicationMenu> menus) =>
		BlazorApplicationBlockSnapshot.Create(new BlazorApplicationBlock {
			Id = Id, Title = Name, Position = Position, IconUrl = IconUrl, Tooltip = Tooltip,
			DefaultScreen = DefaultScreen, DefaultScreenTitle = DefaultScreenTitle,
			DefaultScreenActivationMode = DefaultScreenActivationMode, DefaultScreenKind = DefaultScreenKind, Menus = menus.ToArray(), ToolBarItems = _toolBarItems.ToArray()
		});
}
