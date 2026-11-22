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
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.BITS;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// The release server's updater on Windows. It checks as <see cref="ReleaseServerProductUpdater"/> does and installs through a
/// <see cref="ProductUpdateInstaller"/>: it downloads the installer with BITS, verifies it against the update's SHA-256 hash, starts it
/// with the update's arguments and answers <see cref="ProductUpdateInstallResult.ExitRequired"/>. <c>UseProductUpdates</c> registers it.
/// </summary>
/// <remarks>
/// The download's progress is raised through <see cref="ProductUpdaterBase.InstallProgressChanged"/> on a BITS worker thread. A refused
/// file name, a failed or mismatched download and an installer that cannot be started fail the installation, and the installer's
/// temporary folder is removed; a started installer keeps its folder, since it outlives the application.
/// </remarks>
public class WindowsReleaseServerProductUpdater : ReleaseServerProductUpdater {

	public WindowsReleaseServerProductUpdater(IProductUpdateService service, IProductInformationProvider product, ILogger logger = null)
		: base(service, logger) {
		Guard.ArgumentNotNull(product, nameof(product));
		Product = product;
	}

	protected IProductInformationProvider Product { get; }

	protected override async Task<ProductUpdateInstallResult> InstallUpdateAsync(ProductUpdate update, nint ownerWindow, CancellationToken cancellationToken) {
		var release = update as ReleaseServerProductUpdate;
		Guard.Argument(release != null, nameof(update), "The release server's updater installs only the updates it found.");
		using var installer = CreateInstaller(release);
		installer.DownloadProgressChanged += InstallerDownloadProgressChanged;
		using (Tools.Scope.ExecuteOnDispose(() => installer.DownloadProgressChanged -= InstallerDownloadProgressChanged))
			await installer.DownloadAsync(cancellationToken);
		await installer.LaunchAsync(ownerWindow, cancellationToken);
		return ProductUpdateInstallResult.ExitRequired;
	}

	/// <summary>Creates the installer for an update of the running product. The updater disposes it when the installation has ended.</summary>
	protected virtual ProductUpdateInstaller CreateInstaller(ReleaseServerProductUpdate update) => new(update, Product.ProductInformation);

	/// <summary>Describes a BITS job's progress. A job that is waiting says why, so an offline machine shows more than an endless download.</summary>
	protected static ProductUpdateProgress DescribeDownloadProgress(ulong bytesTransferred, ulong bytesTotal, JobState state) {
		var stage = state switch {
			JobState.Queued => ProductUpdateStage.Waiting,
			JobState.Connecting => ProductUpdateStage.Connecting,
			JobState.TransientError => ProductUpdateStage.Retrying,
			_ => ProductUpdateStage.Downloading
		};
		// BITS reports no total until the server has answered, and its unknown size is the largest value.
		var total = bytesTotal == ulong.MaxValue ? 0 : (long)bytesTotal;
		return new ProductUpdateProgress(stage, (long)Math.Min(bytesTransferred, (ulong)long.MaxValue), total);
	}

	private void InstallerDownloadProgressChanged(JobProgress progress, JobState state)
		=> NotifyInstallProgressChanged(DescribeDownloadProgress(progress.BytesTransferred, progress.BytesTotal, state));
}
