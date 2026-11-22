// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Blazor menu metadata and registration-time notification handlers.</summary>
/// <remarks>Shared registrations must not capture scoped services or mutable per-user state in handlers.</remarks>
public interface IApplicationMenuItem : SharedUI.IApplicationMenuItem {
	new IApplicationMenu Parent { get; set; }

	string Id => Title;

	event EventHandlerEx Hover;

	event EventHandlerEx Select;

	string Icon { get; }

	/// <summary>Presentation only: hidden commands remain available for programmatic activation.</summary>
	bool IsVisible => true;

	/// <summary>Whether this command can currently execute.</summary>
	bool IsEnabled => true;

	/// <summary>Shows the caption beside the icon on a toolbar. Toolbars otherwise show an icon-bearing command as its icon, with the caption as its tooltip and accessible name.</summary>
	/// <remarks>Menus always show the icon and caption. A command without an icon always shows its caption.</remarks>
	bool ShowToolBarText => false;

	/// <summary>
	/// The caption a toolbar shows beside the icon, which defaults to <see cref="SharedUI.IApplicationMenuItem.Title"/>. A command whose title
	/// also names what blocks it, such as "Lock Product (Product: Locked)", shows its plain name and keeps the title as its tooltip.
	/// </summary>
	string ToolBarCaption => Title;

	#region Base Interface Bridges

	SharedUI.IApplicationMenu SharedUI.IApplicationMenuItem.Parent {
		get => Parent;
		set => Parent = value == null ? null : Guard.ArgumentCast<IApplicationMenu>(value, nameof(value));
	}

	#endregion
}
