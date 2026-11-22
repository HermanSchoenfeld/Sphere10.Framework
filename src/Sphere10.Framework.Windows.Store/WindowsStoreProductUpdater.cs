// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Windows.ApplicationModel;
using Windows.Services.Store;
using WinRT.Interop;

namespace Sphere10.Framework.Windows.Store;

/// <summary>
/// The updater of an application distributed through the Microsoft Store. A check asks the Store for the package updates of the
/// application and its optional packages; an installation hands them to the Store, which shows its own consent, downloads them, closes
/// the application and starts it again. The Windows Store library's <see cref="ModuleConfiguration"/> registers it.
/// </summary>
/// <remarks>
/// <para>
/// A check answers <see cref="ProductUpdateCheckResult.NoUpdate"/>, never an error, when the Store cannot offer updates: when the process
/// has no package identity (an unpackaged or development run) and when the Store refuses the request, as it does for a package that was
/// not installed from the Store. The reason is logged. The update's version is that of the application's own package, it is mandatory
/// when any of its package updates is, and it has no release notes, because the Store publishes none to the application.
/// </para>
/// <para>
/// An installation initialises the Store's context with the notice's window, as a desktop application must for the Store to show its
/// dialogs, or with the process's main window when it is given none. It answers <see cref="ProductUpdateInstallResult.Completed"/> when the
/// Store installed the update and <see cref="ProductUpdateInstallResult.Declined"/> when the user cancelled in the Store's dialog, and
/// fails with the Store's reason otherwise. Progress is raised with the bytes of every package summed.
/// </para>
/// <para>
/// The Store is reached only through the protected virtual members <see cref="GetPackageFamilyName"/>,
/// <see cref="GetStorePackageUpdatesAsync"/> and <see cref="RequestDownloadAndInstallAsync"/>, so a subclass can run the updater without
/// the Store, as its tests do.
/// </para>
/// </remarks>
public class WindowsStoreProductUpdater : ProductUpdaterBase {

	public WindowsStoreProductUpdater(ILogger logger = null)
		: base(logger) {
	}

	protected override async Task<ProductUpdateCheckResult> FetchUpdateAsync(CancellationToken cancellationToken) {
		var familyName = GetPackageFamilyName();
		if (familyName == null) {
			Logger.Info("Windows Store updates are not available because the application has no package identity.");
			return ProductUpdateCheckResult.NoUpdate;
		}
		StorePackageUpdateInfo[] updates;
		try {
			updates = await GetStorePackageUpdatesAsync(cancellationToken).ConfigureAwait(false);
		} catch (Exception error) when (error is not OperationCanceledException) {
			// A package the Store did not install, or a Store that cannot answer, offers no update; it is not a failure to repeat.
			Logger.Warning($"Windows Store updates are not available: {error.Message}");
			return ProductUpdateCheckResult.NoUpdate;
		}
		return CreateCheckResult(familyName, updates ?? Array.Empty<StorePackageUpdateInfo>());
	}

	protected override async Task<ProductUpdateInstallResult> InstallUpdateAsync(ProductUpdate update, nint ownerWindow, CancellationToken cancellationToken) {
		var storeUpdate = update as WindowsStoreProductUpdate;
		Guard.Argument(storeUpdate is { Packages.Length: > 0 }, nameof(update), "The Windows Store updater installs only the updates it found.");
		var state = await RequestDownloadAndInstallAsync(storeUpdate.Packages, ownerWindow, new InstallProgress(this), cancellationToken);
		return state switch {
			StorePackageUpdateState.Completed => ProductUpdateInstallResult.Completed,
			StorePackageUpdateState.Canceled => ProductUpdateInstallResult.Declined,
			StorePackageUpdateState.ErrorLowBattery => throw new InvalidOperationException("The Windows Store did not install the update because the battery is low."),
			StorePackageUpdateState.ErrorWiFiRecommended => throw new InvalidOperationException("The Windows Store did not install the update because it recommends a Wi-Fi connection for its download."),
			StorePackageUpdateState.ErrorWiFiRequired => throw new InvalidOperationException("The Windows Store did not install the update because its download requires a Wi-Fi connection."),
			_ => throw new InvalidOperationException($"The Windows Store did not install the update ({state}).")
		};
	}

