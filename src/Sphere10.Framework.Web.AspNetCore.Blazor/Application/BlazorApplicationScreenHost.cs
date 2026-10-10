// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Blazor session factory over the shared host. The renderer creates and disposes components.</summary>
public class BlazorApplicationScreenHost : BlazorApplicationScreenHostBase {
	public BlazorApplicationScreenHost(IServiceProvider services)
		: base(services) {
	}

	/// <summary>Constructs a host from startup block definitions; normal applications resolve the scoped host through AddSphere10Blazor.</summary>
	public BlazorApplicationScreenHost(IEnumerable<IBlazorApplicationBlock> blocks, IServiceProvider services)
		: base(blocks, services) {
	}

	protected override BlazorApplicationScreenSession CreateScreen(IBlazorApplicationBlock block, ApplicationScreenDefinition definition) {
		var effective = ResolveDefinition(definition);
		var source = GetScreenMenuItem(block, definition);
		var screen = new BlazorApplicationScreenSession(block, source, effective, CurrentActivation);
		TrackKeyedScreen(screen);
		return screen;
	}
}
