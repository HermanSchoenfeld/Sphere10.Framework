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

namespace Sphere10.Framework.Windows.Files;

/// <summary>Windows file resource operations, including named alternate data streams.</summary>
public class FileUtil {
	public virtual WindowsFileInspection Inspect(string filePath) => new WindowsFileInspectionService().Inspect(filePath);

	public virtual Task<WindowsFileInspection> InspectAsync(string filePath, CancellationToken cancellationToken = default)
		=> new WindowsFileInspectionService().InspectAsync(filePath, cancellationToken);

	public virtual WindowsAlternateStreamInfo[] GetAlternateStreams(string filePath) => WindowsFile.GetAlternateStreams(filePath);

	public virtual Task<WindowsAlternateStreamInfo[]> GetAlternateStreamsAsync(string filePath, CancellationToken cancellationToken = default)
		=> WindowsFile.GetAlternateStreamsAsync(filePath, cancellationToken);

	public virtual FileStream OpenAlternateStream(string filePath, string streamName, FileMode mode, FileAccess access)
		=> WindowsFile.OpenAlternateStream(filePath, streamName, mode, access);

	public virtual void DeleteAlternateStream(string filePath, string streamName) => WindowsFile.DeleteAlternateStream(filePath, streamName);

	public virtual Task DeleteAlternateStreamAsync(string filePath, string streamName, CancellationToken cancellationToken = default)
		=> WindowsFile.DeleteAlternateStreamAsync(filePath, streamName, cancellationToken);

	public virtual Task<byte[]> ReadAlternateStreamAsync(string filePath, string streamName, int maximumBytes, CancellationToken cancellationToken = default) => Task.Run(async () => {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		Guard.Argument(maximumBytes > 0, nameof(maximumBytes), "Specify a positive byte limit.");
		cancellationToken.ThrowIfCancellationRequested();
		await using var stream = OpenAlternateStream(filePath, streamName, FileMode.Open, FileAccess.Read);
		Guard.Ensure(stream.Length <= maximumBytes, "The stream is larger than the requested read limit.");
		var bytes = new byte[checked((int)stream.Length)];
		await stream.ReadExactlyAsync(bytes, cancellationToken).ConfigureAwait(false);
		return bytes;
	}, cancellationToken);

	public virtual Task WriteAlternateStreamAsync(string filePath, string streamName, byte[] data, bool createNew, CancellationToken cancellationToken = default) => Task.Run(async () => {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		Guard.ArgumentNotNull(data, nameof(data));
		cancellationToken.ThrowIfCancellationRequested();
		await using var stream = OpenAlternateStream(filePath, streamName, createNew ? FileMode.CreateNew : FileMode.Truncate, FileAccess.Write);
		await stream.WriteAsync(data, cancellationToken).ConfigureAwait(false);
		await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
	}, cancellationToken);
}
