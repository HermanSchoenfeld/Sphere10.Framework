// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Edits one actual settings object; the containing dialog retains validation, persistence and acceptance ownership.</summary>
public interface IBlazorApplicationSettingsEditor {
	/// <summary>Expose this as a component parameter and edit only this actual settings value.</summary>
	object Value { get; set; }

	/// <summary>Expose this as a component parameter and await it after a draft value changes.</summary>
	EventCallback Changed { get; set; }

	/// <summary>Validates pending editor input before the dialog invokes the declared settings validation.</summary>
	Task<Result> ValidateAsync(CancellationToken cancellationToken = default);
}
