// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

public sealed class VoidSerializer() : ConstantSizeItemSerializerBase<Void>(0, false) {

	public static VoidSerializer Instance { get; } = new();

	public override void Serialize(Void item, EndianBinaryWriter writer, SerializationContext context) {
	}

	public override Void Deserialize(EndianBinaryReader reader, SerializationContext context) {
		return Void.Value;
	}
}

