// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

/// <summary>Registration metadata and notifications for an application menu item.</summary>
/// <remarks>
/// Catalog snapshots preserve the handlers subscribed during registration. Because the catalog is shared,
/// those handlers must not capture circuit-scoped services or mutable per-user state. Use an ActionMenuItem
/// asynchronous callback to resolve services from the current circuit's provider instead.
/// </remarks>
public interface IApplicationMenuItem {
	/// <summary>Raised when the menu receives pointer hover or keyboard focus.</summary>
	event EventHandlerEx Hover;

	/// <summary>Raised once after a screen menu selection or action succeeds, including reselecting a retained screen.</summary>
	event EventHandlerEx Select;

	string Id => Title;

	string Icon { get; }

	string Title { get; }
}
