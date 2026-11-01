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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class BlazorApplicationBlockBuilder : ApplicationBlockBuilderBase<BlazorApplicationBlockBuilder, IBlazorApplicationMenu, BlazorApplicationMenuBuilder, BlazorApplicationBlock> {
	private readonly List<IBlazorApplicationMenuItem> _toolBarItems = new();
	private string _id;

	/// <summary>Adds a component using optional inline registration metadata.</summary>
	public BlazorApplicationBlockBuilder AddScreen<TScreen>(string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, string stableKey = null, object settings = null, Func<object> settingsFactory = null,
		IReadOnlyDictionary<string, object> parameters = null,
		Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> activator = null, Type settingsEditor = null)
		where TScreen : IBlazorApplicationScreen {
		return AddScreen<TScreen>(title, out _, activationMode, screenKind, stableKey, settings, settingsFactory, parameters, activator, settingsEditor);
	}

	/// <summary>Adds a component and captures its exact registration for later navigation.</summary>
	public BlazorApplicationBlockBuilder AddScreen<TScreen>(string title, out ApplicationScreenRegistration<TScreen> registration,
		ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal, string stableKey = null,
		object settings = null, Func<object> settingsFactory = null, IReadOnlyDictionary<string, object> parameters = null,
		Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> activator = null, Type settingsEditor = null)
		where TScreen : IBlazorApplicationScreen {
		registration = new ApplicationScreenRegistration<TScreen>();
		AddScreenDefinitionInternal(CreateScreenDefinition(typeof(TScreen), registration, title, activationMode, screenKind, stableKey, settings, settingsFactory, parameters, activator, settingsEditor));
		return this;
	}

	/// <summary>Adds a runtime component type using optional inline registration metadata.</summary>
	public BlazorApplicationBlockBuilder AddScreen(Type screenType, string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, string stableKey = null, object settings = null, Func<object> settingsFactory = null,
		IReadOnlyDictionary<string, object> parameters = null,
		Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> activator = null, Type settingsEditor = null) {
		return AddScreen(screenType, title, out _, activationMode, screenKind, stableKey, settings, settingsFactory, parameters, activator, settingsEditor);
	}

	/// <summary>Adds a runtime component type and captures its exact registration.</summary>
	public BlazorApplicationBlockBuilder AddScreen(Type screenType, string title, out ApplicationScreenRegistration registration,
		ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal, string stableKey = null,
		object settings = null, Func<object> settingsFactory = null, IReadOnlyDictionary<string, object> parameters = null,
		Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> activator = null, Type settingsEditor = null) {
		registration = new ApplicationScreenRegistration(screenType);
		AddScreenDefinitionInternal(CreateScreenDefinition(screenType, registration, title, activationMode, screenKind, stableKey, settings, settingsFactory, parameters, activator, settingsEditor));
		return this;
	}

	/// <summary>Adds a menu with optional web presentation and child configuration.</summary>
	public BlazorApplicationBlockBuilder AddMenu(string text, string icon = null, string id = null,
		Action<BlazorApplicationMenuBuilder> configure = null) {
		return AddMenu(text, out _, icon, id, configure);
	}

	/// <summary>Adds a configured menu and captures its definition before graph snapshots are built.</summary>
	public BlazorApplicationBlockBuilder AddMenu(string text, out IBlazorApplicationMenu registration, string icon = null,
		string id = null, Action<BlazorApplicationMenuBuilder> configure = null) {
		return AddMenu(menu => {
			menu
				.WithText(text)
				.WithIcon(icon);

			if (id != null)
				menu.WithId(id);

			configure?.Invoke(menu);
		}, out registration);
	}

	/// <summary>Adds a top-level menu through the block's existing menu ownership path.</summary>
	public BlazorApplicationBlockBuilder AddMenuItem(string text, string icon = null, string id = null,
		Action<BlazorApplicationMenuBuilder> configure = null) {
		return AddMenu(text, icon, id, configure);
	}

	/// <summary>Adds a top-level menu and captures its configuration definition.</summary>
	public BlazorApplicationBlockBuilder AddMenuItem(string text, out IBlazorApplicationMenu registration, string icon = null,
		string id = null, Action<BlazorApplicationMenuBuilder> configure = null) {
		return AddMenu(text, out registration, icon, id, configure);
	}

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

	/// <summary>Adds a toolbar action using optional inline web presentation.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(string text, Action action, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(text, action, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action and captures its configuration definition.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(string text, Action action, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return ConfigureToolBarItem(item => item.WithAction(action), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action using optional inline web presentation.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(string text, Func<IServiceProvider, CancellationToken, Task> action, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(text, action, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action and captures its configuration definition.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(string text, Func<IServiceProvider, CancellationToken, Task> action, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return ConfigureToolBarItem(item => item.WithAction(action), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation for the owning block's first matching registration.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem<TScreen>(string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null)
		where TScreen : IBlazorApplicationScreen {
		return AddToolBarItem(typeof(TScreen), text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds typed toolbar navigation and captures its configuration definition.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem<TScreen>(string text, out IBlazorApplicationMenuItem registration, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null)
		where TScreen : IBlazorApplicationScreen {
		return AddToolBarItem(typeof(TScreen), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation without creating or changing a screen registration.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(Type screenType, string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(screenType, text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation and captures its configuration definition.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(Type screenType, string text, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(ApplicationScreenRegistration.ForType(screenType), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation without creating or changing a screen registration.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(ApplicationScreenRegistration screen, string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(screen, text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation and captures its configuration definition.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(ApplicationScreenRegistration screen, string text, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return ConfigureToolBarItem(item => item.WithScreen(screen), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation without creating or changing a screen registration.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(ApplicationScreenDefinition definition, string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(definition, text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation and captures its configuration definition.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(ApplicationScreenDefinition definition, string text, out IBlazorApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		return AddToolBarItem(definition.Registration, text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action group using optional inline web presentation.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(string text, IBlazorApplicationMenuItem[] children, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(text, children, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action group and captures its configuration definition.</summary>
	public BlazorApplicationBlockBuilder AddToolBarItem(string text, IBlazorApplicationMenuItem[] children,
		out IBlazorApplicationMenuItem registration, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return ConfigureToolBarItem(item => item.WithChildren(children), text, out registration, id, icon, isVisible, canExecute);
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

	// Reuse the child builder's behavior validation and the block's existing toolbar ownership path.
	private BlazorApplicationBlockBuilder ConfigureToolBarItem(Action<BlazorApplicationMenuItemBuilder> configureBehavior, string text,
		out IBlazorApplicationMenuItem registration, string id, string icon, bool isVisible, Func<bool> canExecute) {
		return AddToolBarItem(item => {
			configureBehavior(item);
			item
				.WithIcon(icon)
				.WithVisibility(isVisible);

			if (text != null)
				item.WithText(text);
			if (id != null)
				item.WithId(id);
			if (canExecute != null)
				item.EnabledWhen(canExecute);
		}, out registration);
	}

	// Capture settings defaults during composition; the renderer still owns operational components.
	private static BlazorApplicationScreenDefinition CreateScreenDefinition(Type screenType, ApplicationScreenRegistration registration,
		string title, ScreenActivationMode? activationMode, ScreenKind screenKind, string stableKey, object settings,
		Func<object> settingsFactory, IReadOnlyDictionary<string, object> parameters,
		Func<BlazorApplicationScreenActivationContext, BlazorApplicationScreenActivation> activator, Type settingsEditor) {
		Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));
		Tools.UI.ValidateScreenPolicy(activationMode, screenKind);
		Guard.Argument(title == null || title.Length > 0, nameof(title), "A screen title cannot be empty.");

		if (settingsFactory != null) {
			settings ??= settingsFactory();
			Guard.ArgumentNotNull(settings, nameof(settingsFactory));
		}

		return new BlazorApplicationScreenDefinition(screenType, title, activationMode, screenKind, registration, stableKey, parameters, activator, settings, settingsFactory, settingsEditor);
	}

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
