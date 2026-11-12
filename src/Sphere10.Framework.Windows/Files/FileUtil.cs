// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Files;
using Sphere10.Framework.Windows;
using Sphere10.Framework;

namespace Tools;

public static partial class Windows {

	/// <summary>Windows file resource operations, including named alternate data streams.</summary>
	public static class Files {
		/// <summary>Creates a fresh service using the existing current-entry-assembly host selection. No capture is started.</summary>
		public static FileHandleService CreateHandleService() => new FileHandleService();

		/// <summary>Creates a fresh service using the caller's diagnostic host. The existing constructor rejects an explicit null host.</summary>
		public static FileHandleService CreateHandleService(WindowsProcessWorkerHost workerHost) => new FileHandleService(workerHost);

		/// <summary>Creates a fresh on-demand inspection service without opening an inspected file.</summary>
		public static WindowsFileInspectionService CreateInspectionService() => new WindowsFileInspectionService();

		/// <summary>Routes the existing file-handle helper command before the application starts. Unrelated arguments are left to the caller.</summary>
		public static bool TryRunHandleWorker(string[] args, out int exitCode) => FileHandleService.TryRunWorker(args, out exitCode);

		/// <summary>Reads PE structure without loading the image. Malformed optional tables remain explicit issues on the detached result.</summary>
		public static PortableExecutableInfo InspectPortableExecutable(string filePath) => PortableExecutableReader.Read(filePath, null);

		public static Task<PortableExecutableInfo> InspectPortableExecutableAsync(string filePath, CancellationToken cancellationToken = default)
			=> Task.Run(() => PortableExecutableReader.Read(filePath, cancellationToken.ThrowIfCancellationRequested), cancellationToken);

		/// <summary>Reads a bounded slice without materializing the complete file. EOF returns the available suffix.</summary>
		public static Task<byte[]> ReadFileRangeAsync(string filePath, long offset, int count, CancellationToken cancellationToken = default)
			=> ReadFileRangeCoreAsync(filePath, offset, count, null, cancellationToken);

		/// <summary>Reads against the captured image revision, rejecting a changed path before interpreting a captured file offset.</summary>
		public static Task<byte[]> ReadPortableExecutableRangeAsync(PortableExecutableInfo image, long offset, int count, CancellationToken cancellationToken = default) {
			Guard.ArgumentNotNull(image, nameof(image));
			return ReadFileRangeCoreAsync(image.FilePath, offset, count, image, cancellationToken);
		}

		public static WindowsFileInspection Inspect(string filePath) => new WindowsFileInspectionService().Inspect(filePath);

		public static Task<WindowsFileInspection> InspectAsync(string filePath, CancellationToken cancellationToken = default)
			=> new WindowsFileInspectionService().InspectAsync(filePath, cancellationToken);

		public static WindowsAlternateStreamInfo[] GetAlternateStreams(string filePath) => WindowsFile.GetAlternateStreams(filePath);

		public static Task<WindowsAlternateStreamInfo[]> GetAlternateStreamsAsync(string filePath, CancellationToken cancellationToken = default)
			=> WindowsFile.GetAlternateStreamsAsync(filePath, cancellationToken);

		public static FileStream OpenAlternateStream(string filePath, string streamName, FileMode mode, FileAccess access)
			=> WindowsFile.OpenAlternateStream(filePath, streamName, mode, access);

		public static void DeleteAlternateStream(string filePath, string streamName) => WindowsFile.DeleteAlternateStream(filePath, streamName);

		public static Task DeleteAlternateStreamAsync(string filePath, string streamName, CancellationToken cancellationToken = default)
			=> WindowsFile.DeleteAlternateStreamAsync(filePath, streamName, cancellationToken);

		public static Task<byte[]> ReadAlternateStreamAsync(string filePath, string streamName, int maximumBytes, CancellationToken cancellationToken = default) => Task.Run(async () => {
			Debug.AssertNotMainThread();
			Guard.Argument(maximumBytes > 0, nameof(maximumBytes), "Specify a positive byte limit.");
			cancellationToken.ThrowIfCancellationRequested();
			await using var stream = OpenAlternateStream(filePath, streamName, FileMode.Open, FileAccess.Read);
			Guard.Ensure(stream.Length <= maximumBytes, "The stream is larger than the requested read limit.");
			var bytes = new byte[checked((int)stream.Length)];
			await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
			return bytes;
		}, cancellationToken);

		public static Task WriteAlternateStreamAsync(string filePath, string streamName, byte[] data, bool createNew, CancellationToken cancellationToken = default) => Task.Run(async () => {
			Debug.AssertNotMainThread();
			Guard.ArgumentNotNull(data, nameof(data));
			cancellationToken.ThrowIfCancellationRequested();
			await using var stream = OpenAlternateStream(filePath, streamName, createNew ? FileMode.CreateNew : FileMode.Truncate, FileAccess.Write);
			await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
			await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
		}, cancellationToken);

		private static Task<byte[]> ReadFileRangeCoreAsync(string filePath, long offset, int count, PortableExecutableInfo expected, CancellationToken cancellationToken) {
			Guard.ArgumentNotNullOrEmpty(filePath, nameof(filePath));
			Guard.ArgumentGTE(offset, 0, nameof(offset));
			Guard.ArgumentInRange(count, 1, 1048576, nameof(count));
			return Task.Run(async () => {
				Debug.AssertNotMainThread();
				await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
				if (expected != null)
					Guard.Ensure(stream.Length == expected.FileLength && File.GetLastWriteTimeUtc(filePath) == expected.LastWriteUtc,
						"The binary file changed after structural inspection. Refresh before reading captured offsets.");
				Guard.ArgumentLTE(offset, stream.Length, nameof(offset));
				stream.Position = offset;
				var bytes = new byte[(int)Math.Min(count, stream.Length - offset)];
				await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
				return bytes;
			}, cancellationToken);
		}
	}
}
