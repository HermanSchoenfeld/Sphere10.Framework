// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Application.UI;

/// <summary>Resolved UI-independent registration metadata, retaining its owning block and stable menu identity.</summary>
public sealed class ApplicationScreenDefinition {
	public const string DefaultMenuItemId = "__default";

	public ApplicationScreenDefinition(IApplicationBlock block, Type screenType, string menuItemId, string title,
		ScreenActivationMode? activationMode, ScreenKind screenKind, bool isDefault
	) {
		Guard.ArgumentNotNull(block, nameof(block));
		Tools.UI.ValidateScreenType(screenType);
		Guard.ArgumentNotNullOrEmpty(menuItemId, nameof(menuItemId));
		Tools.UI.ValidateScreenPolicy(activationMode, screenKind);
		Block = block;
		ScreenType = screenType;
		MenuItemId = menuItemId;
		Title = title ?? screenType.Name;
		ActivationMode = activationMode;
		ScreenKind = screenKind;
		IsDefault = isDefault;
	}

	public IApplicationBlock Block { get; }

	public Type ScreenType { get; }

	public string MenuItemId { get; }

	public string Title { get; }

	public ScreenActivationMode? ActivationMode { get; }

	public ScreenKind ScreenKind { get; }

	public bool IsDefault { get; }
}
