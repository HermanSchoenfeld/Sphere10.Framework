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
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class ApplicationBlockBuilder : ApplicationBlockBuilderBase<ApplicationBlockBuilder, IApplicationMenu, ApplicationMenuBuilder, ApplicationBlock> {
	private readonly List<IApplicationMenuItem> _toolBarItems = new();
	private string _id;

	/// <summary>Adds a component using optional inline registration metadata.</summary>
	public ApplicationBlockBuilder AddScreen<TScreen>(string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, string stableKey = null, SettingsObject settings = null,
		IReadOnlyDictionary<string, object> parameters = null,
		Func<ApplicationScreenActivationContext, ApplicationScreenActivation> activator = null, Type settingsEditor = null)
		where TScreen : IApplicationScreen {
		return AddScreen<TScreen>(title, out _, activationMode, screenKind, stableKey, settings, parameters, activator, settingsEditor);
	}

	/// <summary>Adds a component and captures its exact registration for later navigation.</summary>
	public ApplicationBlockBuilder AddScreen<TScreen>(string title, out ApplicationScreenRegistration<TScreen> registration,
		ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal, string stableKey = null,
		SettingsObject settings = null, IReadOnlyDictionary<string, object> parameters = null,
		Func<ApplicationScreenActivationContext, ApplicationScreenActivation> activator = null, Type settingsEditor = null)
		where TScreen : IApplicationScreen {
		registration = new ApplicationScreenRegistration<TScreen>();
		AddScreenDefinitionInternal(CreateScreenDefinition(typeof(TScreen), registration, title, activationMode, screenKind, stableKey, settings, parameters, activator, settingsEditor));
		return this;
	}

	/// <summary>Adds a runtime component type using optional inline registration metadata.</summary>
	public ApplicationBlockBuilder AddScreen(Type screenType, string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, string stableKey = null, SettingsObject settings = null,
		IReadOnlyDictionary<string, object> parameters = null,
		Func<ApplicationScreenActivationContext, ApplicationScreenActivation> activator = null, Type settingsEditor = null) {
		return AddScreen(screenType, title, out _, activationMode, screenKind, stableKey, settings, parameters, activator, settingsEditor);
	}

	/// <summary>Adds a runtime component type and captures its exact registration.</summary>
	public ApplicationBlockBuilder AddScreen(Type screenType, string title, out ApplicationScreenRegistration registration,
		ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal, string stableKey = null,
		SettingsObject settings = null, IReadOnlyDictionary<string, object> parameters = null,
		Func<ApplicationScreenActivationContext, ApplicationScreenActivation> activator = null, Type settingsEditor = null) {
		registration = new ApplicationScreenRegistration(screenType);
		AddScreenDefinitionInternal(CreateScreenDefinition(screenType, registration, title, activationMode, screenKind, stableKey, settings, parameters, activator, settingsEditor));
		return this;
	}

	/// <summary>Adds a menu with optional web presentation and child configuration.</summary>
	public ApplicationBlockBuilder AddMenu(string text, string icon = null, string id = null,
		Action<ApplicationMenuBuilder> configure = null) {
		return AddMenu(text, out _, icon, id, configure);
	}

	/// <summary>Adds a configured menu and captures its definition before graph snapshots are built.</summary>
	public ApplicationBlockBuilder AddMenu(string text, out IApplicationMenu registration, string icon = null,
		string id = null, Action<ApplicationMenuBuilder> configure = null) {
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
	public ApplicationBlockBuilder AddMenuItem(string text, string icon = null, string id = null,
		Action<ApplicationMenuBuilder> configure = null) {
		return AddMenu(text, icon, id, configure);
	}

	/// <summary>Adds a top-level menu and captures its configuration definition.</summary>
	public ApplicationBlockBuilder AddMenuItem(string text, out IApplicationMenu registration, string icon = null,
		string id = null, Action<ApplicationMenuBuilder> configure = null) {
		return AddMenu(text, out registration, icon, id, configure);
	}

	/// <summary>Configures and registers a component without requiring a menu command.</summary>
	public ApplicationBlockBuilder AddScreen<TScreen>(Action<ApplicationScreenBuilder<TScreen>> configure)
		where TScreen : IApplicationScreen {
		return AddScreen(configure, out _);
	}

	/// <summary>Captures a graph-independent reference to the configured component registration.</summary>
	public ApplicationBlockBuilder AddScreen<TScreen>(Action<ApplicationScreenBuilder<TScreen>> configure, out ApplicationScreenRegistration<TScreen> registration)
		where TScreen : IApplicationScreen {
		Guard.ArgumentNotNull(configure, nameof(configure));
		registration = new ApplicationScreenRegistration<TScreen>();
		var builder = new ApplicationScreenBuilder<TScreen>(registration);
		configure(builder);
		AddScreenDefinitionInternal(builder.Build());
		return this;
	}

	/// <summary>Registers a component whose type is supplied by its callback.</summary>
	public ApplicationBlockBuilder AddScreen(Action<ApplicationScreenBuilder> configure) {
		return AddScreen(configure, out _);
	}

	/// <summary>Captures the identity after a callback has supplied the component type and configuration.</summary>
	public ApplicationBlockBuilder AddScreen(Action<ApplicationScreenBuilder> configure, out ApplicationScreenRegistration registration) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new ApplicationScreenBuilder();
		configure(builder);
		var definition = builder.Build();
		registration = definition.Registration;
		AddScreenDefinitionInternal(definition);
		return this;
	}

	/// <summary>Sets the stable browser identity of the configured block.</summary>
	public ApplicationBlockBuilder WithId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
		return this;
	}

	/// <summary>Sets the display name of the configured block.</summary>
	public ApplicationBlockBuilder WithTitle(string title) {
		return WithName(title);
	}

	/// <summary>Sets the block navigation icon.</summary>
	public ApplicationBlockBuilder WithIconUrl(string iconUrl) {
		SetIconUrl(iconUrl);
		return this;
	}

	/// <summary>Sets the block navigation tooltip.</summary>
	public ApplicationBlockBuilder WithTooltip(string tooltip) {
		SetTooltip(tooltip);
		return this;
	}

	/// <summary>Adds a toolbar action using optional inline web presentation.</summary>
	public ApplicationBlockBuilder AddToolBarItem(string text, Action action, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(text, action, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action and captures its configuration definition.</summary>
	public ApplicationBlockBuilder AddToolBarItem(string text, Action action, out IApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return ConfigureToolBarItem(item => item.WithAction(action), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action using optional inline web presentation.</summary>
	public ApplicationBlockBuilder AddToolBarItem(string text, Func<IServiceProvider, CancellationToken, Task> action, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(text, action, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action and captures its configuration definition.</summary>
	public ApplicationBlockBuilder AddToolBarItem(string text, Func<IServiceProvider, CancellationToken, Task> action, out IApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return ConfigureToolBarItem(item => item.WithAction(action), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation for the owning block's first matching registration.</summary>
	public ApplicationBlockBuilder AddToolBarItem<TScreen>(string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null)
		where TScreen : IApplicationScreen {
		return AddToolBarItem(typeof(TScreen), text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds typed toolbar navigation and captures its configuration definition.</summary>
	public ApplicationBlockBuilder AddToolBarItem<TScreen>(string text, out IApplicationMenuItem registration, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null)
		where TScreen : IApplicationScreen {
		return AddToolBarItem(typeof(TScreen), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation without creating or changing a screen registration.</summary>
	public ApplicationBlockBuilder AddToolBarItem(Type screenType, string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(screenType, text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation and captures its configuration definition.</summary>
	public ApplicationBlockBuilder AddToolBarItem(Type screenType, string text, out IApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(ApplicationScreenRegistration.ForType(screenType), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation without creating or changing a screen registration.</summary>
	public ApplicationBlockBuilder AddToolBarItem(ApplicationScreenRegistration screen, string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(screen, text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation and captures its configuration definition.</summary>
	public ApplicationBlockBuilder AddToolBarItem(ApplicationScreenRegistration screen, string text, out IApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return ConfigureToolBarItem(item => item.WithScreen(screen), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation without creating or changing a screen registration.</summary>
	public ApplicationBlockBuilder AddToolBarItem(SharedUI.ApplicationScreenDefinition definition, string text = null, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(definition, text, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds toolbar navigation and captures its configuration definition.</summary>
	public ApplicationBlockBuilder AddToolBarItem(SharedUI.ApplicationScreenDefinition definition, string text, out IApplicationMenuItem registration,
		string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		return AddToolBarItem(definition.Registration, text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action group using optional inline web presentation.</summary>
	public ApplicationBlockBuilder AddToolBarItem(string text, IApplicationMenuItem[] children, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return AddToolBarItem(text, children, out _, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar action group and captures its configuration definition.</summary>
	public ApplicationBlockBuilder AddToolBarItem(string text, IApplicationMenuItem[] children,
		out IApplicationMenuItem registration, string id = null, string icon = null, bool isVisible = true, Func<bool> canExecute = null) {
		return ConfigureToolBarItem(item => item.WithChildren(children), text, out registration, id, icon, isVisible, canExecute);
	}

	/// <summary>Adds a toolbar command to the configured block.</summary>
	public ApplicationBlockBuilder AddToolBarItem(IApplicationMenuItem item) {
		return AddToolBarItem(item, out _);
	}

	/// <summary>Adds a toolbar command and captures its configured definition before graph snapshots are built.</summary>
	public ApplicationBlockBuilder AddToolBarItem(IApplicationMenuItem item, out IApplicationMenuItem registration) {
		Guard.ArgumentNotNull(item, nameof(item));
		_toolBarItems.Add(item);
		registration = item;
		return this;
	}

	/// <summary>Adds a toolbar command to the configured block.</summary>
	public ApplicationBlockBuilder AddToolBarItem(Action<ApplicationMenuItemBuilder> configure) {
		return AddToolBarItem(configure, out _);
	}

	/// <summary>Configures one toolbar command, captures its definition and returns the block builder.</summary>
	public ApplicationBlockBuilder AddToolBarItem(Action<ApplicationMenuItemBuilder> configure, out IApplicationMenuItem registration) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new ApplicationMenuItemBuilder();
		configure(builder);
		return AddToolBarItem(builder.Build(), out registration);
	}

	/// <summary>Adds a toolbar separator at the current declaration position.</summary>
	public ApplicationBlockBuilder AddToolBarSeparator() {
		return AddToolBarItem(new BlazorMenuSeparator());
	}

	/// <inheritdoc />
	protected override void ValidateName(string name) {
		Guard.ArgumentNotNullOrEmpty(name, nameof(name));
	}

	/// <inheritdoc />
	protected override void ValidateScreenType(Type screenType) {
		Tools.UI.ValidateScreenType(screenType, typeof(IApplicationScreen));
	}

	/// <inheritdoc />
	protected override ApplicationBlock CreateBlock(IReadOnlyList<IApplicationMenu> menus) {
		return ApplicationBlockSnapshot.Create(new BlockSource {
			Id = _id ?? Name, Title = Name, Position = Position, IconUrl = IconUrl, Tooltip = Tooltip, BlockSettings = BlockSettings,
			ScreenDefinitions = ScreenDefinitions, DefaultScreenDefinition = DefaultScreenDefinition,
			DefaultScreen = DefaultScreen, DefaultScreenTitle = DefaultScreenTitle,
			DefaultScreenActivationMode = DefaultScreenActivationMode, DefaultScreenKind = DefaultScreenKind, Menus = menus.ToArray(), ToolBarItems = _toolBarItems.ToArray()
		});
	}

	// Reuse the child builder's behavior validation and the block's existing toolbar ownership path.
	private ApplicationBlockBuilder ConfigureToolBarItem(Action<ApplicationMenuItemBuilder> configureBehavior, string text,
		out IApplicationMenuItem registration, string id, string icon, bool isVisible, Func<bool> canExecute) {
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
	private static ApplicationScreenDefinition CreateScreenDefinition(Type screenType, ApplicationScreenRegistration registration,
		string title, ScreenActivationMode? activationMode, ScreenKind screenKind, string stableKey, SettingsObject settings,
		IReadOnlyDictionary<string, object> parameters,
		Func<ApplicationScreenActivationContext, ApplicationScreenActivation> activator, Type settingsEditor) {
		Tools.UI.ValidateScreenType(screenType, typeof(IApplicationScreen));
		Tools.UI.ValidateScreenPolicy(activationMode, screenKind);
		Guard.Argument(title == null || title.Length > 0, nameof(title), "A screen title cannot be empty.");

		return new ApplicationScreenDefinition(screenType, title, activationMode, screenKind, registration, stableKey, parameters, activator, settings, settingsEditor);
	}

	#region Inner Types

	/// <summary>Builder inputs are borrowed until the final snapshot owns their cloned graph.</summary>
	private sealed class BlockSource : IApplicationBlock {
		/// <inheritdoc />
		public IApplication Parent { get; set; }

		/// <inheritdoc />
		public IBlazorPlugin Source { get; set; }

		/// <inheritdoc />
		public string Id { get; init; }

		/// <inheritdoc />
		public string Title { get; init; }

		/// <inheritdoc />
		public int Position { get; init; }

		/// <inheritdoc />
		public string IconUrl { get; init; }

		/// <inheritdoc />
		public string Tooltip { get; init; }

		/// <inheritdoc />
		public IApplicationMenu[] Menus { get; init; }

		/// <inheritdoc />
		public IApplicationMenuItem[] ToolBarItems { get; init; }

		/// <inheritdoc />
		public SettingsObject BlockSettings { get; set; }

		/// <inheritdoc />
		public SharedUI.ApplicationScreenDefinition[] ScreenDefinitions { get; init; }

		/// <inheritdoc />
		public SharedUI.ApplicationScreenDefinition DefaultScreenDefinition { get; init; }

		/// <inheritdoc />
		public Type DefaultScreen { get; init; }

		/// <inheritdoc />
		public string DefaultScreenTitle { get; init; }

		/// <inheritdoc />
		public ScreenActivationMode? DefaultScreenActivationMode { get; init; }

		/// <inheritdoc />
		public ScreenKind DefaultScreenKind { get; init; }
	}

	#endregion
}
