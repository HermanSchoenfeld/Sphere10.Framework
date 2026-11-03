// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Finishes the first acquired model/UI update using its requested completion behavior.</summary>
public readonly struct UpdateScope : IScope {
	private readonly FinishedUpdateBehaviour _behaviour;
	private readonly IUpdatable _updateable;

	/// <summary>Begins an update only when another scope does not already own completion.</summary>
	public UpdateScope(IUpdatable updateable, FinishedUpdateBehaviour behaviour) {
		Guard.ArgumentNotNull(updateable, nameof(updateable));
		if (!updateable.Updating) {
			_updateable = updateable;
			_updateable.BeginUpdate();
			_behaviour = behaviour;
		}
	}

	/// <summary>Queues native completion to its existing dispatcher and awaits the result.</summary>
	public async ValueTask DisposeAsync() {
		if (_updateable == null)
			return;
		if (_updateable is Control control)
			await control.InvokeAsync(Dispose).ConfigureAwait(false);
		else
			await Task.Run(Dispose).ConfigureAwait(false);
	}

	/// <summary>Completes the owned update inline on its calling thread.</summary>
	public void Dispose() {
		_updateable?.FinishUpdate(_behaviour);
	}
}
