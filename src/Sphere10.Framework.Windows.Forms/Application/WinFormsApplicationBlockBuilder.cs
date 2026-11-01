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
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

public class WinFormsApplicationBlockBuilder : ApplicationBlockBuilderBase<WinFormsApplicationBlockBuilder, IWinFormsApplicationMenu, WinFormsApplicationMenuBuilder, WinFormsApplicationBlock> {
	private WinFormsApplicationBlock _block;
	private Image _image32x32;
	private Image _image8x8;
	private string _helpFile;
	private bool _showInMenuStrip;
	private bool _showInToolStrip;

	/// <summary>Adds a native screen using optional inline registration metadata.</summary>
	public WinFormsApplicationBlockBuilder AddScreen<TScreen>(string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, string stableKey = null, object settings = null, Func<object> settingsFactory = null,
		Func<WinFormsApplicationScreenActivationContext, WinFormsApplicationScreen> activator = null,
		Func<WinFormsApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor = null)
		where TScreen : WinFormsApplicationScreen {
		return AddScreen<TScreen>(title, out _, activationMode, screenKind, stableKey, settings, settingsFactory, activator, settingsEditor);
	}

	/// <summary>Adds a native screen and captures the exact registration for later navigation.</summary>
	public WinFormsApplicationBlockBuilder AddScreen<TScreen>(string title, out ApplicationScreenRegistration<TScreen> registration,
		ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal, string stableKey = null,
		object settings = null, Func<object> settingsFactory = null,
		Func<WinFormsApplicationScreenActivationContext, WinFormsApplicationScreen> activator = null,
		Func<WinFormsApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor = null)
		where TScreen : WinFormsApplicationScreen {
		registration = new ApplicationScreenRegistration<TScreen>();
		AddScreenDefinitionInternal(CreateScreenDefinition(typeof(TScreen), registration, title, activationMode, screenKind, stableKey, settings, settingsFactory, activator, settingsEditor));
		return this;
	}

	/// <summary>Adds a runtime screen type using the same optional registration metadata.</summary>
	public WinFormsApplicationBlockBuilder AddScreen(Type screenType, string title = null, ScreenActivationMode? activationMode = null,
		ScreenKind screenKind = ScreenKind.Normal, string stableKey = null, object settings = null, Func<object> settingsFactory = null,
		Func<WinFormsApplicationScreenActivationContext, WinFormsApplicationScreen> activator = null,
		Func<WinFormsApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor = null) {
		return AddScreen(screenType, title, out _, activationMode, screenKind, stableKey, settings, settingsFactory, activator, settingsEditor);
	}

	/// <summary>Adds a runtime screen type and captures its exact registration.</summary>
	public WinFormsApplicationBlockBuilder AddScreen(Type screenType, string title, out ApplicationScreenRegistration registration,
		ScreenActivationMode? activationMode = null, ScreenKind screenKind = ScreenKind.Normal, string stableKey = null,
		object settings = null, Func<object> settingsFactory = null,
		Func<WinFormsApplicationScreenActivationContext, WinFormsApplicationScreen> activator = null,
		Func<WinFormsApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor = null) {
		registration = new ApplicationScreenRegistration(screenType);
		AddScreenDefinitionInternal(CreateScreenDefinition(screenType, registration, title, activationMode, screenKind, stableKey, settings, settingsFactory, activator, settingsEditor));
		return this;
	}

	/// <summary>Adds a menu with optional native presentation and child configuration.</summary>
	public WinFormsApplicationBlockBuilder AddMenu(string text, Image image32x32 = null, bool showInMenuStrip = false,
		Action<WinFormsApplicationMenuBuilder> configure = null) {
		return AddMenu(text, out _, image32x32, showInMenuStrip, configure);
	}

	/// <summary>Adds a configured menu and captures the owned menu definition.</summary>
	public WinFormsApplicationBlockBuilder AddMenu(string text, out IWinFormsApplicationMenu registration, Image image32x32 = null,
		bool showInMenuStrip = false, Action<WinFormsApplicationMenuBuilder> configure = null) {
		return AddMenu(menu => {
			menu
				.WithText(text)
				.WithImage32x32(image32x32)
				.ShowInMenuStrip(showInMenuStrip);

			configure?.Invoke(menu);
		}, out registration);
	}

	/// <summary>Adds a top-level menu through the block's existing menu ownership path.</summary>
	public WinFormsApplicationBlockBuilder AddMenuItem(string text, Image image32x32 = null, bool showInMenuStrip = false,
		Action<WinFormsApplicationMenuBuilder> configure = null) {
		return AddMenu(text, image32x32, showInMenuStrip, configure);
	}

