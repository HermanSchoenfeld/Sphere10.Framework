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

public partial class ComponentForTestClass3 : IGridComponent<TestClass3> {
	public ComponentForTestClass3() {
	}
	[Parameter] public TestClass3 Data { get; set; }

	public void Render(TestClass3 item, RenderTreeBuilder builder) {
		builder.OpenComponent<ComponentForTestClass3>(0);
		builder.AddAttribute(1, "Data", item);
		builder.CloseComponent();
	}
}
