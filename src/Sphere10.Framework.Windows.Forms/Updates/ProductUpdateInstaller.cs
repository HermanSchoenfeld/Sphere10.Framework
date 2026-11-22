// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.BITS;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Carries out one <see cref="ReleaseServerProductUpdate"/>. <see cref="DownloadAsync"/> downloads its installer with BITS into a new temporary
/// folder and verifies it against the update's SHA-256 hash, and <see cref="LaunchAsync"/> starts the verified installer with the
/// update's arguments. <see cref="WindowsReleaseServerProductUpdater"/> drives both when the user accepts the update in <see cref="ProductUpdateDialog"/>.
/// </summary>
/// <remarks>
/// The installer's file name comes from the server, so it is checked before anything is downloaded and only an .exe or .msi file
/// with a plain, valid name is accepted. The downloaded file is opened denying write and delete to every other handle before it is
/// hashed, and that handle is kept until the installer process has been started, so the bytes that run are the bytes that were
/// verified. A failed, refused or cancelled download, a failed or declined launch and disposal without a launch all remove the
/// temporary folder. A started installer stays in its folder, since it outlives the application that started it. Download
/// failures are logged. <see cref="DownloadProgressChanged"/> is raised on a worker thread.
/// </remarks>
public class ProductUpdateInstaller : SyncDisposable {

	public event EventHandlerEx<JobProgress, JobState> DownloadProgressChanged;

	// ERROR_ELEVATION_REQUIRED, the answer of a direct start to an executable whose manifest asks for elevation.
	private const int ElevationRequiredError = 740;
	private string _folder;
	private FileStream _verifiedFile;
	private bool _launched;

	public ProductUpdateInstaller(ReleaseServerProductUpdate update, ProductInformation product, ILogger logger = null) {
		Guard.ArgumentNotNull(update, nameof(update));
		Guard.ArgumentNotNull(product, nameof(product));
		Update = update;
		Product = product;
		Logger = logger ?? new NoOpLogger();
	}

	public ReleaseServerProductUpdate Update { get; }

	/// <summary>The running product, whose name and version the notice shows beside the update.</summary>
	public ProductInformation Product { get; }

	/// <summary>The downloaded and verified installer, or null before a successful download and after a failed launch.</summary>
	public string InstallerPath { get; private set; }

	protected ILogger Logger { get; }

