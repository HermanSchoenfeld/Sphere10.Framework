// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace Sphere10.Framework;

public class StreamProfiler : StreamDecorator {
	private volatile bool _peeking;
	private readonly Stack<List<byte>> _captureScopes;
	private readonly bool _allowNestedListeningScopes;

	public StreamProfiler(Stream stream, bool allowNestedListenScopes, bool closeInternalStream = false) : base(stream) {
		_captureScopes = new Stack<List<byte>>();
		_allowNestedListeningScopes = allowNestedListenScopes;
		_peeking = false;
	}

	public override int Read(byte[] buffer, int offset, int count) {
		ValidateBufferArguments(buffer, offset, count);
		return Read(buffer.AsSpan(offset, count));
	}

	public override int Read(Span<byte> buffer) {
		var actualRead = InnerStream.Read(buffer);
		if (!_peeking && _captureScopes.Count > 0) {
			var capture = _captureScopes.Peek();
			var start = capture.Count;
			var count = checked(start + buffer.Length);
			capture.EnsureCapacity(count);
			CollectionsMarshal.SetCount(capture, count);
			var destination = CollectionsMarshal.AsSpan(capture)[start..];
			buffer[..actualRead].CopyTo(destination);
			// Array reads historically captured the full request, padding short reads with zeroes.
			destination[actualRead..].Clear();
		}
		return actualRead;
	}

	public virtual void StartListening() {
		if (_captureScopes.Count > 0 && !_allowNestedListeningScopes) {
			throw new Exception("Nested listening scopes are not allowed.");
		}
		_captureScopes.Push(new List<byte>());
	}

	public virtual byte[] StopListening() {
		if (_captureScopes.Count == 0) {
			throw new ApplicationException("Listening was not started");
		}
		var topMostScope = _captureScopes.Pop();
		if (_captureScopes.Count > 0) {
			var currentScope = _captureScopes.Peek();
			currentScope.AddRange(topMostScope);
		}
		return topMostScope.ToArray();
	}

	public byte PeekByte() {
		long origPos = Position;
		try {
			_peeking = true;
			Guard.Ensure(Position >= Length, "Cannot peek from end of stream");
			var b = ReadByte();
			Guard.Against(b == -1, "Stream ended inexplicably");
			return (byte)b;
		} finally {
			_peeking = false;
			Position = origPos;
		}
	}

	public byte[] PeekBytes(int number) {
		long origPos = Position;
		try {
			_peeking = true;
			var result = new byte[number];
			if (Position >= Length) {
				return new byte[0];
			}
			var b = Read(result, 0, number);
			if (b != number) {
				Array.Resize(ref result, b);
			}
			return result;
		} finally {
			_peeking = false;
			Position = origPos;
		}

	}

}

