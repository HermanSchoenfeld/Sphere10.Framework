// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A renderer-created Blazor component implementing the portable application-screen lifecycle.</summary>
/// <remarks>
/// Derive from BlazorApplicationScreen for automatic attachment. Guard and lifecycle callbacks must not await another
/// transition on their host. Resolve per-user dependencies through the current circuit's service provider.
/// </remarks>
public interface IBlazorApplicationScreen : IApplicationScreen<IBlazorApplicationMenu, IBlazorApplicationMenuItem>, IComponent {
	new IBlazorApplicationScreenHost Host { get; set; }

	new IBlazorApplicationBlock Block { get; set; }

	/// <summary>Whether this screen can be reopened without its current session state.</summary>
	bool CanRestoreAtStartup => true;

	#region Base Interface Bridges

	IApplicationScreenHost IApplicationScreen.Host {
		get => Host;
		set => Host = value == null ? null : Guard.ArgumentCast<IBlazorApplicationScreenHost>(value, nameof(value));
	}

	IApplicationBlock IApplicationScreen.Block {
		get => Block;
		set => Block = value == null ? null : Guard.ArgumentCast<IBlazorApplicationBlock>(value, nameof(value));
	}

	#endregion
}
