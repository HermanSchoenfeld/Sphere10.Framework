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

	public BlazorApplicationBlockBuilder WithDefaultScreen<TScreen>(string title = null) where TScreen : IComponent, IBlazorApplicationScreen =>
		WithDefaultScreen(typeof(TScreen), title);

	public BlazorApplicationBlockBuilder WithDefaultScreen(Type screenType, string title = null) {
		SetDefaultScreen(screenType, title);
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

	protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));

	protected override BlazorApplicationBlock CreateBlock(IReadOnlyList<IBlazorApplicationMenu> menus) =>
		BlazorApplicationBlockSnapshot.Create(new BlazorApplicationBlock {
			Id = Id, Title = Name, Position = Position, IconUrl = IconUrl, Tooltip = Tooltip,
			DefaultScreen = DefaultScreen, DefaultScreenTitle = DefaultScreenTitle, Menus = menus.ToArray()
		});
}
