// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Application.UI;

/// <summary>A command referencing a screen definition registered by its owning block.</summary>
public interface IScreenMenuItem : IApplicationMenuItem {
	ApplicationScreenDefinition ScreenDefinition { get; }

	/// <summary>The exact registration or unambiguous type request to resolve in the owning block.</summary>
	ApplicationScreenRegistration Registration => ScreenDefinition?.Registration;

	/// <summary>Binds a command to its graph's canonical definition without registering another screen.</summary>
	void ResolveScreenDefinition(ApplicationScreenDefinition definition)
		=> Guard.Argument(ReferenceEquals(ScreenDefinition, definition), nameof(definition), "This command requires its canonical registered definition.");
}
