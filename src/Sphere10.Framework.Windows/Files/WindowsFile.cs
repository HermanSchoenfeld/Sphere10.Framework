// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Files;

/// <summary>Copies one Windows file, including its streams, with cooperative cancellation and byte progress.</summary>
public static class WindowsFile {
	/// <summary>Progress and checkpoints run synchronously on the calling worker. Cancelled copies discard partial destination data.</summary>
	public static void Copy(string sourcePath, string destinationPath, bool overwrite = true,
		Action<WindowsFileCopyProgress> progress = null, Action checkpoint = null) {
		Guard.ArgumentNotNullOrEmpty(sourcePath, nameof(sourcePath));
		Guard.ArgumentNotNullOrEmpty(destinationPath, nameof(destinationPath));
		var source = ExtendedPath(sourcePath);
		var destination = ExtendedPath(destinationPath);
		checkpoint?.Invoke();
		Exception callbackFailure = null;
		WinAPI.KERNEL32.ProgressCallback callback = (total, transferred, streamSize, streamTransferred, streamNumber, reason, sourceHandle, destinationHandle, data) => {
			try {
				checkpoint?.Invoke();
				progress?.Invoke(new WindowsFileCopyProgress(total, transferred));
				checkpoint?.Invoke();
				return WinAPI.KERNEL32.Continue;
			} catch (Exception error) {
				callbackFailure ??= error;
				return WinAPI.KERNEL32.Cancel;
			}
		};
		var copied = WinAPI.KERNEL32.CopyFileEx(source, destination, callback, IntPtr.Zero, IntPtr.Zero, overwrite ? 0U : 1U);
		var nativeError = Marshal.GetLastPInvokeError();
		GC.KeepAlive(callback);
		if (callbackFailure != null)
			ExceptionDispatchInfo.Capture(callbackFailure).Throw();
		checkpoint?.Invoke();
		if (!copied) {
			if (nativeError == 5)
				throw new UnauthorizedAccessException($"Access denied copying '{sourcePath}' to '{destinationPath}'.");
			throw new Win32Exception(nativeError);
		}
	}

	public static Task CopyAsync(string sourcePath, string destinationPath, bool overwrite = true, CancellationToken cancellationToken = default,
		Action<WindowsFileCopyProgress> progress = null, Action checkpoint = null)
		=> Task.Run(() => Copy(sourcePath, destinationPath, overwrite, progress, () => { cancellationToken.ThrowIfCancellationRequested(); checkpoint?.Invoke(); cancellationToken.ThrowIfCancellationRequested(); }), cancellationToken);

	/// <summary>Enumerates named data streams without returning the primary file contents.</summary>
	public static WindowsAlternateStreamInfo[] GetAlternateStreams(string filePath) => GetAlternateStreamsCore(filePath, null);

	public static Task<WindowsAlternateStreamInfo[]> GetAlternateStreamsAsync(string filePath, CancellationToken cancellationToken = default)
		=> Task.Run(() => GetAlternateStreamsCore(filePath, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

	/// <summary>Opens a named data stream on an existing file. The caller owns the returned stream.</summary>
	public static FileStream OpenAlternateStream(string filePath, string streamName, FileMode mode, FileAccess access) {
		var path = ValidatePrimaryFilePath(filePath);
		var streamPath = AlternateStreamPath(path, streamName);
		// Keep the base file from being renamed/replaced while opening its stream; never create the base file.
		using var primary = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		return new FileStream(streamPath, mode, access, FileShare.Read, 4096, FileOptions.Asynchronous);
	}

	public static void DeleteAlternateStream(string filePath, string streamName) {
		var path = ValidatePrimaryFilePath(filePath);
		var streamPath = AlternateStreamPath(path, streamName);
		// Delete access is checked against open streams; this guard must not block its own named-stream deletion.
		using var primary = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		if (!WinAPI.KERNEL32.DeleteFileW(streamPath))
			throw new Win32Exception(Marshal.GetLastPInvokeError(), "Windows could not remove the named file stream.");
	}

	public static Task DeleteAlternateStreamAsync(string filePath, string streamName, CancellationToken cancellationToken = default)
		=> Task.Run(() => { cancellationToken.ThrowIfCancellationRequested(); DeleteAlternateStream(filePath, streamName); }, cancellationToken);

	private static WindowsAlternateStreamInfo[] GetAlternateStreamsCore(string filePath, Action checkpoint) {
		var path = ValidatePrimaryFilePath(filePath);
		checkpoint?.Invoke();
		using var primary = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
		using var search = WinAPI.KERNEL32.FindFirstStreamW(ExtendedPath(path), 0, out var data, 0);
		var error = Marshal.GetLastPInvokeError();
		if (search.IsInvalid) {
			if (error == 38) return Array.Empty<WindowsAlternateStreamInfo>();
			throw new Win32Exception(error, "Windows could not enumerate the file's data streams. The filesystem must support named streams.");
		}
		var result = new List<WindowsAlternateStreamInfo>();
		do {
			checkpoint?.Invoke();
			var raw = data.StreamName;
			if (raw != "::$DATA" && raw?.StartsWith(":", StringComparison.Ordinal) == true && raw.EndsWith(":$DATA", StringComparison.Ordinal))
				result.Add(new WindowsAlternateStreamInfo(path, raw.Substring(1, raw.Length - 7), data.StreamSize));
		} while (WinAPI.KERNEL32.FindNextStreamW(search, out data));
		error = Marshal.GetLastPInvokeError();
		if (error != 38)
			throw new Win32Exception(error, "Windows could not finish enumerating the file's data streams.");
		checkpoint?.Invoke();
		return result.ToArray();
	}

	private static string ValidatePrimaryFilePath(string filePath) {
		Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
		var full = Path.GetFullPath(filePath);
		Guard.Argument(full.Substring(Path.GetPathRoot(full).Length).IndexOf(':') < 0, nameof(filePath), "Select a primary file, not a stream path.");
		return full;
	}

	private static string AlternateStreamPath(string filePath, string streamName) {
		Guard.ArgumentNotNullOrEmpty(streamName, nameof(streamName));
		Guard.Argument(streamName.IndexOfAny(Path.GetInvalidFileNameChars()) < 0, nameof(streamName), "The stream name must not contain path separators, colons or invalid filename characters.");
		return ExtendedPath(filePath) + ":" + streamName + ":$DATA";
	}

	private static string ExtendedPath(string path) {
		var full = Path.GetFullPath(path);
		if (full.StartsWith(@"\\?\", StringComparison.Ordinal))
			return full;
		return full.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + full[2..] : @"\\?\" + full;
	}
}
