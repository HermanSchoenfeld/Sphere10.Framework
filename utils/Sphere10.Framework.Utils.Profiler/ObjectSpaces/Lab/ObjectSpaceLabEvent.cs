// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

public class ObjectSpaceLabEvent {
	private long _id = 0;
	private long _recordId = 0;
	private int _kind = 0;
	private long _timestampTicks = 0;
	private string _message = string.Empty;

	public long Id { get => _id; set => SetField(ref _id, value); }

	public long RecordId { get => _recordId; set => SetField(ref _recordId, value); }

	public int Kind { get => _kind; set => SetField(ref _kind, value); }

	public long TimestampTicks { get => _timestampTicks; set => SetField(ref _timestampTicks, value); }

	public string Message { get => _message; set => SetField(ref _message, value); }

	[Transient]
	public bool Dirty { get; set; }

	public ObjectSpaceLabEvent Clone() => (ObjectSpaceLabEvent)MemberwiseClone();

	private void SetField<T>(ref T field, T value) {
		if (EqualityComparer<T>.Default.Equals(field, value))
			return;
		field = value;
		Dirty = true;
	}
}