	/// <summary>The package family of the running application, or null when the process has no package identity.</summary>
	protected virtual string GetPackageFamilyName() {
		try {
			return Package.Current.Id.FamilyName;
		} catch (Exception error) when (error is InvalidOperationException or COMException) {
			// APPMODEL_ERROR_NO_PACKAGE: an unpackaged process has no package, so the Store has nothing to update.
			return null;
		}
	}

	/// <summary>Asks the Store for the package updates of the application and its optional packages.</summary>
	protected virtual async Task<StorePackageUpdateInfo[]> GetStorePackageUpdatesAsync(CancellationToken cancellationToken) {
		var updates = await StoreContext.GetDefault().GetAppAndOptionalStorePackageUpdatesAsync().AsTask(cancellationToken).ConfigureAwait(false);
		return updates.Select(update => new StorePackageUpdateInfo(update.Package.Id.FamilyName, ToApplicationVersion(update.Package.Id.Version), update.Mandatory, update)).ToArray();
	}

	/// <summary>
	/// Asks the Store to download and install the package updates, reporting each package's status, and completes with the overall
	/// state. The Store shows its own consent and progress, owned by the window given.
	/// </summary>
	protected virtual async Task<StorePackageUpdateState> RequestDownloadAndInstallAsync(StorePackageUpdateInfo[] packages, nint ownerWindow, IProgress<StorePackageUpdateStatus> progress, CancellationToken cancellationToken) {
		var context = StoreContext.GetDefault();
		// A desktop application names the window that owns the Store's dialogs (IInitializeWithWindow); without one they cannot show.
		var window = ownerWindow;
		if (window == 0) {
			using var process = Process.GetCurrentProcess();
			window = process.MainWindowHandle;
		}
		if (window != 0)
			InitializeWithWindow.Initialize(context, window);
		var request = context.RequestDownloadAndInstallStorePackageUpdatesAsync(packages.Select(package => package.StoreUpdate).ToList());
		var result = await request.AsTask(cancellationToken, progress);
		return result.OverallState;
	}

	// The application's own package names the version; an offer of optional packages alone takes the highest of theirs.
	private static ProductUpdateCheckResult CreateCheckResult(string familyName, StorePackageUpdateInfo[] updates) {
		if (updates.Length == 0)
			return ProductUpdateCheckResult.NoUpdate;
		var application = updates.FirstOrDefault(update => string.Equals(update.PackageFamilyName, familyName, StringComparison.OrdinalIgnoreCase)) ?? updates.MaxBy(update => update.Version);
		return ProductUpdateCheckResult.Available(new WindowsStoreProductUpdate {
			Version = application.Version,
			IsMandatory = updates.Any(update => update.IsMandatory),
			Packages = updates
		});
	}

	private static ApplicationVersion ToApplicationVersion(PackageVersion version)
		=> new() { Major = version.Major, Minor = version.Minor, Build = version.Build, Revision = version.Revision };

	#region Inner Types

	/// <summary>
	/// Sums the Store's per-package statuses into one progress: installing once any package deploys, downloading once any package
	/// downloads, waiting before that.
	/// </summary>
	private sealed class InstallProgress : IProgress<StorePackageUpdateStatus> {
		private readonly WindowsStoreProductUpdater _updater;
		private readonly Dictionary<string, StorePackageUpdateStatus> _packages = new(StringComparer.OrdinalIgnoreCase);

		public InstallProgress(WindowsStoreProductUpdater updater) => _updater = updater;

		public void Report(StorePackageUpdateStatus status) {
			ProductUpdateProgress progress;
			lock (_packages) {
				_packages[status.PackageFamilyName ?? string.Empty] = status;
				var states = _packages.Values.Select(package => package.PackageUpdateState).ToArray();
				var stage = states.Contains(StorePackageUpdateState.Deploying) || states.All(state => state == StorePackageUpdateState.Completed)
					? ProductUpdateStage.Installing
					: states.Contains(StorePackageUpdateState.Downloading) ? ProductUpdateStage.Downloading : ProductUpdateStage.Waiting;
				var transferred = _packages.Values.Aggregate(0UL, (sum, package) => sum + package.PackageBytesDownloaded);
				var total = _packages.Values.Aggregate(0UL, (sum, package) => sum + package.PackageDownloadSizeInBytes);
				progress = new ProductUpdateProgress(stage, (long)Math.Min(transferred, (ulong)long.MaxValue), (long)Math.Min(total, (ulong)long.MaxValue));
			}
			_updater.NotifyInstallProgressChanged(progress);
		}
	}

	#endregion
}
