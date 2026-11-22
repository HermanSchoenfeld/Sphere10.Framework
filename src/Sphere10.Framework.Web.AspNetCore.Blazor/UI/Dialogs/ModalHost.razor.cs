// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs;

/// <summary>
/// Shows one <see cref="ModalComponent"/> at a time for <see cref="ModalService"/> and returns its <see cref="ModalResult"/>.
/// </summary>
/// <remarks>
/// The host cascades itself to the modal it shows, so that modal's <see cref="ModalTemplate"/> can ask
/// <see cref="ConfirmCloseAsync"/> before a click outside it or the Escape key closes it.
/// </remarks>
public partial class ModalHost {
	// Created with the first close confirmation and completed once the host that shows it has rendered.
	private TaskCompletionSource<ModalHost> _confirmationHost;
	private bool _confirmingClose;

	protected override ModalResult CanceledResult => ModalResult.Cancel;

	private ModalHost ConfirmationHost { set => _confirmationHost.TrySetResult(value); }

	public Task<ModalResult> ShowAsync<T>(ParameterView parameters, ModalOptions options = null)
		where T : ModalComponent
		=> ShowAsync<T>(parameters, options, CancellationToken.None);

	public Task<ModalResult> ShowAsync<T>(ParameterView parameters, ModalOptions options, CancellationToken cancellationToken)
		where T : ModalComponent {
		var values = new Dictionary<string, object> {
			[nameof(ModalComponent.Options)] = options ?? new ModalOptions()
		};
		foreach (var parameter in parameters)
			values[parameter.Name] = parameter.Value;
		return ShowCoreAsync<T>(values, cancellationToken);
	}

	/// <summary>
	/// Asks "Are you sure you want to close?" above the modal being shown, which stays rendered with its state.
	/// </summary>
	/// <param name="cancellationToken">Withdraws the question, which then answers No.</param>
	/// <returns>Whether the user chose Yes. A question asked while another is open answers No.</returns>
	public async Task<bool> ConfirmCloseAsync(CancellationToken cancellationToken = default) {
		if (_confirmingClose)
			return false;
		_confirmingClose = true;
		using var scope = Tools.Scope.ExecuteOnDispose(() => _confirmingClose = false);

		// The question has its own host, so the modal beneath keeps its component, focus and entered data.
		if (_confirmationHost == null) {
			_confirmationHost = new TaskCompletionSource<ModalHost>(TaskCreationOptions.RunContinuationsAsynchronously);
			await InvokeAsync(StateHasChanged);
		}
		var parameters = new Dictionary<string, object> {
			[nameof(Confirm.Title)] = "Close",
			[nameof(Confirm.Message)] = "Are you sure you want to close?",
			[nameof(Confirm.ConfirmMessageText)] = "Yes",
			[nameof(Confirm.CancelMessageText)] = "No"
		};
		try {
			var host = await _confirmationHost.Task.WaitAsync(cancellationToken);
			var result = await host.ShowAsync<Confirm>(ParameterView.FromDictionary(parameters), new ModalOptions { Size = ModalSize.Small }, cancellationToken);
			return result.ResultType == ModalResultType.Ok;
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			// The modal that asked was removed before the question could be shown.
			return false;
		}
	}

	protected override Task WaitUntilRenderedAsync(ModalComponent component) => component.ModalRendered;

	protected override Task<ModalResult> GetResultAsync(ModalComponent component) => component.ShowAsync();

	protected override Task<bool> RequestCloseAsync(ModalComponent component) => component.RequestCloseAsync();
}
