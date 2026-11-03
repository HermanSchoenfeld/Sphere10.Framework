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
	public long Id {
		get => field;
		set => SetField(ref field, value);
	} = 0;

	public long RecordId {
		get => field;
		set => SetField(ref field, value);
	} = 0;

	public int Kind {
		get => field;
		set => SetField(ref field, value);
	} = 0;

	public long TimestampTicks {
		get => field;
		set => SetField(ref field, value);
	} = 0;

	public string Message {
		get => field;
		set => SetField(ref field, value);
	} = string.Empty;

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
