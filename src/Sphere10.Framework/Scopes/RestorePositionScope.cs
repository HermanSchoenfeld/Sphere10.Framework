// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Restores a borrowed stream's captured position without closing the stream.</summary>
/// <remarks>
/// Obtain this scope through <see cref="StreamExtensions.EnterRestorePositionScope"/> or <see cref="StreamExtensions.EnterRestorePositionSeek"/>.
/// Keep one concrete local instance, normally with <c>using var</c>, to avoid boxing. Dispose that owner once.
/// </remarks>
public readonly struct RestorePositionScope : IScope {
	private readonly Stream _stream;
	private readonly long _position;

	internal RestorePositionScope(Stream stream, long position) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		_stream = stream;
		_position = position;
	}

	public void Dispose() => _stream.Position = _position;

	public ValueTask DisposeAsync() => new(Task.Run(Dispose));
}
