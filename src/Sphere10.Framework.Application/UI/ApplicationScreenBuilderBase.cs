// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>Configures registration-owned metadata while leaving platform activation to the corresponding adapter.</summary>
public abstract class ApplicationScreenBuilderBase<TSelf> : IApplicationUIBuilder<ApplicationScreenDefinition>
	where TSelf : class {
	/// <summary>Starts registration configuration without constructing its screen.</summary>
	protected ApplicationScreenBuilderBase(Type screenType = null, ApplicationScreenRegistration registration = null) {
		if (screenType != null)
			SetScreenType(screenType, registration);
	}

	protected Type ScreenType { get; private set; }

	protected string Title { get; private set; }

	protected ScreenActivationMode? ActivationMode { get; private set; }

	protected ScreenKind ScreenKind { get; private set; }

	protected string StableKey { get; private set; }

	protected ApplicationScreenRegistration Registration { get; private set; }

	/// <summary>The typed settings object edited by this registration's Options page.</summary>
	protected SettingsObject Settings { get; private set; }

	/// <summary>Validates and captures the declared registration metadata.</summary>
	public virtual ApplicationScreenDefinition Build() {
		Guard.Ensure(ScreenType != null, "A screen type is required.");
		ApplicationHelper.ValidateScreenPolicy(ActivationMode, ScreenKind);
		return CreateDefinition();
	}

	/// <summary>Supplies the type for a non-generic screen registration.</summary>
	public TSelf WithScreen(Type screenType) {
		SetScreenType(screenType, null);
		return (TSelf)(object)this;
	}

	/// <summary>Supplies a concrete screen type without activating it.</summary>
	public TSelf WithScreen<TScreen>()
		where TScreen : IApplicationScreen {
		return WithScreen(typeof(TScreen));
	}

	/// <summary>Sets the screen title independently of command captions.</summary>
	public TSelf WithTitle(string title) {
		Guard.ArgumentNotNullOrEmpty(title, nameof(title));
		Title = title;
		return (TSelf)(object)this;
	}

	/// <summary>Overrides the lifetime policy for this registration alone.</summary>
	public TSelf WithActivationMode(ScreenActivationMode activationMode) {
		ApplicationHelper.ValidateActivationMode(activationMode);
		ActivationMode = activationMode;
		return (TSelf)(object)this;
	}

	/// <summary>Sets this registration's normal or empty-workspace role.</summary>
	public TSelf WithScreenKind(ScreenKind screenKind) {
		ApplicationHelper.ValidateScreenKind(screenKind);
		ScreenKind = screenKind;
		return (TSelf)(object)this;
	}

	/// <summary>Sets a persistent key for startup choices and settings records.</summary>
	public TSelf WithStableKey(string stableKey) {
		Guard.ArgumentNotNullOrEmpty(stableKey, nameof(stableKey));
		StableKey = stableKey;
		return (TSelf)(object)this;
	}

	/// <summary>Declares typed settings without constructing an operational screen.</summary>
	public TSelf UseSettings<TSettings>()
		where TSettings : SettingsObject, new() {
		return UseSettings(new TSettings());
	}

	/// <summary>Uses the supplied settings instance for this registration.</summary>
	public TSelf UseSettings(SettingsObject settings) {
		Guard.ArgumentNotNull(settings, nameof(settings));
		Settings = settings;
		return (TSelf)(object)this;
	}

	/// <summary>Retains one instance for this registration in its owning block.</summary>
	public TSelf AsSingleInstance() => WithActivationMode(ScreenActivationMode.SingleInstance);

	/// <summary>Creates independent instances when this registration is opened.</summary>
	public TSelf AsMultiInstance() => WithActivationMode(ScreenActivationMode.MultiInstance);

	/// <summary>Creates a retained instance during host initialization.</summary>
	public TSelf AsPermanentSingleton() => WithActivationMode(ScreenActivationMode.PermanentSingleton);

	/// <summary>Copies only declared values; building never activates an operational screen.</summary>
	protected virtual ApplicationScreenDefinition CreateDefinition()
		=> new ApplicationScreenDefinition(ScreenType, Title, ActivationMode, ScreenKind, Registration, StableKey, Settings);

	// Platform builders additionally validate their supported screen contract.
	protected virtual void ValidateScreenType(Type screenType) => ApplicationHelper.ValidateScreenType(screenType);

	// Keep one token and one immutable type throughout this child's configuration.
	private void SetScreenType(Type screenType, ApplicationScreenRegistration registration) {
		ValidateScreenType(screenType);
		Guard.Argument(ScreenType == null || ScreenType == screenType, nameof(screenType), "A screen registration cannot change its type.");
		ScreenType = screenType;
		Registration ??= registration ?? new ApplicationScreenRegistration(screenType);
	}
}