	/// <summary>
	/// Downloads and verifies the installer, completing with its path. It fails when the update's file name is refused, the download
	/// fails or the file does not match the update, and is cancelled through the token. In each of those cases the temporary folder
	/// is removed, and the download can be tried again.
	/// </summary>
	public async Task<string> DownloadAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		Guard.Ensure(_verifiedFile == null && !_launched, "This installer has already been downloaded.");
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => DownloadAndVerifyAsync(cancellationToken)).ConfigureAwait(false);
		if (error != null) {
			if (error is not OperationCanceledException)
				Logger.Exception(error, $"The update installer could not be downloaded from '{Update.DownloadUrl}'.");
			ExceptionDispatchInfo.Capture(error).Throw();
		}
		return InstallerPath;
	}

	/// <summary>
	/// Starts the downloaded installer with the update's arguments and releases the handle that kept it unchanged. The installer is
	/// not awaited and runs on after this application exits. A launch that fails or is declined removes the temporary folder.
	/// </summary>
	/// <remarks>
	/// An .msi package is run as <c>msiexec /i</c> with the verified path. An .exe is started directly, and only when Windows answers
	/// that it requires elevation is it started again through the shell's "runas" verb, since a direct start cannot show the consent
	/// prompt. The arguments reach the installer as one string and never pass through a command interpreter.
	/// </remarks>
	public virtual async Task LaunchAsync(IntPtr ownerWindow = default, CancellationToken cancellationToken = default) {
		Guard.Ensure(_verifiedFile != null, "Download and verify the installer before starting it.");
		var installerPath = InstallerPath;
		var arguments = Update.InstallerArguments ?? string.Empty;
		var isPackage = Path.GetExtension(installerPath).Equals(".msi", StringComparison.OrdinalIgnoreCase);
		var fileName = isPackage ? Path.Combine(Environment.SystemDirectory, "msiexec.exe") : installerPath;
		var commandLine = isPackage ? $"/i \"{installerPath}\" {arguments}".TrimEnd() : arguments;
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(
			() => StartInstallerAsync(fileName, commandLine, Path.GetDirectoryName(installerPath), ownerWindow, cancellationToken)
		);
		// The start call has returned, so the handle that pinned the verified bytes can go.
		ReleaseVerifiedFile();
		if (error != null) {
			RemoveFolder();
			ExceptionDispatchInfo.Capture(error).Throw();
		}
		_launched = true;
	}

	/// <summary>Downloads the address to the destination. The default uses a BITS job named after the product and this process, so another running instance keeps its own job.</summary>
	protected virtual async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken) {
		var download = new BitsFileDownload($"{Product.ProductName} Update {Environment.ProcessId}", url, destinationPath);
		download.ProgressChanged += NotifyDownloadProgressChanged;
		using (Tools.Scope.ExecuteOnDispose(() => download.ProgressChanged -= NotifyDownloadProgressChanged))
			await download.DownloadAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Starts the installer's process. The returned task completes when the start call has returned, not when the installer exits.</summary>
	protected virtual async Task StartInstallerAsync(string fileName, string arguments, string workingDirectory, IntPtr ownerWindow, CancellationToken cancellationToken) {
		try {
			var start = new ProcessStartInfo { FileName = fileName, Arguments = arguments, WorkingDirectory = workingDirectory, UseShellExecute = false };
			using var installer = await Task.Run(() => Process.Start(start), cancellationToken);
			return;
		} catch (Win32Exception error) when (error.NativeErrorCode == ElevationRequiredError) {
			// Only the shell can ask the user to consent to the elevation this installer's manifest requires.
		}
		using var elevatedInstaller = await Tools.Windows.Shell.LaunchAsync(fileName, arguments, workingDirectory, "runas", ownerWindow, cancellationToken: cancellationToken);
	}

	protected void NotifyDownloadProgressChanged(JobProgress progress, JobState state) {
		OnDownloadProgressChanged(progress, state);
		DownloadProgressChanged?.Invoke(progress, state);
	}

	protected virtual void OnDownloadProgressChanged(JobProgress progress, JobState state) {
	}

	protected override void DisposeInternal() {
		ReleaseVerifiedFile();
		// An installer that was never started has no further use.
		if (!_launched)
			RemoveFolder();
	}

	private async Task DownloadAndVerifyAsync(CancellationToken cancellationToken) {
		var fileName = GetInstallerFileName();
		var folder = Tools.FileSystem.GetTempEmptyDirectory();
		var installerPath = Path.Combine(folder, fileName);
		FileStream verifiedFile = null;
		var verified = false;
		using (Tools.Scope.ExecuteOnDispose(() => {
			if (verified)
				return;
			verifiedFile?.Dispose();
			// Best effort: a leftover temporary folder must not replace the download's own outcome.
			Tools.Exceptions.ExecuteIgnoringException(() => Tools.FileSystem.DeleteDirectory(folder, true));
		})) {
			await DownloadFileAsync(Update.DownloadUrl, installerPath, cancellationToken).ConfigureAwait(false);
			// Sharing only read access denies write and delete to every other handle from before the file is hashed.
			verifiedFile = new FileStream(installerPath, FileMode.Open, FileAccess.Read, FileShare.Read);
			var verification = await Update.VerifyFileAsync(verifiedFile, cancellationToken).ConfigureAwait(false);
			verification.ThrowOnFailure();
			_folder = folder;
			_verifiedFile = verifiedFile;
			InstallerPath = installerPath;
			verified = true;
		}
	}

	// The server names the file, so the name is checked rather than repaired and nothing is downloaded for a name that is refused.
	private string GetInstallerFileName() {
		var fileName = Update.FileName;
		Guard.Ensure(!string.IsNullOrWhiteSpace(fileName), "The update does not name its installer file.");
		var isPlainName = fileName.IndexOfAny(Tools.FileSystem.CrossPlatformInvalidFileNameChars) < 0 && fileName[^1] is not ('.' or ' ') && !IsReservedDeviceName(fileName);
		Guard.Ensure(isPlainName, $"The update names its installer '{fileName}', which is not a valid file name.");
		var extension = Path.GetExtension(fileName).ToLowerInvariant();
		Guard.Ensure(extension is ".exe" or ".msi", $"The update's installer '{fileName}' is neither an .exe nor an .msi file.");
		return fileName;
	}

	// Windows reserves these names whatever follows their first dot, so "NUL.exe" opens the device.
	private static bool IsReservedDeviceName(string fileName) {
		var dot = fileName.IndexOf('.');
		var name = (dot < 0 ? fileName : fileName[..dot]).TrimEnd(' ').ToUpperInvariant();
		return name.IsIn(Tools.FileSystem.CrossPlatformForbiddenFileNames)
			|| name is "CONIN$" or "CONOUT$"
			|| name.Length == 4 && name.StartsWith("LPT", StringComparison.Ordinal) && name[3] is >= '1' and <= '9';
	}

	private void ReleaseVerifiedFile() {
		_verifiedFile?.Dispose();
		_verifiedFile = null;
	}

	private void RemoveFolder() {
		if (_folder != null)
			Tools.Exceptions.ExecuteIgnoringException(() => Tools.FileSystem.DeleteDirectory(_folder, true));
		_folder = null;
		InstallerPath = null;
	}
}
