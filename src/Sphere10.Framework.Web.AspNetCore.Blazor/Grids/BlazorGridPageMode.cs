// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Chooses manually sized pages, pages fitted to the viewport, or one complete result page.</summary>
public enum BlazorGridPageMode {
	Paged,
	Auto,
	Single
}
