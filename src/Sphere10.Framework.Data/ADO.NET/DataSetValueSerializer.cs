// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Text;

namespace Sphere10.Framework.Data;

/// <summary>Composes primitive serializers for the existing binary DataSet field representation.</summary>
internal sealed class DataSetValueSerializer : ItemSerializerBase<object> {
	private readonly Type _valueType;
	private readonly ByteArraySerializer _bytesSerializer;

	// A column supplies its type; the surrounding DataSet framing carries null and DBNull markers.
	public DataSetValueSerializer(Type valueType) {
		_valueType = valueType;
		_bytesSerializer = new ByteArraySerializer(SizeDescriptorStrategy.UseUInt32);
	}

	public override void Serialize(object item, EndianBinaryWriter writer, SerializationContext context) {
		// These special values retain their historical framing rather than the default factory formats.
		if (_valueType == typeof(string)) {
			_bytesSerializer.Serialize(Encoding.UTF8.GetBytes((string)item), writer, context);
			return;
		}
		if (_valueType == typeof(byte[])) {
			_bytesSerializer.Serialize((byte[])item, writer, context);
			return;
		}
		if (_valueType == typeof(Guid)) {
			_bytesSerializer.Serialize(((Guid)item).ToByteArray(), writer, context);
			return;
		}
		if (_valueType == typeof(DateTime)) {
			PrimitiveSerializer<double>.Instance.Serialize(((DateTime)item).ToOADate(), writer, context);
			return;
		}

		// A character is a two-byte code unit here; it is independent of the writer's text encoding.
		switch (Type.GetTypeCode(_valueType)) {
			case TypeCode.Int16:
				PrimitiveSerializer<short>.Instance.Serialize((short)item, writer, context);
				break;
			case TypeCode.Int32:
				PrimitiveSerializer<int>.Instance.Serialize((int)item, writer, context);
				break;
			case TypeCode.Int64:
				PrimitiveSerializer<long>.Instance.Serialize((long)item, writer, context);
				break;
			case TypeCode.Single:
				PrimitiveSerializer<float>.Instance.Serialize((float)item, writer, context);
				break;
			case TypeCode.Double:
				PrimitiveSerializer<double>.Instance.Serialize((double)item, writer, context);
				break;
			case TypeCode.Boolean:
				PrimitiveSerializer<bool>.Instance.Serialize((bool)item, writer, context);
				break;
			case TypeCode.Decimal:
				PrimitiveSerializer<decimal>.Instance.Serialize((decimal)item, writer, context);
				break;
			case TypeCode.Char:
				PrimitiveSerializer<ushort>.Instance.Serialize((char)item, writer, context);
				break;
			case TypeCode.Byte:
				PrimitiveSerializer<byte>.Instance.Serialize((byte)item, writer, context);
				break;
			default:
				throw new NotSupportedException($"Type {_valueType} is not supported by the binary DataSet format.");
		}
	}

	public override long CalculateSize(SerializationContext context, object item) {
		if (_valueType == typeof(string))
			return sizeof(int) + Encoding.UTF8.GetByteCount((string)item);
		if (_valueType == typeof(byte[]))
			return sizeof(int) + ((byte[])item).Length;
		if (_valueType == typeof(Guid))
			return sizeof(int) + 16;
		if (_valueType == typeof(DateTime))
			return sizeof(double);
		return Type.GetTypeCode(_valueType) switch {
			TypeCode.Int16 or TypeCode.Char => 2,
			TypeCode.Int32 or TypeCode.Single => 4,
			TypeCode.Int64 or TypeCode.Double => 8,
			TypeCode.Boolean or TypeCode.Byte => 1,
			TypeCode.Decimal => 16,
			_ => throw new NotSupportedException($"Type {_valueType} is not supported by the binary DataSet format.")
		};
	}

	public override object Deserialize(EndianBinaryReader reader, SerializationContext context) {
		if (_valueType == typeof(string))
			return Encoding.UTF8.GetString(ReadBlob(reader, context));
		if (_valueType == typeof(byte[]))
			return ReadBlob(reader, context);
		if (_valueType == typeof(Guid))
			return new Guid(ReadBlob(reader, context));
		if (_valueType == typeof(DateTime))
			return DateTime.FromOADate(PrimitiveSerializer<double>.Instance.Deserialize(reader, context));
		return Type.GetTypeCode(_valueType) switch {
			TypeCode.Int16 => PrimitiveSerializer<short>.Instance.Deserialize(reader, context),
			TypeCode.Int32 => PrimitiveSerializer<int>.Instance.Deserialize(reader, context),
			TypeCode.Int64 => PrimitiveSerializer<long>.Instance.Deserialize(reader, context),
			TypeCode.Single => PrimitiveSerializer<float>.Instance.Deserialize(reader, context),
			TypeCode.Double => PrimitiveSerializer<double>.Instance.Deserialize(reader, context),
			TypeCode.Boolean => PrimitiveSerializer<bool>.Instance.Deserialize(reader, context),
			TypeCode.Decimal => PrimitiveSerializer<decimal>.Instance.Deserialize(reader, context),
			TypeCode.Char => (char)PrimitiveSerializer<ushort>.Instance.Deserialize(reader, context),
			TypeCode.Byte => PrimitiveSerializer<byte>.Instance.Deserialize(reader, context),
			_ => throw new NotSupportedException($"Type {_valueType} is not supported by the binary DataSet format.")
		};
	}

	// Length-prefixed values are complete records; a truncated payload must not become a shorter valid value.
	private static byte[] ReadBlob(EndianBinaryReader reader, SerializationContext context) {
		var length = PrimitiveSerializer<int>.Instance.Deserialize(reader, context);
		return reader.ReadBytesOrThrow(length);
	}
}
