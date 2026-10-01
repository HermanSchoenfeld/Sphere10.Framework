// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application.UI;

/// <summary>Shared help metadata and asynchronous lifecycle contract for application screens.</summary>
/// <remarks>UI adapters own rendering, thread affinity, attachment and disposal.</remarks>
public interface IApplicationScreen : IHelpableObject {
	bool HasUnsavedChanges => false;

	/// <summary>Allows or vetoes hiding, closing, or navigating away; may be called for a hidden screen.</summary>
	Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(true);
	}

	/// <summary>Runs on first active attachment and whenever this retained screen is selected again.</summary>
	Task OnActivatedAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.CompletedTask;
	}

	/// <summary>Runs before the active screen is hidden or removed, after its guard permits the operation.</summary>
	Task OnDeactivatedAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.CompletedTask;
	}

	HelpType IHelpableObject.Type => HelpType.None;

	string IHelpableObject.FileName => null;

	string IHelpableObject.Url => null;

	int? IHelpableObject.PageNumber => null;

	int? IHelpableObject.HelpTopicID => null;

	int? IHelpableObject.HelpTopicAlias => null;
}
