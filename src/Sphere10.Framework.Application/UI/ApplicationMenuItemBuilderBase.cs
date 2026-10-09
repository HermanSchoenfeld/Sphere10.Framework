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
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Shared screen/action selection and validation for platform menu item builders.</summary>
public abstract class ApplicationMenuItemBuilderBase {
	private string _id;

	protected string Id => _id ?? ScreenType?.Name ?? Text;

	protected string Text { get; private set; }

	protected Type ScreenType { get; private set; }

	protected string ScreenTitle { get; private set; }

	protected ScreenActivationMode? ActivationMode { get; private set; }

	protected ScreenKind ScreenKind { get; private set; }

	protected bool IsDefault { get; private set; }

	protected IReadOnlyDictionary<string, object> Parameters { get; private set; } = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());

	protected Action Action { get; private set; }

	protected Action<IUserInterfaceServices> UIAction { get; private set; }

	protected Func<IServiceProvider, CancellationToken, Task> AsyncAction { get; private set; }

	protected void SetId(string id) {
		Guard.ArgumentNotNullOrEmpty(id, nameof(id));
		_id = id;
	}

	protected void SetText(string text) {
		ValidateText(text);
		Text = text;
	}

	protected void SetScreenType(Type screenType) {
		ValidateScreenType(screenType);
		Guard.Ensure(Action == null && UIAction == null && AsyncAction == null, "A menu item cannot contain both a screen and an action.");
		ScreenType = screenType;
	}

	protected void SetScreenTitle(string title) => ScreenTitle = title;

	protected void SetActivationMode(ScreenActivationMode activationMode) {
		Tools.UI.ValidateActivationMode(activationMode);
		ActivationMode = activationMode;
	}

	protected void SetScreenKind(ScreenKind screenKind) {
		Tools.UI.ValidateScreenKind(screenKind);
		ScreenKind = screenKind;
	}

	protected void SetIsDefault(bool isDefault) => IsDefault = isDefault;

	protected void SetParameters(IReadOnlyDictionary<string, object> parameters) {
		Guard.ArgumentNotNull(parameters, nameof(parameters));
		Parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(parameters));
	}

	protected void SetAction(Action action) {
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(ScreenType == null, "A menu item cannot contain both a screen and an action.");
		Action = action;
		UIAction = null;
		AsyncAction = null;
	}

	protected void SetAction(Action<IUserInterfaceServices> action) {
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(ScreenType == null, "A menu item cannot contain both a screen and an action.");
		UIAction = action;
		Action = null;
		AsyncAction = null;
	}

	protected void SetAction(Func<IServiceProvider, CancellationToken, Task> action) {
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(ScreenType == null, "A menu item cannot contain both a screen and an action.");
		AsyncAction = action;
		Action = null;
		UIAction = null;
	}

	protected void ValidateItem() {
		Guard.Ensure(!string.IsNullOrWhiteSpace(Text), "Menu item text is required.");
		Guard.Ensure(ScreenType != null || Action != null || UIAction != null || AsyncAction != null, "A screen or action is required.");
		Guard.Ensure(ScreenType != null || !IsDefault && ActivationMode == null && ScreenTitle == null && ScreenKind == ScreenKind.Normal, "Only screen items can declare screen behavior.");
		Tools.UI.ValidateScreenPolicy(ActivationMode, ScreenKind);
	}

	protected virtual void ValidateText(string text) => Guard.ArgumentNotNull(text, nameof(text));

	protected virtual void ValidateScreenType(Type screenType) => Tools.UI.ValidateScreenType(screenType);
}

/// <summary>Shared fluent screen and action configuration. Platform factories validate the item kinds they support.</summary>
public abstract class ApplicationMenuItemBuilderBase<TSelf> : ApplicationMenuItemBuilderBase where TSelf : class {
	public TSelf WithId(string id) {
		SetId(id);
		return (TSelf)(object)this;
	}

	public TSelf WithText(string text) {
		SetText(text);
		return (TSelf)(object)this;
	}

	public TSelf WithScreen(Type screenType) {
		SetScreenType(screenType);
		return (TSelf)(object)this;
	}

	public TSelf WithScreenTitle(string title) {
		SetScreenTitle(title);
		return (TSelf)(object)this;
	}

	public TSelf WithActivationMode(ScreenActivationMode activationMode) {
		SetActivationMode(activationMode);
		return (TSelf)(object)this;
	}

	public TSelf WithScreenKind(ScreenKind screenKind) {
		SetScreenKind(screenKind);
		return (TSelf)(object)this;
	}

	public TSelf AsDefault(bool isDefault = true) {
		SetIsDefault(isDefault);
		return (TSelf)(object)this;
	}

	public TSelf WithAction(Action action) {
		SetAction(action);
		return (TSelf)(object)this;
	}

	public TSelf WithAction(Func<IServiceProvider, CancellationToken, Task> action) {
		SetAction(action);
		return (TSelf)(object)this;
	}

	public TSelf WithScreen<TScreen>() where TScreen : IApplicationScreen => WithScreen(typeof(TScreen));

	public TSelf AsSingleInstance() => WithActivationMode(ScreenActivationMode.SingleInstance);

	public TSelf AsMultiInstance() => WithActivationMode(ScreenActivationMode.MultiInstance);

	public TSelf AsPermanentSingleton() => WithActivationMode(ScreenActivationMode.PermanentSingleton);
}
