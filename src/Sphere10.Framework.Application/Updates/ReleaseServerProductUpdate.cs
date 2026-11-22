// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

/// <summary>
/// An update offered by the application's release server, with the installer that performs it. The application's
/// <see cref="IProductUpdateService"/> answers it, <see cref="ReleaseServerProductUpdater"/> checks it, and a platform's release-server
/// updater downloads, verifies and runs its installer.
/// </summary>
public class ReleaseServerProductUpdate : ProductUpdate {

	/// <summary>The absolute http or https address of the installer.</summary>
	public string DownloadUrl { get; set; }

	/// <summary>The installer's file name, including its extension.</summary>
	public string FileName { get; set; }

	/// <summary>The installer's length in bytes, or zero when it is not known.</summary>
	public long Length { get; set; }

	/// <summary>The installer's SHA-256 hash as 64 hexadecimal characters.</summary>
	public string Hash { get; set; }

	/// <summary>Command-line arguments passed to the installer, or null for none.</summary>
	public string InstallerArguments { get; set; }

	/// <summary>
	/// Checks that a downloaded file is this update's installer. It fails when the file is missing, when its length differs from a
	/// declared <see cref="Length"/>, when <see cref="Hash"/> is not a SHA-256 hash or when the file's hash differs from it. The file
	/// can change once this method returns, so a caller that will run it uses <see cref="VerifyFile(Stream)"/> instead.
	/// </summary>
	public Result VerifyFile(string path) {
		Guard.ArgumentNotNullOrWhitespace(path, nameof(path));
		if (!File.Exists(path))
			return Result.Error($"The downloaded file '{path}' does not exist.");
		using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		return VerifyFile(file);
	}

	/// <summary>
	/// Checks an open file as <see cref="VerifyFile(string)"/> does, reading it from its start. A caller that will run the file opens
	/// it with <see cref="FileShare.Read"/> before this call and keeps that stream open until the file has been started. No other
	/// handle can then write, replace or delete the file, so the bytes that run are the bytes that were verified.
	/// </summary>
	public Result VerifyFile(Stream file) {
		Guard.ArgumentNotNull(file, nameof(file));
		Guard.Argument(file.CanRead && file.CanSeek, nameof(file), "Supply a readable, seekable stream.");
		// An update without a hash is refused, since nothing shows that the file is the publisher's installer.
		if (Hash == null || !HexEncoding.TryDecode(Hash, out var expectedHash) || expectedHash.Length != Hashers.GetDigestSizeBytes(CHF.SHA2_256))
			return Result.Error("The update does not declare a valid SHA-256 hash, so the downloaded file cannot be verified.");
		if (Length > 0 && file.Length != Length)
			return Result.Error($"The downloaded file is {file.Length} bytes long but the update declares {Length} bytes.");
		file.Position = 0;
		using var hasher = new HashingStream(CHF.SHA2_256);
		file.CopyTo(hasher);
		return hasher.GetDigest().AsSpan().SequenceEqual(expectedHash)
			? Result.Success
			: Result.Error("The downloaded file does not match the update's SHA-256 hash.");
	}

	/// <summary>Runs <see cref="VerifyFile(string)"/> on a worker, so a caller on a UI thread does not wait for the file to be hashed.</summary>
	public Task<Result> VerifyFileAsync(string path, CancellationToken cancellationToken = default)
		=> Task.Run(() => VerifyFile(path), cancellationToken);

	/// <summary>Runs <see cref="VerifyFile(Stream)"/> on a worker. The caller keeps the stream open and does not use it until the task completes.</summary>
	public Task<Result> VerifyFileAsync(Stream file, CancellationToken cancellationToken = default)
		=> Task.Run(() => VerifyFile(file), cancellationToken);
}
