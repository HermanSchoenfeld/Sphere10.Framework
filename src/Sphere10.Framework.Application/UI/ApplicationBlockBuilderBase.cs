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

namespace Sphere10.Framework.Application.UI;

/// <summary>Accumulates screen registrations and presentation commands for platform block builders.</summary>
public abstract class ApplicationBlockBuilderBase<TMenu, TBlock> : IApplicationUIBuilder<TBlock>
	where TMenu : IApplicationMenu
	where TBlock : IApplicationBlock, IApplicationCommandProvider {
	private readonly List<TMenu> _menus = new();
	private readonly List<ApplicationScreenDefinition> _screenDefinitions = new();
	private ApplicationScreenRegistration _defaultScreenRegistration;

	protected string Name { get; private set; }

	protected int Position { get; private set; }

	protected SettingsObject BlockSettings { get; private set; }

	protected string IconUrl { get; private set; }

	protected string Tooltip { get; private set; }

	protected ApplicationScreenDefinition DefaultScreenDefinition { get; private set; }

	protected Type DefaultScreen => DefaultScreenDefinition?.ScreenType;

	protected string DefaultScreenTitle => DefaultScreenDefinition?.Title;

	protected ScreenActivationMode? DefaultScreenActivationMode => DefaultScreenDefinition?.ActivationMode;

	protected ScreenKind DefaultScreenKind => DefaultScreenDefinition?.ScreenKind ?? ScreenKind.Normal;

	protected ApplicationScreenDefinition[] ScreenDefinitions => _screenDefinitions.ToArray();

	public virtual TBlock Build() {
		ValidateConfiguration();
		ResolveDefaultScreen();
		var block = CreateBlock(Array.AsReadOnly(_menus.ToArray()));
		if (BlockSettings != null)
			block.BlockSettings = BlockSettings;
		RegisterScreenDefinitions(block);
		ResolveScreenReferences(block);
		ApplicationHelper.GetScreenDefinitions(new IApplicationBlock[] { block });
		Guard.Ensure(ScreenDefinitions.All(definition => block.ScreenDefinitions.Any(candidate => candidate.Registration.Token == definition.Registration.Token)),
			"The block factory must own every configured screen definition.");
		return block;
	}

	protected virtual void RegisterScreenDefinitions(TBlock block) {
		if (block is ApplicationBlockBase storage) {
			foreach (var definition in ScreenDefinitions)
				storage.AddScreenDefinition(definition);
			if (DefaultScreenDefinition != null)
				storage.DefaultScreenDefinition = ApplicationHelper.ResolveScreenRegistration(block.ScreenDefinitions, DefaultScreenDefinition.Registration);
		}
	}

	/// <summary>Completes pending navigation bindings against this product's canonical registrations.</summary>
	protected virtual void ResolveScreenReferences(TBlock block) {
		ApplicationHelper.ResolveScreenReferences(block, block);
	}

	protected void SetBlockSettings(SettingsObject settings) {
		Guard.ArgumentNotNull(settings, nameof(settings));
		BlockSettings = settings;
		OnConfigurationChanged(nameof(BlockSettings));
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

	protected void SetDefaultScreen(Type screenType) {
		ValidateScreenType(screenType);
		SetDefaultScreen(ApplicationScreenRegistration.ForType(screenType));
	}

	protected void SetDefaultScreen(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		SetDefaultScreen(definition.Registration);
	}

	protected void SetDefaultScreen(ApplicationScreenRegistration registration) {
		Guard.ArgumentNotNull(registration, nameof(registration));
		ValidateScreenType(registration.ScreenType);
		_defaultScreenRegistration = registration;
		DefaultScreenDefinition = null;
		OnConfigurationChanged(nameof(DefaultScreenDefinition));
	}

	protected void AddScreenDefinitionInternal(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		ValidateScreenType(definition.ScreenType);
		Guard.Argument(!_screenDefinitions.Any(candidate => candidate.Registration.Token == definition.Registration.Token), nameof(definition), "The screen registration has already been added to this block.");
		_screenDefinitions.Add(definition);
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

	/// <summary>Validates the effective product configuration before publishing or updating its registrations.</summary>
	protected virtual void ValidateConfiguration() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Name), "Block name is required.");
	}

	protected virtual void ValidateScreenType(Type screenType) => ApplicationHelper.ValidateScreenType(screenType);

	protected abstract TBlock CreateBlock(IReadOnlyList<TMenu> menus);

	/// <summary>Resolves a default reference without creating or changing registration metadata.</summary>
	protected virtual void ResolveDefaultScreen() {
		DefaultScreenDefinition = _defaultScreenRegistration == null ? null : ApplicationHelper.ResolveScreenRegistration(ScreenDefinitions, _defaultScreenRegistration);
	}
}

/// <summary>Shared block metadata, screen policy and typed menu composition.</summary>
public abstract class ApplicationBlockBuilderBase<TSelf, TMenu, TMenuBuilder, TBlock> : ApplicationBlockBuilderBase<TMenu, TBlock>
	where TSelf : class
	where TMenu : IApplicationMenu
	where TBlock : IApplicationBlock, IApplicationCommandProvider
	where TMenuBuilder : IApplicationUIBuilder<TMenu>, new() {
	public TSelf UseSettings<TSettings>()
		where TSettings : SettingsObject, new() {
		return UseSettings(new TSettings());
	}

	public TSelf UseSettings(SettingsObject settings) {
		SetBlockSettings(settings);
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

	public TSelf WithDefaultScreen<TScreen>()
		where TScreen : IApplicationScreen {
		return WithDefaultScreen(typeof(TScreen));
	}

	public TSelf WithDefaultScreen(Type screenType) {
		SetDefaultScreen(screenType);
		return (TSelf)(object)this;
	}

	public TSelf WithDefaultScreen(ApplicationScreenDefinition definition) {
		SetDefaultScreen(definition);
		return (TSelf)(object)this;
	}

	public TSelf WithDefaultScreen(ApplicationScreenRegistration registration) {
		SetDefaultScreen(registration);
		return (TSelf)(object)this;
	}

	/// <summary>Adds an existing compatible screen definition independently of commands.</summary>
	public TSelf AddScreen(ApplicationScreenDefinition definition) {
		return AddScreen(definition, out _);
	}

	/// <summary>Adds a definition and captures its immutable screen registration reference.</summary>
	public TSelf AddScreen(ApplicationScreenDefinition definition, out ApplicationScreenRegistration registration) {
		AddScreenDefinitionInternal(definition);
		registration = definition.Registration;
		return (TSelf)(object)this;
	}

	public TSelf AddMenu(TMenu menu) {
		return AddMenu(menu, out _);
	}

	/// <summary>Adds an existing menu definition and returns its configuration reference.</summary>
	public TSelf AddMenu(TMenu menu, out TMenu registration) {
		AddMenuDefinition(menu);
		registration = menu;
		return (TSelf)(object)this;
	}

	public TSelf AddMenu(Action<TMenuBuilder> configure) {
		return AddMenu(configure, out _);
	}

	/// <summary>Configures one child menu, captures its definition and returns this parent.</summary>
	public TSelf AddMenu(Action<TMenuBuilder> configure, out TMenu registration) {
		Guard.ArgumentNotNull(configure, nameof(configure));
		var builder = new TMenuBuilder();
		configure(builder);
		return AddMenu(builder.Build(), out registration);
	}
}
