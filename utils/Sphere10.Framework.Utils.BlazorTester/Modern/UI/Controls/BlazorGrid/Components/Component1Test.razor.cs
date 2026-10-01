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

partial class Component1Test : IGridComponent<string> {
	[Parameter] public string ListId { get; set; }

	public Component1Test() {
	}

	public Component1Test(string listId) {
		ListId = listId;
	}

	public void Render(string item, RenderTreeBuilder builder) {
		builder.OpenComponent<Component1Test>(0);
		builder.AddAttribute(1, "ListId", ListId);
		builder.CloseComponent();
	}
}
