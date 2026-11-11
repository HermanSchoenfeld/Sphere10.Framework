// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Configures command presentation and a screen reference or runtime action without registering screens.</summary>
public abstract class ApplicationMenuItemBuilderBase {
	protected string Text { get; private set; }

	protected ApplicationScreenDefinition ScreenDefinition { get; private set; }

	protected ApplicationScreenRegistration ScreenRegistration { get; private set; }

	protected Type ScreenType => ScreenRegistration?.ScreenType;

	protected Action Action { get; private set; }

	protected Action<IUserInterfaceServices> UIAction { get; private set; }

	protected Func<IServiceProvider, CancellationToken, Task> AsyncAction { get; private set; }

	protected void SetText(string text) {
		ValidateText(text);
		Text = text;
	}

	protected void SetScreenType(Type screenType) {
		ValidateScreenType(screenType);
		SetScreenRegistration(ApplicationScreenRegistration.ForType(screenType));
	}

	protected void SetScreenDefinition(ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		SetScreenRegistration(definition.Registration);
		ScreenDefinition = definition;
	}

	protected void SetScreenRegistration(ApplicationScreenRegistration registration) {
		Guard.ArgumentNotNull(registration, nameof(registration));
		ValidateScreenType(registration.ScreenType);
		Guard.Ensure(Action == null && UIAction == null && AsyncAction == null, "A menu item cannot contain both a screen and an action.");
		ScreenRegistration = registration;
		ScreenDefinition = null;
	}

	protected void SetAction(Action action) {
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(ScreenRegistration == null, "A menu item cannot contain both a screen and an action.");
		Action = action;
		UIAction = null;
		AsyncAction = null;
	}

	protected void SetAction(Action<IUserInterfaceServices> action) {
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(ScreenRegistration == null, "A menu item cannot contain both a screen and an action.");
		UIAction = action;
		Action = null;
		AsyncAction = null;
	}

	protected void SetAction(Func<IServiceProvider, CancellationToken, Task> action) {
		Guard.ArgumentNotNull(action, nameof(action));
		Guard.Ensure(ScreenRegistration == null, "A menu item cannot contain both a screen and an action.");
		AsyncAction = action;
		Action = null;
		UIAction = null;
	}

	protected void ValidateItem() {
		Guard.Ensure(ScreenRegistration != null || Action != null || UIAction != null || AsyncAction != null, "A registered screen reference or action is required.");
		Guard.Ensure(ScreenRegistration != null || !string.IsNullOrWhiteSpace(Text), "Menu item text is required.");
	}

	protected virtual void ValidateText(string text) => Guard.ArgumentNotNull(text, nameof(text));

	protected virtual void ValidateScreenType(Type screenType) => ApplicationHelper.ValidateScreenType(screenType);
}

/// <summary>Shared fluent command presentation, registered-screen references and runtime action callbacks.</summary>
public abstract class ApplicationMenuItemBuilderBase<TSelf> : ApplicationMenuItemBuilderBase
	where TSelf : class {
	public TSelf WithText(string text) {
		SetText(text);
		return (TSelf)(object)this;
	}

	public TSelf WithScreen(Type screenType) {
		SetScreenType(screenType);
		return (TSelf)(object)this;
	}

	public TSelf WithScreen(ApplicationScreenDefinition definition) {
		SetScreenDefinition(definition);
		return (TSelf)(object)this;
	}

	public TSelf WithScreen(ApplicationScreenRegistration registration) {
		SetScreenRegistration(registration);
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

	public TSelf WithScreen<TScreen>()
		where TScreen : IApplicationScreen {
		return WithScreen(typeof(TScreen));
	}
}
