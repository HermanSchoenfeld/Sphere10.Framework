// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A navigation command referencing a screen owned by its block.</summary>
public class BlazorScreenMenuItem : ApplicationMenuItemBase, IScreenMenuItem {
	private SharedUI.ApplicationScreenDefinition _screenDefinition;
	private ApplicationScreenRegistration _registration;

	public Type ScreenType {
		get => _screenDefinition?.ScreenType ?? Registration?.ScreenType;
		init {
			Tools.UI.ValidateScreenType(value, typeof(IApplicationScreen));
			_registration = ApplicationScreenRegistration.ForType(value);
		}
	}

	public string ScreenTitle => _screenDefinition?.Title;

	public ScreenActivationMode ActivationMode => _screenDefinition?.ActivationMode ?? ScreenActivationMode.MultiInstance;

	public ScreenKind ScreenKind => _screenDefinition?.ScreenKind ?? ScreenKind.Normal;

	public ApplicationScreenRegistration Registration {
		get => _registration ?? _screenDefinition?.Registration;
		init => _registration = value;
	}

	public SharedUI.ApplicationScreenDefinition ScreenDefinition {
		get => _screenDefinition;
		init {
			Guard.ArgumentNotNull(value, nameof(value));
			Tools.UI.ValidateScreenType(value.ScreenType, typeof(IApplicationScreen));
			_screenDefinition = value;
		}
	}

	protected override string DefaultId {
		get {
			return Registration == null ? base.DefaultId : Registration.IsTypeReference ? Registration.ScreenType.Name : Registration.Token.ToString("N");
		}
	}

	/// <summary>Binds the command to its own graph's canonical definition and updates reverse membership.</summary>
	public void ResolveScreenDefinition(SharedUI.ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(definition, nameof(definition));
		Tools.UI.ValidateScreenType(definition.ScreenType, typeof(IApplicationScreen));
		Guard.Argument(Registration == null || Registration.ScreenType == definition.ScreenType
			&& (Registration.IsTypeReference || Registration.Token == definition.Registration.Token), nameof(definition), "The definition must match this command's registration reference.");
		if (!ReferenceEquals(_screenDefinition, definition)) {
			_screenDefinition?.RemoveActivator(this);
			_screenDefinition = definition;
		}
		SetDefaultTitle(definition.Title);
		if (Parent != null)
			definition.AddActivator(this);
	}

	public static BlazorScreenMenuItem For<TScreen>(string icon, string title)
		where TScreen : IApplicationScreen {
		return new BlazorScreenMenuItem { Id = typeof(TScreen).Name, Icon = icon, Title = title, ScreenType = typeof(TScreen) };
	}
}
