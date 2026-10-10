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
		Guard.Argument(instanceKey == null || !string.IsNullOrWhiteSpace(instanceKey), nameof(instanceKey), "A screen instance key cannot be blank.");
		Parameters = new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(parameters ?? new Dictionary<string, object>(), System.StringComparer.OrdinalIgnoreCase));
		InstanceKey = instanceKey;
	}

	public IReadOnlyDictionary<string, object> Parameters { get; }

	public string InstanceKey { get; }
}
