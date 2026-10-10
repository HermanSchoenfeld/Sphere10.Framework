// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class BlazorApplicationMenuItemBuilder : ApplicationMenuItemBuilderBase<BlazorApplicationMenuItemBuilder>, IApplicationUIBuilder<BlazorApplicationMenuItemBase> {
	private string _icon;
	private bool _isVisible = true;
	private Func<bool> _canExecute;
	private IBlazorApplicationMenuItem[] _children;
	private string _id;
	private IReadOnlyDictionary<string, object> Parameters { get; set; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
	private string Id => _id ?? ScreenType?.Name ?? Text;

	public BlazorApplicationMenuItemBuilder WithId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
		return this;
	}

	public BlazorApplicationMenuItemBuilder WithTitle(string title) => WithText(title);

	public BlazorApplicationMenuItemBuilder WithIcon(string icon) {
		_icon = icon;
		return this;
	}

	public BlazorApplicationMenuItemBuilder WithParameters(IReadOnlyDictionary<string, object> parameters) {
		Guard.ArgumentNotNull(parameters, nameof(parameters));
		Parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(parameters));
		return this;
	}

	/// <summary>Changes presentation without unregistering the command or its screen.</summary>
	public BlazorApplicationMenuItemBuilder WithVisibility(bool isVisible) {
		_isVisible = isVisible;
		return this;
	}

	public BlazorApplicationMenuItemBuilder EnabledWhen(Func<bool> canExecute) {
		Guard.ArgumentNotNull(canExecute, nameof(canExecute));
		_canExecute = canExecute;
		return this;
	}

	/// <summary>Creates an action group. Screen registrations remain top-level block menu items.</summary>
	public BlazorApplicationMenuItemBuilder WithChildren(params IBlazorApplicationMenuItem[] children) {
		Guard.ArgumentNotNull(children, nameof(children));
		Guard.Argument(children.Length > 0 && children.All(child => child != null), nameof(children), "An action group requires children.");
		_children = children.ToArray();
		return this;
	}

	public BlazorApplicationMenuItemBase Build() {
		Guard.Ensure(ScreenType != null || Parameters.Count == 0, "Only screen items can declare component parameters.");
		if (_children != null) {
			Guard.Ensure(!string.IsNullOrWhiteSpace(Text), "Menu item text is required.");
			Guard.Ensure(ScreenType == null && Action == null && AsyncAction == null && !IsDefault && ActivationMode == null && ScreenTitle == null && ScreenKind == ScreenKind.Normal,
				"An action group cannot also declare an action or screen behavior.");
			return (BlazorApplicationMenuGroup)BlazorApplicationBlockSnapshot.CreateItem(new BlazorApplicationMenuGroup {
				Id = Id, Title = Text, Icon = _icon, IsVisible = _isVisible, CanExecute = _canExecute, Items = _children
			});
		}
		ValidateItem();
		if (ScreenType != null)
			return new BlazorScreenMenuItem {
				Id = Id, Title = Text, Icon = _icon, IsVisible = _isVisible, CanExecute = _canExecute, ScreenDefinition = ScreenDefinition,
				IsDefault = IsDefault, Parameters = Parameters
			};
		return new BlazorActionMenuItem { Id = Id, Title = Text, Icon = _icon, IsVisible = _isVisible, CanExecute = _canExecute, Action = Action, AsyncAction = AsyncAction };
	}

	protected override void ValidateText(string text) => Guard.ArgumentNotNullOrEmpty(text, nameof(text));

	protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));
}