	/// <summary>Adds a top-level menu and captures its definition for later composition.</summary>
	public WinFormsApplicationBlockBuilder AddMenuItem(string text, out IWinFormsApplicationMenu registration, Image image32x32 = null,
		bool showInMenuStrip = false, Action<WinFormsApplicationMenuBuilder> configure = null) {
		return AddMenu(text, out registration, image32x32, showInMenuStrip, configure);
	}

	public WinFormsApplicationBlockBuilder WithImage32x32(Image image) {
		_image32x32 = image;
		if (_block != null)
			_block.Image32x32 = image;
		return this;
	}

	public WinFormsApplicationBlockBuilder WithImage8x8(Image image) {
		_image8x8 = image;
		if (_block != null)
			_block.Image8x8 = image;
		return this;
	}

	public WinFormsApplicationBlockBuilder WithHelpFile(string helpFile) {
		_helpFile = helpFile;
		if (_block != null)
			_block.HelpFileCHM = helpFile;
		return this;
	}

	public WinFormsApplicationBlockBuilder ShowInMenuStrip(bool show = true) {
		_showInMenuStrip = show;
		if (_block != null)
			_block.ShowInMenuStrip = show;
		return this;
	}

	public WinFormsApplicationBlockBuilder ShowInToolStrip(bool show = true) {
		_showInToolStrip = show;
		if (_block != null)
			_block.ShowInToolStrip = show;
		return this;
	}

	/// <summary>Configures and adds one native screen while returning the owning block builder.</summary>
	public WinFormsApplicationBlockBuilder AddScreen<TScreen>(Action<WinFormsApplicationScreenBuilder<TScreen>> configure)
		where TScreen : WinFormsApplicationScreen {
		return AddScreen(configure, out _);
	}

	/// <summary>Adds one native registration and returns its typed identity after configuration completes.</summary>
	public WinFormsApplicationBlockBuilder AddScreen<TScreen>(Action<WinFormsApplicationScreenBuilder<TScreen>> configure,
		out ApplicationScreenRegistration<TScreen> registration)
		where TScreen : WinFormsApplicationScreen {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new WinFormsApplicationScreenBuilder<TScreen>();
		configure(builder);
		var definition = builder.Build();
		registration = (ApplicationScreenRegistration<TScreen>)definition.Registration;
		AddScreenDefinitionInternal(definition);
		return this;
	}

	/// <summary>Accepts a callback that supplies its screen type explicitly.</summary>
	public WinFormsApplicationBlockBuilder AddScreen(Action<WinFormsApplicationScreenBuilder> configure) {
		return AddScreen(configure, out _);
	}

	/// <summary>Adds an explicitly typed native registration and returns its identity.</summary>
	public WinFormsApplicationBlockBuilder AddScreen(Action<WinFormsApplicationScreenBuilder> configure,
		out ApplicationScreenRegistration registration) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new WinFormsApplicationScreenBuilder();
		configure(builder);
		var definition = builder.Build();
		registration = definition.Registration;
		AddScreenDefinitionInternal(definition);
		return this;
	}

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
		}
	}

	protected override void OnMenuAdded(IWinFormsApplicationMenu menu) {
		_block?.AddMenu(menu);
	}

	/// <summary>Retained native products keep direct metadata edits as the authoritative configuration.</summary>
	protected override void ValidateConfiguration() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(_block?.Name ?? Name), "Block name is required.");
	}

	protected override void ValidateScreenType(Type screenType) {
		Tools.UI.ValidateScreenType(screenType, typeof(WinFormsApplicationScreen));
	}

	protected override WinFormsApplicationBlock CreateBlock(IReadOnlyList<IWinFormsApplicationMenu> menus) {
		if (_block != null)
			return _block;
		_block = new WinFormsApplicationBlock(Name, _showInToolStrip, _showInMenuStrip, _image32x32, _image8x8, _helpFile, null) {
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
	private static WinFormsApplicationScreenDefinition CreateScreenDefinition(Type screenType, ApplicationScreenRegistration registration,
		string title, ScreenActivationMode? activationMode, ScreenKind screenKind, string stableKey, object settings,
		Func<object> settingsFactory, Func<WinFormsApplicationScreenActivationContext, WinFormsApplicationScreen> activator,
		Func<WinFormsApplicationSettingsEditorContext, ICrudEntityEditor<object>> settingsEditor) {
		Tools.UI.ValidateScreenType(screenType, typeof(WinFormsApplicationScreen));
		Tools.UI.ValidateScreenPolicy(activationMode, screenKind);
		Guard.Argument(title == null || title.Length > 0, nameof(title), "A screen title cannot be empty.");

		if (settingsFactory != null) {
			settings ??= settingsFactory();
			Guard.ArgumentNotNull(settings, nameof(settingsFactory));
		}

		return new WinFormsApplicationScreenDefinition(screenType, title, activationMode, screenKind, registration, stableKey, activator, settings, settingsEditor, settingsFactory);
	}
}
