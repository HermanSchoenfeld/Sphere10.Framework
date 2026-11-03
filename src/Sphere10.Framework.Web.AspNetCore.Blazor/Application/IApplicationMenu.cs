// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public interface IBlazorApplicationMenu : IApplicationMenu<IBlazorApplicationMenuItem> {
	new IBlazorApplicationBlock Parent { get; set; }

	string Id => Text;

	string Icon { get; }

	#region Base Interface Bridges

	IApplicationBlock IApplicationMenu.Parent {
		get => Parent;
		set => Parent = value == null ? null : Guard.ArgumentCast<IBlazorApplicationBlock>(value, nameof(value));
	}

	#endregion
}
