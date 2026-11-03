// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>Identifies a registration in each independently built block graph without retaining a screen or definition.</summary>
public class ApplicationScreenRegistration {
	/// <summary>Creates an exact configuration identity for one screen type.</summary>
	public ApplicationScreenRegistration(Type screenType)
		: this(screenType, Guid.NewGuid()) {
	}

	// Type-only references have no exact token and are resolved by their owning command or host.
	private protected ApplicationScreenRegistration(Type screenType, Guid token) {
		ApplicationHelper.ValidateScreenType(screenType);
		ScreenType = screenType;
		Token = token;
	}

	/// <summary>The declared screen type, without an operational screen instance.</summary>
	public Type ScreenType { get; }

	/// <summary>The configuration identity shared by copies of this registration.</summary>
	public Guid Token { get; }

	/// <summary>Indicates a type shortcut rather than an exact registration identity.</summary>
	public bool IsTypeReference => Token == Guid.Empty;

	/// <summary>Creates a type request; navigation uses declaration order and programmatic lookup requires an unambiguous match.</summary>
	public static ApplicationScreenRegistration ForType(Type screenType) {
		return new ApplicationScreenRegistration(screenType, Guid.Empty);
	}
}

/// <summary>A typed registration identity; the host resolves it within its own block graph.</summary>
public sealed class ApplicationScreenRegistration<TScreen> : ApplicationScreenRegistration
	where TScreen : IApplicationScreen {
	/// <summary>Creates an exact typed configuration identity.</summary>
	public ApplicationScreenRegistration()
		: base(typeof(TScreen)) {
	}
}
