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

/// <summary>Provides the owning circuit and registration when preparing a new renderer-owned screen.</summary>
public sealed class BlazorApplicationScreenActivationContext {
	public BlazorApplicationScreenActivationContext(IServiceProvider services, IBlazorApplicationBlock block, ApplicationScreenDefinition definition) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(block, nameof(block));
		Guard.ArgumentNotNull(definition, nameof(definition));
		Services = services;
		Block = block;
		Definition = definition;
	}

	public IServiceProvider Services { get; }

	public IBlazorApplicationBlock Block { get; }

	public ApplicationScreenDefinition Definition { get; }
}
