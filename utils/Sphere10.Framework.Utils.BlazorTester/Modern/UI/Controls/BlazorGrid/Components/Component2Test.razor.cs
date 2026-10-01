// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid.Classes;

namespace Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls.BlazorGrid.Components;

partial class Component2Test : IGridComponent<bool> {
	[Parameter] public bool Checked { get; set; }

	public Component2Test() {
	}

	public Component2Test(bool checkedValue) {
		Checked = checkedValue;
	}

	public void Render(bool item, RenderTreeBuilder builder) {
		builder.OpenComponent<Component2Test>(0);
		builder.AddAttribute(1, "Checked", Checked);
		builder.CloseComponent();
	}
}
