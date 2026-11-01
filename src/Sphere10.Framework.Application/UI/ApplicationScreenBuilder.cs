// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

/// <summary>Declares UI-independent screen metadata with an explicitly supplied screen type.</summary>
public class ApplicationScreenBuilder : ApplicationScreenBuilderBase<ApplicationScreenBuilder> {
}

/// <summary>Declares a typed UI-independent screen registration.</summary>
public class ApplicationScreenBuilder<TScreen> : ApplicationScreenBuilderBase<ApplicationScreenBuilder<TScreen>>
	where TScreen : IApplicationScreen {
	/// <summary>Starts configuration with an exact typed registration identity.</summary>
	public ApplicationScreenBuilder()
		: base(typeof(TScreen), new ApplicationScreenRegistration<TScreen>()) {
	}
}
