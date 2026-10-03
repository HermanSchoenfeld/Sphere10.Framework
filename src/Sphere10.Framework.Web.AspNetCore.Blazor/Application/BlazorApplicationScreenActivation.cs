// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Immutable activation metadata for a registered screen; component parameter values must themselves be immutable.</summary>
public sealed class BlazorApplicationScreenActivation {
	public BlazorApplicationScreenActivation(IDictionary<string, object> parameters = null, string instanceKey = null) {
		Guard.Argument(instanceKey == null || !string.IsNullOrWhiteSpace(instanceKey), nameof(instanceKey), "An instance key must be nonblank when supplied.");
		Parameters = new ReadOnlyDictionary<string, object>(parameters == null ? new Dictionary<string, object>() : new Dictionary<string, object>(parameters));
		InstanceKey = instanceKey;
	}

	/// <summary>Parameter overrides copied at construction and applied only when a new session is created.</summary>
	public IReadOnlyDictionary<string, object> Parameters { get; }

	/// <summary>Optional reuse key, scoped to the registered block, screen menu item and component type. Singleton policies take precedence.</summary>
	public string InstanceKey { get; }
}
