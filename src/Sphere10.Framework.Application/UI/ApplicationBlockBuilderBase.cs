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

/// <summary>Accumulates neutral block configuration; UI builders supply their platform-specific product.</summary>
public abstract class ApplicationBlockBuilderBase<TMenu, TBlock> : IApplicationUIBuilder<TBlock>
	where TMenu : IApplicationMenu
	where TBlock : IApplicationBlock {
	private readonly List<TMenu> _menus = new();
	private readonly List<ApplicationScreenDefinition> _screenDefinitions = new();
	private ApplicationScreenDefinition _defaultScreenTemplate;
	private bool _inheritDefaultScreenTitle;

	protected string Name { get; private set; }

	protected int Position { get; private set; }

	protected string IconUrl { get; private set; }

	protected string Tooltip { get; private set; }

	protected ApplicationScreenDefinition DefaultScreenDefinition { get; private set; }

	protected Type DefaultScreen => DefaultScreenDefinition?.ScreenType;

	protected string DefaultScreenTitle => DefaultScreenDefinition?.Title;

	protected ScreenActivationMode? DefaultScreenActivationMode => DefaultScreenDefinition?.ActivationMode;

	protected ScreenKind DefaultScreenKind => DefaultScreenDefinition?.ScreenKind ?? ScreenKind.Normal;

	protected bool IsTypeBasedDefault => _defaultScreenTemplate != null;

	protected ApplicationScreenDefinition[] ScreenDefinitions => _screenDefinitions
		.Concat(_menus.SelectMany(menu => menu.Items).OfType<IScreenMenuItem>().Select(item => item.ScreenDefinition))
		.Concat(DefaultScreenDefinition == null ? Array.Empty<ApplicationScreenDefinition>() : new[] { DefaultScreenDefinition })
		.Distinct<ApplicationScreenDefinition>(ReferenceEqualityComparer.Instance).ToArray();

	public virtual TBlock Build() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Name), "Block name is required.");
		ResolveDefaultScreen();
		var block = CreateBlock(Array.AsReadOnly(_menus.ToArray()));
		RegisterScreenDefinitions(block);
		Tools.UI.GetScreenDefinitions(new IApplicationBlock[] { block });
		Guard.Ensure(ScreenDefinitions.GroupBy(definition => (definition.ScreenType, definition.Title, definition.ActivationMode, definition.ScreenKind))
			.All(group => block.ScreenDefinitions.Count(candidate => (candidate.ScreenType, candidate.Title, candidate.ActivationMode, candidate.ScreenKind) == group.Key) >= group.Count()),
			"The block factory must own every configured screen definition.");
		return block;
	}

	protected virtual void RegisterScreenDefinitions(TBlock block) {
		if (block is ApplicationBlockBase storage) {
			foreach (var definition in ScreenDefinitions)
				storage.AddScreenDefinition(definition);
			if (DefaultScreenDefinition != null)
				storage.DefaultScreenDefinition = DefaultScreenDefinition;
		}
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
		_defaultScreenTemplate = new ApplicationScreenDefinition(screenType, title, activationMode, screenKind);
		_inheritDefaultScreenTitle = title == null;
		DefaultScreenDefinition = _defaultScreenTemplate;
		OnConfigurationChanged(nameof(DefaultScreenDefinition));
	}

	protected void SetDefaultScreen(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		ValidateScreenType(definition.ScreenType);
		_defaultScreenTemplate = null;
		DefaultScreenDefinition = definition;
		OnConfigurationChanged(nameof(DefaultScreenDefinition));
	}

	protected void AddScreenDefinitionInternal(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		ValidateScreenType(definition.ScreenType);
		if (!_screenDefinitions.Any(candidate => ReferenceEquals(candidate, definition)))
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

	protected virtual void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType);

	protected abstract TBlock CreateBlock(IReadOnlyList<TMenu> menus);

	/// <summary>Supplies canonical definitions eligible for type-based default inference.</summary>
	protected virtual IEnumerable<ApplicationScreenDefinition> GetDefaultScreenDefinitions() =>
		_menus.SelectMany(menu => menu.Items).OfType<IScreenMenuItem>().Select(item => item.ScreenDefinition).Concat(_screenDefinitions);

	/// <summary>Type-based convenience defaults inherit owned metadata; explicitly supplied definitions remain canonical.</summary>
	protected virtual void ResolveDefaultScreen() {
		if (_defaultScreenTemplate == null)
			return;
		var matching = GetDefaultScreenDefinitions().FirstOrDefault(definition => definition.ScreenType == _defaultScreenTemplate.ScreenType);
		var title = _inheritDefaultScreenTitle ? matching?.Title ?? _defaultScreenTemplate.Title : _defaultScreenTemplate.Title;
		var mode = _defaultScreenTemplate.ActivationMode ?? matching?.ActivationMode;
		var kind = _defaultScreenTemplate.ScreenKind == ScreenKind.Normal ? matching?.ScreenKind ?? ScreenKind.Normal : _defaultScreenTemplate.ScreenKind;
		var resolved = DefaultScreenDefinition;
		if (matching == null)
			resolved = _defaultScreenTemplate;
		else if (matching.Title == title && matching.ActivationMode == mode && matching.ScreenKind == kind)
			resolved = matching;
		else if (resolved.Title != title || resolved.ActivationMode != mode || resolved.ScreenKind != kind)
			resolved = new ApplicationScreenDefinition(_defaultScreenTemplate.ScreenType, title, mode, kind);
		if (ReferenceEquals(DefaultScreenDefinition, resolved))
			return;
		DefaultScreenDefinition = resolved;
		OnConfigurationChanged(nameof(DefaultScreenDefinition));
	}
}

/// <summary>Shared block metadata, screen policy and typed menu composition.</summary>
public abstract class ApplicationBlockBuilderBase<TSelf, TMenu, TMenuBuilder, TBlock> : ApplicationBlockBuilderBase<TMenu, TBlock>
	where TSelf : class
	where TMenu : IApplicationMenu
	where TBlock : IApplicationBlock
	where TMenuBuilder : IApplicationUIBuilder<TMenu>, new() {
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

	public TSelf WithDefaultScreen(ApplicationScreenDefinition definition) {
		SetDefaultScreen(definition);
		return (TSelf)(object)this;
	}

	public TSelf AddScreenDefinition(ApplicationScreenDefinition definition) {
		AddScreenDefinitionInternal(definition);
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
