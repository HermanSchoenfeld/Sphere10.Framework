// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public class DateTimeOffsetSerializer : ConstantSizeItemSerializerBase<DateTimeOffset> {

	public DateTimeOffsetSerializer() 
		: base(DateTimeSerializer.Instance.ConstantSize + TimeSpanSerializer.Instance.ConstantSize, false){
	}

	public static DateTimeOffsetSerializer Instance { get; } = new();

	public override void Serialize(DateTimeOffset item, EndianBinaryWriter writer, SerializationContext context) {
		DateTimeSerializer.Instance.Serialize(item.LocalDateTime, writer, context);
		TimeSpanSerializer.Instance.Serialize(item.Offset, writer, context);
	}

	public override DateTimeOffset Deserialize(EndianBinaryReader reader, SerializationContext context) {
		var datetime = DateTimeSerializer.Instance.Deserialize(reader, context);
		var timespan = TimeSpanSerializer.Instance.Deserialize(reader, context);

		// The stored local time is the same instant on any machine, so it is read as that instant and then given the stored offset.
		// Passing both to the constructor would throw whenever the stored offset is not this machine's own offset.
		return new DateTimeOffset(datetime).ToOffset(timespan);
	}
}

