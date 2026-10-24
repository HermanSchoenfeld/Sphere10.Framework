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

namespace Sphere10.Framework;

/// <summary>
/// Creates properties mapped onto caller-owned streams.
/// </summary>
public static class StreamMappedProperty {

	public static StreamMappedProperty<T> From<T>(Stream stream, long offset, long size, IItemSerializer<T> serializer, IEqualityComparer<T> comparer = null, ICriticalObject @lock = null, Endianness endianness = Sphere10FrameworkDefaults.Endianness, bool useCache = false)
		=> new(stream, offset, size, serializer, useCache, comparer, @lock, endianness);

	public static StreamMappedProperty<T> From<T>(Stream stream, long offset, long size, IItemSerializer<T> serializer, EndianBinaryReader reader, EndianBinaryWriter writer, IEqualityComparer<T> comparer = null, ICriticalObject @lock = null, bool useCache = false)
		=> new(stream, offset, size, serializer, reader, writer, useCache, comparer, @lock);

	/// <summary>
	/// Maps a constant-size value using its registered serializer unless one is supplied.
	/// </summary>
	public static StreamMappedProperty<T> At<T>(Stream stream, long offset, IItemSerializer<T> serializer = null, IEqualityComparer<T> comparer = null, ICriticalObject @lock = null, Endianness endianness = Sphere10FrameworkDefaults.Endianness, bool useCache = false) {
		serializer ??= SerializerFactory.Default.GetSerializer<T>();
		Guard.Argument(serializer.IsConstantSize, nameof(serializer), "An explicit mapping size is required for a variable-size serializer");
		return From(stream, offset, serializer.ConstantSize, serializer, comparer, @lock, endianness, useCache);
	}
}

/// <summary>
/// Reads and writes a value at a fixed stream offset. Reads observe the stream unless caching is enabled explicitly.
/// </summary>
/// <remarks>The stream and any supplied reader/writer remain caller-owned. Cached values require <see cref="FlushCache"/>
/// after independent stream changes. Reference values are cached as supplied; they are not cloned.</remarks>
public class StreamMappedProperty<T> {

	public EventHandlerEx<T, T> ValueChanged;

	private readonly Stream _stream;
	private readonly long _offset;
	private readonly IItemSerializer<T> _serializer;
	private readonly ICriticalObject _lock;
	private readonly IEqualityComparer<T> _equalityComparer;
	private readonly EndianBinaryReader _reader;
	private readonly EndianBinaryWriter _writer;

	private bool _hasValue;
		
	private T _lastValue;

	public StreamMappedProperty(Stream stream, long offset, long size, IItemSerializer<T> serializer, IEqualityComparer<T> comparer = null, ICriticalObject @lock = null, Endianness endianess = Sphere10FrameworkDefaults.Endianness) 
		: this(stream, offset, size, serializer, false, comparer, @lock, endianess) {
	}

	public StreamMappedProperty(Stream stream, long offset, long size, IItemSerializer<T> serializer, bool useCache, IEqualityComparer<T> comparer = null, ICriticalObject @lock = null, Endianness endianess = Sphere10FrameworkDefaults.Endianness)
		: this(stream, offset, size, serializer, new EndianBinaryReader(EndianBitConverter.For(endianess), stream), new EndianBinaryWriter(EndianBitConverter.For(endianess), stream), useCache, comparer, @lock) {
	}

	public StreamMappedProperty(Stream stream, long offset, long size, IItemSerializer<T> serializer, EndianBinaryReader reader, EndianBinaryWriter writer, IEqualityComparer<T> comparer = null, ICriticalObject @lock = null)
		: this(stream, offset, size, serializer, reader, writer, false, comparer, @lock) {
	}

	public StreamMappedProperty(Stream stream, long offset, long size, IItemSerializer<T> serializer, EndianBinaryReader reader, EndianBinaryWriter writer, bool useCache, IEqualityComparer<T> comparer = null, ICriticalObject @lock = null) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		Guard.ArgumentNotNull(serializer, nameof(serializer));
		_stream = stream;
		_offset = offset;
		_serializer = serializer;
		_lock = @lock ?? new CriticalObject(); 
		_equalityComparer = comparer ?? EqualityComparer<T>.Default;
		_reader = reader;
		_writer = writer;
		UseCache = useCache;
		_hasValue = false;
		_lastValue = default;
	}

	public bool UseCache { get; }

	public T Value {
		get {
			using var _ = _lock.EnterAccessScope();
			if (!UseCache || !_hasValue) {
				_stream.Seek(_offset, SeekOrigin.Begin);
				_lastValue = _serializer.Deserialize(_reader);
				_hasValue = true;
			}
			return _lastValue;
		}
		set {
			using var _ = _lock.EnterAccessScope();
			if (UseCache && _hasValue && _equalityComparer.Equals(_lastValue, value))
				return;
			_stream.Seek(_offset, SeekOrigin.Begin);
			_serializer.Serialize(value, _writer);
			var oldValue = _lastValue;
			_lastValue = value;
			_hasValue = true;
			ValueChanged?.Invoke(oldValue, value);
		}
	}


	public void FlushCache() => _hasValue = false;

	public void ClearListeners() => ValueChanged = null;
}


