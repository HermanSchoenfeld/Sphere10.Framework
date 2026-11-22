// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Presents an available update to the user. While started it observes the application's <see cref="ProductUpdateChecker"/> and,
/// for each update the checker announces, shows <see cref="ProductUpdateDialog"/> on the UI thread. The notice installs the update the
/// user accepts through the application's <see cref="ProductUpdaterBase"/>. When the updater has started an installer that must replace
/// the application's files, the presenter asks the application to exit through <see cref="IUserInterfaceServices.Exit"/>; a channel that
/// restarts the application itself, such as the Windows Store, needs no request.
/// </summary>
/// <remarks>
/// The checker's notification only posts the notice to the UI thread and returns, so the checker never waits for the user. One
/// notice is shown at a time. An update that could not be shown, because a notice was open or the application was exiting, and an
/// update whose presentation failed are handed back to the checker with <see cref="ProductUpdateChecker.OfferAgain"/>, so its next
/// check announces them again. A failure is logged and reported to the user, and the application keeps running. Call
/// <see cref="ShowAsync"/> from the UI thread to present an update found by other means, such as a Check For Updates command that
/// awaited <see cref="ProductUpdateChecker.CheckAsync"/>.
/// </remarks>
public class ProductUpdatePresenter {
	private readonly ProductUpdateChecker _checker;
	private bool _started;
	private bool _presenting;

	public ProductUpdatePresenter(IServiceProvider services, ProductUpdateChecker checker, ProductUpdaterBase updater) {
		Guard.ArgumentNotNull(services, nameof(services));
		Guard.ArgumentNotNull(checker, nameof(checker));
		Guard.ArgumentNotNull(updater, nameof(updater));
		Services = services;
		_checker = checker;
		Updater = updater;
	}

	// The main form registers the user interface services, so they are resolved when used rather than when this is constructed.
	protected IServiceProvider Services { get; }

	/// <summary>The updater that installs the update the user accepts, which is the one the checker asks.</summary>
	protected ProductUpdaterBase Updater { get; }

	protected ILogger Logger => Services.GetService<ILogger>() ?? SystemLog.Logger;

	/// <summary>Begins presenting the updates the checker announces. A second call does nothing.</summary>
	public void Start() {
		if (_started)
			return;
		_started = true;
		_checker.UpdateAvailable += CheckerUpdateAvailable;
	}

	/// <summary>Stops presenting the checker's updates. A notice that is already open stays with its window.</summary>
	public void Stop() {
		if (!_started)
			return;
		_started = false;
		_checker.UpdateAvailable -= CheckerUpdateAvailable;
	}

	/// <summary>
	/// Shows the notice for an update. When the user accepts it the notice installs it through <see cref="Updater"/>, and the presenter
	/// asks the application to exit when the updater requires it. It answers <see cref="ProductUpdateNoticeResult.NotShown"/> without
	/// showing anything while another notice is open or the application is exiting.
	/// </summary>
	public async Task<ProductUpdateNoticeResult> ShowAsync(ProductUpdate update) {
		Guard.ArgumentNotNull(update, nameof(update));
		var userInterface = Services.GetRequiredService<IUserInterfaceServices>();
		if (_presenting || userInterface.ApplicationExiting)
			return ProductUpdateNoticeResult.NotShown;
		_presenting = true;
		using (Tools.Scope.ExecuteOnDispose(() => _presenting = false)) {
			var installed = await ShowNoticeAsync(userInterface.PrimaryUIController as Form, update);
			if (installed == null)
				return ProductUpdateNoticeResult.Declined;
			if (installed == ProductUpdateInstallResult.ExitRequired)
				RequestExit(userInterface);
			return ProductUpdateNoticeResult.Installing;
		}
	}

	/// <summary>
	/// Shows the notice, which installs the update the user accepts, and completes with what the installation did, or null when the
	/// user declined.
	/// </summary>
	protected virtual Task<ProductUpdateInstallResult?> ShowNoticeAsync(Form owner, ProductUpdate update)
		=> ProductUpdateDialog.ShowAsync(owner, Updater, update, Services.GetRequiredService<IProductInformationProvider>().ProductInformation);

	/// <summary>
	/// Asks the application to exit so the started installer can replace its files. The request takes the ordinary exit path, with its
	/// confirmation and guards, so unsaved work can still keep the application open.
	/// </summary>
	protected virtual void RequestExit(IUserInterfaceServices userInterface) => userInterface.Exit();

	// Runs on the checker's worker. Posting, rather than awaiting, lets the check end at once: the checker, and a shutdown that
	// waits for it, never depend on the user's answer. A failure to post leaves the version unannounced, so the next check repeats it.
	private void CheckerUpdateAvailable(ProductUpdate update)
		=> Services.GetRequiredService<IUserInterfaceServices>().ExecuteInUIFriendlyContext(() => _ = PresentAsync(update), true);

	private async Task PresentAsync(ProductUpdate update) {
		var result = ProductUpdateNoticeResult.NotShown;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => result = await ShowAsync(update));
		// The checker counts an update as announced once it has been handed over. One that was not shown, or whose presentation
		// failed, is handed back so that the next check announces it again.
		if (error != null || result == ProductUpdateNoticeResult.NotShown)
			_checker.OfferAgain(update);
		if (error == null || error is OperationCanceledException)
			return;
		// The application keeps running, and the user is told why the update was not installed.
		Tools.Exceptions.ExecuteIgnoringException(() => Logger.Exception(error, "The product update could not be installed."));
		await Tools.Exceptions.ExecuteCapturingExceptionAsync(
			() => Services.GetRequiredService<IUserInterfaceServices>().ReportErrorAsync("Update Not Installed", error.Message)
		);
	}
}
