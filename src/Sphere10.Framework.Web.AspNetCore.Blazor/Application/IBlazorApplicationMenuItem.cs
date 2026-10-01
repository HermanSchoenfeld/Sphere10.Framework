// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Blazor menu metadata and registration-time notification handlers.</summary>
/// <remarks>Shared registrations must not capture scoped services or mutable per-user state in handlers.</remarks>
public interface IBlazorApplicationMenuItem : IApplicationMenuItem {
	event EventHandlerEx Hover;

	event EventHandlerEx Select;

	string Icon { get; }

}
