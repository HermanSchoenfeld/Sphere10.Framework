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

/// <summary>The circuit-local application aggregate over the existing retained-screen host.</summary>
public interface IApplication : SharedUI.IApplication<IBlazorPlugin, IApplicationScreenHost>, IApplicationCommandProvider<IApplicationMenu, IApplicationMenuItem> {
	new IApplicationBlock[] Blocks { get; }

	#region Base Interface Bridges
	SharedUI.IApplicationBlock[] SharedUI.IApplication.Blocks => Blocks;
	#endregion
}
