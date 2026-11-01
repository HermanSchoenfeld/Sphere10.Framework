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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class BlazorApplicationBlockBuilder : ApplicationBlockBuilderBase<BlazorApplicationBlockBuilder, IBlazorApplicationMenu, BlazorApplicationMenuBuilder, BlazorApplicationBlock> {
	private readonly List<IBlazorApplicationMenuItem> _toolBarItems = new();
	private string _id;

	/// <summary>Configures and registers a component without requiring a menu command.</summary>
	public BlazorApplicationBlockBuilder AddScreen<TScreen>(Action<BlazorApplicationScreenBuilder<TScreen>> configure)
		where TScreen : IBlazorApplicationScreen {
		return AddScreen(configure, out _);
	}

	/// <summary>Captures a graph-independent reference to the configured component registration.</summary>
	public BlazorApplicationBlockBuilder AddScreen<TScreen>(Action<BlazorApplicationScreenBuilder<TScreen>> configure, out ApplicationScreenRegistration<TScreen> registration)
		where TScreen : IBlazorApplicationScreen {
		Guard.ArgumentNotNull(configure, nameof(configure));
		registration = new ApplicationScreenRegistration<TScreen>();
		var builder = new BlazorApplicationScreenBuilder<TScreen>(registration);
		configure(builder);
		AddScreenDefinitionInternal(builder.Build());
		return this;
	}

	/// <summary>Registers a component whose type is supplied by its callback.</summary>
	public BlazorApplicationBlockBuilder AddScreen(Action<BlazorApplicationScreenBuilder> configure) {
		return AddScreen(configure, out _);
	}

	/// <summary>Captures the identity after a callback has supplied the component type and configuration.</summary>
	public BlazorApplicationBlockBuilder AddScreen(Action<BlazorApplicationScreenBuilder> configure, out ApplicationScreenRegistration registration) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorApplicationScreenBuilder();
		configure(builder);
		var definition = builder.Build();
		registration = definition.Registration;
		AddScreenDefinitionInternal(definition);
		return this;
	}

	public BlazorApplicationBlockBuilder WithId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
		return this;
	}

	public BlazorApplicationBlockBuilder WithTitle(string title) => WithName(title);

	public BlazorApplicationBlockBuilder WithIconUrl(string iconUrl) {
		SetIconUrl(iconUrl);
		return this;
	}

	public BlazorApplicationBlockBuilder WithTooltip(string tooltip) {
		SetTooltip(tooltip);
		return this;
	}

	public BlazorApplicationBlockBuilder AddToolBarItem(IBlazorApplicationMenuItem item) {
		return AddToolBarItem(item, out _);
	}

	/// <summary>Adds a toolbar command and captures its configured definition before graph snapshots are built.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(IBlazorApplicationMenuItem item, out IBlazorApplicationMenuItem registration) {
		Guard.ArgumentNotNull(item, nameof(item));
		_toolBarItems.Add(item);
		registration = item;
		return this;
	}

	public BlazorApplicationBlockBuilder AddToolBarItem(Action<BlazorApplicationMenuItemBuilder> configure) {
		return AddToolBarItem(configure, out _);
	}

	/// <summary>Configures one toolbar command, captures its definition and returns the block builder.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(Action<BlazorApplicationMenuItemBuilder> configure, out IBlazorApplicationMenuItem registration) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new BlazorApplicationMenuItemBuilder();
		configure(builder);
		return AddToolBarItem(builder.Build(), out registration);
	}

	public BlazorApplicationBlockBuilder AddToolBarSeparator() => AddToolBarItem(new BlazorMenuSeparator());

	protected override void ValidateName(string name) => Guard.ArgumentNotNullOrEmpty(name, nameof(name));

	protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));

	protected override BlazorApplicationBlock CreateBlock(IReadOnlyList<IBlazorApplicationMenu> menus) =>
		BlazorApplicationBlockSnapshot.Create(new BlockSource {
			Id = _id ?? Name, Title = Name, Position = Position, IconUrl = IconUrl, Tooltip = Tooltip,
			ScreenDefinitions = ScreenDefinitions, DefaultScreenDefinition = DefaultScreenDefinition,
			DefaultScreen = DefaultScreen, DefaultScreenTitle = DefaultScreenTitle,
			DefaultScreenActivationMode = DefaultScreenActivationMode, DefaultScreenKind = DefaultScreenKind, Menus = menus.ToArray(), ToolBarItems = _toolBarItems.ToArray()
		});

	/// <summary>Builder inputs are borrowed until the final snapshot owns their cloned graph.</summary>
	private sealed class BlockSource : IBlazorApplicationBlock {
		public IBlazorApplication Parent { get; set; }

		public IBlazorPlugin Source { get; set; }

		public string Id { get; init; }

		public string Title { get; init; }

		public int Position { get; init; }

		public string IconUrl { get; init; }

		public string Tooltip { get; init; }

		public IBlazorApplicationMenu[] Menus { get; init; }

		public IBlazorApplicationMenuItem[] ToolBarItems { get; init; }

		public ApplicationScreenDefinition[] ScreenDefinitions { get; init; }

		public ApplicationScreenDefinition DefaultScreenDefinition { get; init; }

		public Type DefaultScreen { get; init; }

		public string DefaultScreenTitle { get; init; }

		public ScreenActivationMode? DefaultScreenActivationMode { get; init; }

		public ScreenKind DefaultScreenKind { get; init; }
	}
}
