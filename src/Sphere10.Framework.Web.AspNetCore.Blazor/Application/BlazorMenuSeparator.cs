// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
using System;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>A separator; command merging removes leading, trailing and repeated boundaries.</summary>
public sealed class BlazorMenuSeparator : BlazorApplicationMenuItem, IApplicationMenuSeparator {
	public BlazorMenuSeparator() {
		Id = Guid.NewGuid().ToString("N");
		Title = "Separator";
	}
}