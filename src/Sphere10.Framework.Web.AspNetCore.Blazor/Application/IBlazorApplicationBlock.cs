// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

public interface IBlazorApplicationBlock : IApplicationBlock<IBlazorApplicationMenu, IBlazorApplicationMenuItem> {
	new IBlazorApplication Parent { get; set; }

	new IBlazorPlugin Source { get; set; }

	string Id => Title;

	string Title { get; }

	string IApplicationBlock.Name => Title;

	string IconUrl { get; }

	string Tooltip { get; }

	Type DefaultScreen => null;

	string DefaultScreenTitle => null;

	ScreenActivationMode? DefaultScreenActivationMode => null;

	ScreenKind DefaultScreenKind => ScreenKind.Normal;

	#region Base Interface Bridges

	IApplication IApplicationBlock.Parent {
		get => Parent;
		set => Parent = value == null ? null : Guard.ArgumentCast<IBlazorApplication>(value, nameof(value));
	}

	IApplicationPlugin IApplicationBlock.Source {
		get => Source;
		set => Source = value == null ? null : Guard.ArgumentCast<IBlazorPlugin>(value, nameof(value));
	}

	#endregion
}
