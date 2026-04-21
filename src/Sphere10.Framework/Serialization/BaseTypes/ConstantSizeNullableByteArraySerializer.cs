// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Serializes a nullable byte array with constant size by prefixing a bool flag (1 byte).
/// Total serialized size = 1 (bool) + arraySize (data or padding).
/// This allows distinguishing between null and a zero-filled array (e.g., zero hash).
/// </summary>
public class ConstantSizeNullableByteArraySerializer : ConstantSizeItemSerializerBase<byte[]> {
    private readonly int _arraySize;
    private readonly byte[] _padding;

    public ConstantSizeNullableByteArraySerializer(int arraySize) 
        : base(sizeof(bool) + arraySize, true) {
        Guard.ArgumentGTE(arraySize, 0, nameof(arraySize));
        _arraySize = arraySize;
        _padding = new byte[arraySize]; // Pre-allocate zero-filled padding
    }

    public override void Serialize(byte[] item, EndianBinaryWriter writer, SerializationContext context) {
        if (item == null) {
            writer.Write(false); // Null flag
            writer.Write(_padding); // Write padding to maintain constant size
        } else {
            Guard.Argument(item.Length == _arraySize, nameof(item), $"Array length must be {_arraySize}");
            writer.Write(true); // Non-null flag
            writer.Write(item);
        }
    }

    public override byte[] Deserialize(EndianBinaryReader reader, SerializationContext context) {
        var hasValue = reader.ReadBoolean();
        var data = reader.ReadBytes(_arraySize);
		if (!hasValue) {
			Guard.Ensure(ByteArrayEqualityComparer.Instance.Equals(data, _padding), "Expected padding for null value");
			return null;
		}
		return data;
    }
}
