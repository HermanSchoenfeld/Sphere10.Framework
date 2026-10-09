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

/// <summary>Accumulates neutral block configuration; UI builders supply their platform-specific product.</summary>
public abstract class ApplicationBlockBuilderBase<TMenu, TBlock> : IApplicationUIBuilder<TBlock> where TMenu : IApplicationMenu where TBlock : IApplicationBlock {
	private readonly List<TMenu> _menus = new();
	private string _id;

	protected string Id => _id ?? Name;

	protected string Name { get; private set; }

	protected int Position { get; private set; }

	protected string IconUrl { get; private set; }

	protected string Tooltip { get; private set; }

	protected Type DefaultScreen { get; private set; }

	protected string DefaultScreenTitle { get; private set; }

	protected ScreenActivationMode? DefaultScreenActivationMode { get; private set; }

	protected ScreenKind DefaultScreenKind { get; private set; }

	public virtual TBlock Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Name), "Block name is required.");
		return CreateBlock(Array.AsReadOnly(_menus.ToArray()));
	}

	protected void SetId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
		OnConfigurationChanged(nameof(Id));
	}

	protected void SetName(string name) {
		ValidateName(name);
		Name = name;
		OnConfigurationChanged(nameof(Name));
	}

	protected void SetPosition(int position) {
		Position = position;
		OnConfigurationChanged(nameof(Position));
	}

	protected void SetIconUrl(string iconUrl) {
		IconUrl = iconUrl;
		OnConfigurationChanged(nameof(IconUrl));
	}

	protected void SetTooltip(string tooltip) {
		Tooltip = tooltip;
		OnConfigurationChanged(nameof(Tooltip));
	}

	protected void SetDefaultScreen(Type screenType, string title = null, ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal) {
		ValidateScreenType(screenType);
		Tools.UI.ValidateScreenPolicy(activationMode, screenKind);
		DefaultScreen = screenType;
		DefaultScreenTitle = title;
		DefaultScreenActivationMode = activationMode;
		DefaultScreenKind = screenKind;
		OnConfigurationChanged(nameof(DefaultScreen));
	}

	protected void AddMenuDefinition(TMenu menu) {
		Guard.ArgumentNotNull(menu, nameof(menu));
		_menus.Add(menu);
		OnMenuAdded(menu);
	}

	protected virtual void OnConfigurationChanged(string propertyName) {
	}

	protected virtual void OnMenuAdded(TMenu menu) {
	}

	protected virtual void ValidateName(string name) => Guard.ArgumentNotNull(name, nameof(name));

	protected virtual void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType);

	protected abstract TBlock CreateBlock(IReadOnlyList<TMenu> menus);
}

/// <summary>Shared block metadata, screen policy and typed menu composition.</summary>
public abstract class ApplicationBlockBuilderBase<TSelf, TMenu, TMenuBuilder, TBlock> : ApplicationBlockBuilderBase<TMenu, TBlock>
	where TSelf : class where TMenu : IApplicationMenu where TBlock : IApplicationBlock
	where TMenuBuilder : IApplicationUIBuilder<TMenu>, new() {
	public TSelf WithId(string id) {
		SetId(id);
		return (TSelf)(object)this;
	}

	public TSelf WithName(string name) {
		SetName(name);
		return (TSelf)(object)this;
	}

	public TSelf WithPosition(int position) {
		SetPosition(position);
		return (TSelf)(object)this;
	}

	public TSelf WithDefaultScreen<TScreen>(string title = null, ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal)
		where TScreen : IApplicationScreen => WithDefaultScreen(typeof(TScreen), title, activationMode, screenKind);

	public TSelf WithDefaultScreen(Type screenType, string title = null, ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal) {
		SetDefaultScreen(screenType, title, activationMode, screenKind);
		return (TSelf)(object)this;
	}

	public TSelf AddMenu(TMenu menu) {
		AddMenuDefinition(menu);
		return (TSelf)(object)this;
	}

	public TSelf ConfigureMenu(Action<TMenuBuilder> configure) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new TMenuBuilder();
		configure(builder);
		return AddMenu(builder.Build());
	}
}
