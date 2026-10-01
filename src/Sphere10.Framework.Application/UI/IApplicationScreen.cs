// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

/// <summary>A Blazor application screen with shared help metadata and asynchronous lifecycle hooks.</summary>
/// <remarks>
/// Derive from ApplicationScreen for automatic attachment and detachment. Implementations used directly must
/// attach their renderer-created instance to IApplicationScreenHost. Guard and lifecycle callbacks execute inside
/// a serialized transition and must not await another host transition. Resolve scoped dependencies through DI.
/// </remarks>
public interface IApplicationScreen : IHelpableObject {
	/// <summary>Used for external-navigation confirmation; notify the host when this value changes.</summary>
	bool HasUnsavedChanges => false;

	/// <summary>Allows or vetoes hiding, closing, or navigating away; may be called for a hidden screen.</summary>
	Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

	/// <summary>Runs on first active attachment and whenever this retained screen is selected again.</summary>
	Task OnActivatedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

	/// <summary>Runs before the active screen is hidden or removed, after its guard permits the operation.</summary>
	Task OnDeactivatedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

	HelpType IHelpableObject.Type => HelpType.None;

	string IHelpableObject.FileName => null;

	string IHelpableObject.Url => null;

	int? IHelpableObject.PageNumber => null;

	int? IHelpableObject.HelpTopicID => null;

	int? IHelpableObject.HelpTopicAlias => null;
}
