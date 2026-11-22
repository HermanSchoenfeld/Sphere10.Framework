// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

/// <summary>
/// The updater of the application's own release server. It checks through the application's <see cref="IProductUpdateService"/> and
/// accepts only an offer that names its installer. The Application module registers it when the host registered no updater.
/// </summary>
/// <remarks>
/// This portable updater finds updates but cannot install one, because the installer is downloaded and run by the platform: the
/// WinForms library's <c>WindowsReleaseServerProductUpdater</c> derives from it and installs. <see cref="InstallAsync"/> on this class
/// fails with <see cref="NotSupportedException"/>.
/// </remarks>
public class ReleaseServerProductUpdater : ProductUpdaterBase {

	public ReleaseServerProductUpdater(IProductUpdateService service, ILogger logger = null)
		: base(logger) {
		Guard.ArgumentNotNull(service, nameof(service));
		Service = service;
	}

	protected IProductUpdateService Service { get; }

	protected override async Task<ProductUpdateCheckResult> FetchUpdateAsync(CancellationToken cancellationToken) {
		var result = await Service.CheckForUpdateAsync(cancellationToken).ConfigureAwait(false) ?? ProductUpdateCheckResult.NoUpdate;
		// The installer is downloaded from the address the server gives, so an offer without one is a failed check, not an update.
		if (result.IsUpdateAvailable)
			Guard.Ensure(result.Update is ReleaseServerProductUpdate release && !string.IsNullOrWhiteSpace(release.DownloadUrl), "The release server offered an update without its installer's download address.");
		return result;
	}

	protected override Task<ProductUpdateInstallResult> InstallUpdateAsync(ProductUpdate update, nint ownerWindow, CancellationToken cancellationToken)
		=> Task.FromException<ProductUpdateInstallResult>(new NotSupportedException("This host cannot install a release-server update; register a platform's release-server updater, such as the WinForms library's."));
}
