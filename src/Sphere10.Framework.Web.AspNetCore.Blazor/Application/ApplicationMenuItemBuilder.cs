// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class ApplicationMenuItemBuilder : ApplicationMenuItemBuilderBase<ApplicationMenuItemBuilder>, IApplicationUIBuilder<ApplicationMenuItemBase> {
	private string _icon;
	private bool _isVisible = true;
	private Func<bool> _canExecute;
	private IApplicationMenuItem[] _children;
	private string _id;
	private string Id => _id ?? ScreenType?.Name ?? Text;

	public ApplicationMenuItemBuilder WithId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
		return this;
	}

	public ApplicationMenuItemBuilder WithTitle(string title) => WithText(title);

	public ApplicationMenuItemBuilder WithIcon(string icon) {
		_icon = icon;
		return this;
	}

	/// <summary>Changes presentation without unregistering the command or its screen.</summary>
	public ApplicationMenuItemBuilder WithVisibility(bool isVisible) {
		_isVisible = isVisible;
		return this;
	}

	public ApplicationMenuItemBuilder EnabledWhen(Func<bool> canExecute) {
		Guard.ArgumentNotNull(canExecute, nameof(canExecute));
		_canExecute = canExecute;
		return this;
	}

	/// <summary>Creates an action group; its commands do not register operational screens.</summary>
	public ApplicationMenuItemBuilder WithChildren(params IApplicationMenuItem[] children) {
		Guard.ArgumentNotNull(children, nameof(children));
		Guard.Argument(children.Length > 0 && children.All(child => child != null), nameof(children), "An action group requires children.");
		_children = children.ToArray();
		return this;
	}

	public ApplicationMenuItemBase Build() {
		if (_children != null) {
			Guard.Ensure(!string.IsNullOrWhiteSpace(Text), "Menu item text is required.");
			Guard.Ensure(ScreenType == null && Action == null && AsyncAction == null,
				"An action group cannot also declare an action or screen behavior.");
			return (ApplicationMenuGroup)ApplicationBlockSnapshot.CreateItem(new ApplicationMenuGroup {
				Id = Id, Title = Text, Icon = _icon, IsVisible = _isVisible, CanExecute = _canExecute, Items = _children
			});
		}
		ValidateItem();
		if (ScreenType != null)
			return new BlazorScreenMenuItem {
				Id = _id, Title = Text, Icon = _icon, IsVisible = _isVisible, CanExecute = _canExecute, Registration = ScreenRegistration
			};
		return new BlazorActionMenuItem { Id = Id, Title = Text, Icon = _icon, IsVisible = _isVisible, CanExecute = _canExecute, Action = Action, AsyncAction = AsyncAction };
	}

	protected override void ValidateText(string text) => Guard.ArgumentNotNullOrEmpty(text, nameof(text));

	protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IApplicationScreen));
}
