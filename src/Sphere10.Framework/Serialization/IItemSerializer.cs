// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Sphere10.Framework;

public interface IItemSerializer : IItemSizer {

	internal void PackedSerialize(object item, EndianBinaryWriter writer, SerializationContext context);

	internal object PackedDeserialize(EndianBinaryReader reader, SerializationContext context);
	
}

public interface IItemSerializer<TItem> : IItemSizer<TItem>, IItemSerializer {

	public new void Serialize(TItem item, EndianBinaryWriter writer, SerializationContext context);

	public new TItem Deserialize(EndianBinaryReader reader, SerializationContext context);

	void IItemSerializer.PackedSerialize(object item, EndianBinaryWriter writer, SerializationContext context)
		=> Serialize((TItem)item, writer, context);

	object IItemSerializer.PackedDeserialize(EndianBinaryReader reader, SerializationContext context)
		=> Deserialize(reader, context);

}

public static class IItemSerializerExtensions {

	#region Serialize<T>

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void Serialize<TItem>(this IItemSerializer<TItem> serializer, TItem item, EndianBinaryWriter writer) {
		using var context = new SerializationContext();
		serializer.Serialize(item, writer, context);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] SerializeBytesLE<TItem>(this IItemSerializer<TItem> serializer, TItem item)
		=> serializer.SerializeToBytes(item, Endianness.LittleEndian);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] SerializeBytesLE<TItem>(this IItemSerializer<TItem> serializer, TItem item, SerializationContext context)
		=> serializer.SerializeToBytes(item, Endianness.LittleEndian, context);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] SerializeToBytes<TItem>(this IItemSerializer<TItem> serializer, TItem item, Endianness endianness) {
		using var context = new SerializationContext();
		return SerializeToBytes(serializer, item, endianness, context);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte[] SerializeToBytes<TItem>(this IItemSerializer<TItem> serializer, TItem item, Endianness endianness, SerializationContext context) {
		using var stream = new MemoryStream();
		using var writer = new EndianBinaryWriter(EndianBitConverter.For(endianness), stream);
		serializer.Serialize(item, writer, context);
		stream.Flush();
		return stream.ToArray();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long SerializeReturnSize<TItem>(this IItemSerializer<TItem> serializer, TItem item, EndianBinaryWriter writer) {
		using var context = new SerializationContext();
		return serializer.SerializeReturnSize(item, writer, context);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long SerializeReturnSize<TItem>(this IItemSerializer<TItem> serializer, TItem item, EndianBinaryWriter writer, SerializationContext context) {
		var startPos = writer.BaseStream.Position;
		serializer.Serialize(item, writer, context);
		return writer.BaseStream.Position - startPos;

		// NOTE: if a malicious serializer writes more than it says, and rewinds stream Position
		// to hide its hidden data, any subsequent serializations will overwrite that hidden data.
		// Thus there is no attack vector of meaningful consequence here. Attempting to write
		// bloated data is responsibility of underlying Stream itself and will not result in security
		// vulnerability.
	}

	#endregion

	#region PackedSerialize

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void PackedSerialize(this IItemSerializer serializer, object item, EndianBinaryWriter writer) {
		using var context = new SerializationContext();
		serializer.PackedSerialize(item, writer, context);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long PackedSerializeReturnSize(this IItemSerializer serializer, object item, EndianBinaryWriter writer) {
		using var context = new SerializationContext();
		return serializer.PackedSerializeReturnSize(item, writer, context);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long PackedSerializeReturnSize(this IItemSerializer serializer, object item, EndianBinaryWriter writer, SerializationContext context) {
		var startPos = writer.BaseStream.Position;
		serializer.PackedSerialize(item, writer, context);
		return writer.BaseStream.Position - startPos;

		// NOTE: if a malicious serializer writes more than it says, and rewinds stream Position
		// to hide its hidden data, any subsequent serializations will overwrite that hidden data.
		// Thus there is no attack vector of meaningful consequence here. Attempting to write
		// bloated data is responsibility of underlying Stream itself and will not result in security
		// vulnerability.
	}

	#endregion

	#region Deserialize<T>

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem Deserialize<TItem>(this IItemSerializer<TItem> serializer, EndianBinaryReader reader) {
		using var context =	new SerializationContext();
		return serializer.Deserialize(reader, context);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem DeserializeBytesLE<TItem>(this IItemSerializer<TItem> serializer, ReadOnlySpan<byte> bytes)
		=> serializer.DeserializeBytes(bytes, Endianness.LittleEndian);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem DeserializeBytesLE<TItem>(this IItemSerializer<TItem> serializer, byte[] bytes)
		=> serializer.DeserializeBytes(bytes, Endianness.LittleEndian);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem DeserializeBytesLE<TItem>(this IItemSerializer<TItem> serializer, ReadOnlySpan<byte> bytes, SerializationContext context)
		=> serializer.DeserializeBytes(bytes, Endianness.LittleEndian, context);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem DeserializeBytes<TItem>(this IItemSerializer<TItem> serializer, ReadOnlySpan<byte> bytes, Endianness endianness)
		=> serializer.DeserializeBytes(bytes.ToArray(), endianness);  // TODO: need fast way to deal with deserializing spans

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem DeserializeBytes<TItem>(this IItemSerializer<TItem> serializer, ReadOnlySpan<byte> bytes, Endianness endianness, SerializationContext context)
		=> serializer.DeserializeBytes(bytes.ToArray(), endianness, context);  // TODO: need fast way to deal with deserializing spans
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem DeserializeBytes<TItem>(this IItemSerializer<TItem> serializer, byte[] bytes, Endianness endianness)  {
		using var context = new SerializationContext();
		return serializer.DeserializeBytes(bytes, endianness, context);
	}

	#endregion

	#region PackedDeserialize

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static object PackedDeserialize(this IItemSerializer serializer, EndianBinaryReader reader) {
		using var context = new SerializationContext();
		return serializer.PackedDeserialize(reader, context);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static TItem DeserializeBytes<TItem>(this IItemSerializer<TItem> serializer, byte[] bytes, Endianness endianness, SerializationContext context)  {
		using var stream = new MemoryStream(bytes);
		using var reader = new EndianBinaryReader(EndianBitConverter.For(endianness), stream);
		return serializer.Deserialize(reader, context);
	}

	#endregion
}

