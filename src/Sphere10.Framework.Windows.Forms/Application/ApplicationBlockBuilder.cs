// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class ApplicationBlockBuilder : ApplicationBlockBuilderBase<ApplicationBlockBuilder, IApplicationMenu, ApplicationMenuBuilder, ApplicationBlock> {
	private ApplicationBlock _block;
	private Image _image32x32;
	private Image _image8x8;
	private string _helpFile;
	private bool _showInMenuStrip;
	private bool _showInToolStrip;

	/// <summary>Adds a native screen using optional inline registration metadata.</summary>
	public ApplicationBlockBuilder AddScreen<TScreen>(string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, string stableKey = null, SettingsObject settings = null,
		Func<ApplicationScreenActivationContext, ApplicationScreen> activator = null,
		Func<ApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor = null)
		where TScreen : ApplicationScreen {
		return AddScreen<TScreen>(title, out _, activationMode, screenKind, stableKey, settings, activator, settingsEditor);
	}

	/// <summary>Adds a native screen and captures the exact registration for later navigation.</summary>
	public ApplicationBlockBuilder AddScreen<TScreen>(string title, out ApplicationScreenRegistration<TScreen> registration,
		ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal, string stableKey = null,
		SettingsObject settings = null,
		Func<ApplicationScreenActivationContext, ApplicationScreen> activator = null,
		Func<ApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor = null)
		where TScreen : ApplicationScreen {
		registration = new ApplicationScreenRegistration<TScreen>();
		AddScreenDefinitionInternal(CreateScreenDefinition(typeof(TScreen), registration, title, activationMode, screenKind, stableKey, settings, activator, settingsEditor));
		return this;
	}

	/// <summary>Adds a runtime screen type using the same optional registration metadata.</summary>
	public ApplicationBlockBuilder AddScreen(Type screenType, string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, string stableKey = null, SettingsObject settings = null,
		Func<ApplicationScreenActivationContext, ApplicationScreen> activator = null,
		Func<ApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor = null) {
		return AddScreen(screenType, title, out _, activationMode, screenKind, stableKey, settings, activator, settingsEditor);
	}

	/// <summary>Adds a runtime screen type and captures its exact registration.</summary>
	public ApplicationBlockBuilder AddScreen(Type screenType, string title, out ApplicationScreenRegistration registration,
		ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal, string stableKey = null,
		SettingsObject settings = null,
		Func<ApplicationScreenActivationContext, ApplicationScreen> activator = null,
		Func<ApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor = null) {
		registration = new ApplicationScreenRegistration(screenType);
		AddScreenDefinitionInternal(CreateScreenDefinition(screenType, registration, title, activationMode, screenKind, stableKey, settings, activator, settingsEditor));
		return this;
	}

	/// <summary>Adds a menu with optional native presentation and child configuration.</summary>
	public ApplicationBlockBuilder AddMenu(string text, Image image32x32 = null, bool showInMenuStrip = false,
		Action<ApplicationMenuBuilder> configure = null) {
		return AddMenu(text, out _, image32x32, showInMenuStrip, configure);
	}

	/// <summary>Adds a configured menu and captures the owned menu definition.</summary>
	public ApplicationBlockBuilder AddMenu(string text, out IApplicationMenu registration, Image image32x32 = null,
		bool showInMenuStrip = false, Action<ApplicationMenuBuilder> configure = null) {
		return AddMenu(menu => {
			menu
				.WithText(text)
				.WithImage32x32(image32x32)
				.ShowInMenuStrip(showInMenuStrip);

			configure?.Invoke(menu);
		}, out registration);
	}

	/// <summary>Adds a top-level menu through the block's existing menu ownership path.</summary>
	public ApplicationBlockBuilder AddMenuItem(string text, Image image32x32 = null, bool showInMenuStrip = false,
		Action<ApplicationMenuBuilder> configure = null) {
		return AddMenu(text, image32x32, showInMenuStrip, configure);
	}

	/// <summary>Adds a top-level menu and captures its definition for later composition.</summary>
	public ApplicationBlockBuilder AddMenuItem(string text, out IApplicationMenu registration, Image image32x32 = null,
		bool showInMenuStrip = false, Action<ApplicationMenuBuilder> configure = null) {
		return AddMenu(text, out registration, image32x32, showInMenuStrip, configure);
	}

	/// <summary>Sets the block image used by native navigation.</summary>
	public ApplicationBlockBuilder WithImage32x32(Image image) {
		_image32x32 = image;
		if (_block != null)
			_block.Image32x32 = image;
		return this;
	}

	/// <summary>Sets the compact block image used by native navigation.</summary>
	public ApplicationBlockBuilder WithImage8x8(Image image) {
		_image8x8 = image;
		if (_block != null)
			_block.Image8x8 = image;
		return this;
	}

	/// <summary>Sets the block's native help file.</summary>
	public ApplicationBlockBuilder WithHelpFile(string helpFile) {
		_helpFile = helpFile;
		if (_block != null)
			_block.HelpFileCHM = helpFile;
		return this;
	}

	/// <summary>Selects whether the block appears in the native menu strip.</summary>
	public ApplicationBlockBuilder ShowInMenuStrip(bool show = true) {
		_showInMenuStrip = show;
		if (_block != null)
			_block.ShowInMenuStrip = show;
		return this;
	}

	/// <summary>Selects whether the block appears in the native toolbar.</summary>
	public ApplicationBlockBuilder ShowInToolStrip(bool show = true) {
		_showInToolStrip = show;
		if (_block != null)
			_block.ShowInToolStrip = show;
		return this;
	}

	/// <summary>Configures and adds one native screen while returning the owning block builder.</summary>
	public ApplicationBlockBuilder AddScreen<TScreen>(Action<ApplicationScreenBuilder<TScreen>> configure)
		where TScreen : ApplicationScreen {
		return AddScreen(configure, out _);
	}

	/// <summary>Adds one native registration and returns its typed identity after configuration completes.</summary>
	public ApplicationBlockBuilder AddScreen<TScreen>(Action<ApplicationScreenBuilder<TScreen>> configure,
		out ApplicationScreenRegistration<TScreen> registration)
		where TScreen : ApplicationScreen {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new ApplicationScreenBuilder<TScreen>();
		configure(builder);
		var definition = builder.Build();
		registration = (ApplicationScreenRegistration<TScreen>)definition.Registration;
		AddScreenDefinitionInternal(definition);
		return this;
	}

	/// <summary>Accepts a callback that supplies its screen type explicitly.</summary>
	public ApplicationBlockBuilder AddScreen(Action<ApplicationScreenBuilder> configure) {
		return AddScreen(configure, out _);
	}

	/// <summary>Adds an explicitly typed native registration and returns its identity.</summary>
	public ApplicationBlockBuilder AddScreen(Action<ApplicationScreenBuilder> configure,
		out ApplicationScreenRegistration registration) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new ApplicationScreenBuilder();
		configure(builder);
		var definition = builder.Build();
		registration = definition.Registration;
		AddScreenDefinitionInternal(definition);
		return this;
	}

	/// <summary>Enforces the existing builder configuration boundary after block activation.</summary>
	protected override void OnConfigurationChanged(string propertyName) {
		if (_block == null)
			return;
		switch (propertyName) {
			case nameof(Name):
				_block.Name = Name;
				break;
			case nameof(Position):
				_block.Position = Position;
				break;
			case nameof(BlockSettings):
				_block.BlockSettings = BlockSettings;
				break;
		}
	}

	/// <summary>Registers the menu through the block's existing ownership rules.</summary>
	protected override void OnMenuAdded(IApplicationMenu menu) {
		_block?.AddMenu(menu);
	}

	// Resolving navigation during composition must not activate the block's immutable runtime command collections.
	protected override void ResolveScreenReferences(ApplicationBlock block) {
		ApplicationHelper.ResolveScreenReferences(block, block.MenuCollection, Array.Empty<SharedUI.IApplicationMenuItem>());
	}

	/// <summary>Retained native products keep direct metadata edits as the authoritative configuration.</summary>
	protected override void ValidateConfiguration() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(_block?.Name ?? Name), "Block name is required.");
	}

	/// <summary>Rejects screen types that cannot be hosted by native application controls.</summary>
	protected override void ValidateScreenType(Type screenType) {
		ApplicationHelper.ValidateScreenType(screenType, typeof(ApplicationScreen));
	}

	/// <summary>Builds the configured native block and attaches its owned menus.</summary>
	protected override ApplicationBlock CreateBlock(IReadOnlyList<IApplicationMenu> menus) {
		if (_block != null)
			return _block;
		_block = new ApplicationBlock(Name, _showInToolStrip, _showInMenuStrip, _image32x32, _image8x8, _helpFile, null) {
			Position = Position
		};
		foreach (var definition in ScreenDefinitions)
			_block.AddScreenDefinition(definition);
		if (DefaultScreenDefinition != null)
			_block.DefaultScreenDefinition = DefaultScreenDefinition;
		foreach (var menu in menus)
			_block.AddMenu(menu);
		return _block;
	}

	// Capture settings defaults during composition; operational screen activation remains deferred.
	private static ApplicationScreenDefinition CreateScreenDefinition(Type screenType, ApplicationScreenRegistration registration,
		string title, ScreenActivationMode? activationMode, ScreenKind screenKind, string stableKey, SettingsObject settings,
		Func<ApplicationScreenActivationContext, ApplicationScreen> activator,
		Func<ApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor) {
		ApplicationHelper.ValidateScreenType(screenType, typeof(ApplicationScreen));
		ApplicationHelper.ValidateScreenPolicy(activationMode, screenKind);
		Guard.Argument(title == null || title.Length > 0, nameof(title), "A screen title cannot be empty.");

		return new ApplicationScreenDefinition(screenType, title, activationMode, screenKind, registration, stableKey, activator, settings, settingsEditor);
	}
}
