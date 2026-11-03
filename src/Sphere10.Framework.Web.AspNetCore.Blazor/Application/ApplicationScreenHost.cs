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
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Blazor session factory over the shared host. The renderer creates and disposes components.</summary>
public class ApplicationScreenHost : ApplicationScreenHostBase {
	public ApplicationScreenHost(IServiceProvider services)
		: base(services) {
	}

	/// <summary>Constructs a host from startup block definitions; normal applications resolve the scoped host through AddSphere10Blazor.</summary>
	public ApplicationScreenHost(IEnumerable<IApplicationBlock> blocks, IServiceProvider services)
		: base(blocks, services) {
	}

	protected override ApplicationScreenSession CreateScreen(IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition) {
		var source = GetScreenMenuItem(block, definition);
		var prepared = (definition as ApplicationScreenDefinition)?.Activator?.Invoke(new ApplicationScreenActivationContext(Services, block, definition));
		Guard.Ensure(definition is not ApplicationScreenDefinition { Activator: not null } || prepared != null,
			"The screen activator must return component activation parameters.");
		Guard.Ensure(prepared?.InstanceKey == null, "Supply instance reuse keys when opening a registration; its activator prepares component parameters.");
		var screen = new ApplicationScreenSession(block, source, definition, CurrentActivation, prepared);
		return screen;
	}
}
