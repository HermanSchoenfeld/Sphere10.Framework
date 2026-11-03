// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

public class ObjectSpaceLabGroup {
	public int Id {
		get => field;
		set => SetField(ref field, value);
	} = 0;

	public string Code {
		get => field;
		set => SetField(ref field, value);
	} = string.Empty;

	public string Name {
		get => field;
		set => SetField(ref field, value);
	} = string.Empty;

	[Transient]
	public bool Dirty { get; set; }

	public ObjectSpaceLabGroup Clone() => (ObjectSpaceLabGroup)MemberwiseClone();

	private void SetField<T>(ref T field, T value) {
		if (EqualityComparer<T>.Default.Equals(field, value))
			return;
		field = value;
		Dirty = true;
	}
}
