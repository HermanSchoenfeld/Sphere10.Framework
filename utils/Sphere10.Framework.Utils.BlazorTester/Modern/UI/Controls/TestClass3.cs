// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: David Price
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid.Classes;
using Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls.BlazorGrid.Components;

namespace Sphere10.Framework.Utils.BlazorTester.Modern.UI.Controls;

public class TestClass3 : IColumnDefinition {
	public int HashCode { get; set; }

	public TestClass3() {
	}

	public TestClass3(string text) {
		HashCode = text.GetHashCode();
	}
	public Type GetComponentType() {
		return typeof(ComponentForTestClass3);
	}
}
