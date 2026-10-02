// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public class BlazorApplicationMenuItemBuilder : ApplicationMenuItemBuilderBase {
	private string _icon;

	public BlazorApplicationMenuItemBuilder WithId(string id) {
		SetId(id);
		return this;
	}

	public BlazorApplicationMenuItemBuilder WithText(string text) {
		Guard.ArgumentNotNullOrEmpty(text, nameof(text));
		SetText(text);
		return this;
	}

	public BlazorApplicationMenuItemBuilder WithTitle(string title) => WithText(title);

	public BlazorApplicationMenuItemBuilder WithIcon(string icon) {
		_icon = icon;
		return this;
	}

	public BlazorApplicationMenuItemBuilder WithScreen<TScreen>() where TScreen : IComponent, IBlazorApplicationScreen => WithScreen(typeof(TScreen));

	public BlazorApplicationMenuItemBuilder WithScreen(Type screenType) {
		SetScreenType(screenType);
		return this;
	}

	public BlazorApplicationMenuItemBuilder AsSingleInstance() {
		SetActivationMode(ScreenActivationMode.SingleInstance);
		return this;
	}

	public BlazorApplicationMenuItemBuilder AsMultiInstance() {
		SetActivationMode(ScreenActivationMode.MultiInstance);
		return this;
	}

	public BlazorApplicationMenuItemBuilder AsPermanentSingleton() {
		SetActivationMode(ScreenActivationMode.PermanentSingleton);
		return this;
	}

	public BlazorApplicationMenuItemBuilder WithScreenKind(ScreenKind kind) {
		SetScreenKind(kind);
		return this;
	}

	public BlazorApplicationMenuItemBuilder AsDefault(bool isDefault = true) {
		SetIsDefault(isDefault);
		return this;
	}

	public BlazorApplicationMenuItemBuilder WithParameters(IReadOnlyDictionary<string, object> parameters) {
		SetParameters(parameters);
		return this;
	}

	public BlazorApplicationMenuItemBuilder WithAction(Func<IServiceProvider, CancellationToken, Task> action) {
		SetAction(action);
		return this;
	}

	public BlazorApplicationMenuItem Build() {
		ValidateItem();
		if (ScreenType != null)
			return new BlazorScreenMenuItem {
				Id = Id, Title = Text, Icon = _icon, ScreenType = ScreenType,
				ActivationMode = ActivationMode ?? ScreenActivationMode.MultiInstance, ScreenKind = ScreenKind, IsDefault = IsDefault, Parameters = Parameters
			};
		return new BlazorActionMenuItem { Id = Id, Title = Text, Icon = _icon, AsyncAction = AsyncAction };
	}

	protected override void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType, typeof(IBlazorApplicationScreen));
}
