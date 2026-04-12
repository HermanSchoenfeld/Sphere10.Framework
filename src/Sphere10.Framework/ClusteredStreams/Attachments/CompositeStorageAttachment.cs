// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Sphere10.Framework;

/// <summary>
/// Base implementation for an <see cref="IClusteredStreamsAttachment"/> that requires multiple reserved streams.
/// Each stream is opened during <see cref="Attach"/> and accessible via <see cref="GetAttachmentStream"/>.
/// </summary>
public abstract class CompositeStorageAttachment : IDisposable, IClusteredStreamsAttachment {
	private readonly int _streamCount;
	private Stream[] _openStreams;
	private bool _attached;

	protected CompositeStorageAttachment(ClusteredStreams streams, string attachmentID, int streamCount) {
		Guard.ArgumentNotNull(streams, nameof(streams));
		Guard.ArgumentNotNull(attachmentID, nameof(attachmentID));
		Guard.ArgumentGTE(streamCount, 1, nameof(streamCount));
		AttachmentID = attachmentID;
		Streams = streams;
		_streamCount = streamCount;
		_attached = false;
		BaseReservedStreamIndex = -1;
	}

	public string AttachmentID { get; }

	public int StreamCount => _streamCount;

	public ClusteredStreams Streams { get; }

	public int BaseReservedStreamIndex { get; private set; }

	public bool IsAttached => _attached;

	/// <summary>
	/// Returns the open stream at the given relative index within this attachment's reserved stream block.
	/// </summary>
	protected Stream GetAttachmentStream(int relativeIndex) {
		CheckAttached();
		Guard.ArgumentInRange(relativeIndex, 0, _streamCount - 1, nameof(relativeIndex));
		return _openStreams[relativeIndex];
	}

	public void Attach() {
		CheckNotAttached();
		Guard.Ensure(!Streams.RequiresLoad, "Unable to attach to an unloaded Object Container");
		Guard.Ensure(Streams.Header.ReservedStreams > 0, "Stream Container has no reserved streams available");
		using (Streams.EnterAccessScope()) {
			_attached = true;

			// Calculate the base reserved stream index as the cumulative StreamCount of all preceding attachments
			BaseReservedStreamIndex = Streams.CalculateAttachmentBaseIndex(AttachmentID);
			Guard.Against(BaseReservedStreamIndex == -1, $"{GetType().ToStringCS()} was not registered with {nameof(ClusteredStreams)} owner");

			// Open all reserved streams for this attachment
			_openStreams = new Stream[_streamCount];
			for (var i = 0; i < _streamCount; i++)
				_openStreams[i] = Streams.Open(BaseReservedStreamIndex + i, false, false);

			AttachInternal();
			VerifyIntegrity();
		}
	}

	protected abstract void AttachInternal();

	protected abstract void VerifyIntegrity();

	protected abstract void DetachInternal();

	public void Detach() {
		CheckAttached();
		Flush();
		using (Streams.EnterAccessScope()) {
			DetachInternal();
			for (var i = 0; i < _openStreams.Length; i++) {
				_openStreams[i]?.Dispose();
				_openStreams[i] = null;
			}
		}
		_openStreams = null;
		_attached = false;
	}

	public virtual void Flush() {
	}

	public virtual void Dispose() {
		if (IsAttached)
			Detach();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void CheckAttached() {
		if (!_attached)
			throw new InvalidOperationException("Attachment is not attached");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected void CheckNotAttached() {
		if (_attached)
			throw new InvalidOperationException("Attachment is already attached");
	}
}
