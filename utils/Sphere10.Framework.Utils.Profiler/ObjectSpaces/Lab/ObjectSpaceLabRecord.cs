// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Utils.Profiler.ObjectSpaces.Lab;

public class ObjectSpaceLabRecord {
	private long _id;
	private string _code = string.Empty;
	private int _category;
	private decimal _balance;
	private byte[] _payload = Array.Empty<byte>();
	private string _notes = string.Empty;

	public long Id { get => _id; set => SetField(ref _id, value); }

	public string Code { get => _code; set => SetField(ref _code, value); }

	public int Category { get => _category; set => SetField(ref _category, value); }

	public decimal Balance { get => _balance; set => SetField(ref _balance, value); }

	public byte[] Payload { get => _payload; set => SetField(ref _payload, value); }

	public string Notes { get => _notes; set => SetField(ref _notes, value); }

	[Transient]
	public bool Dirty { get; set; }

	public ObjectSpaceLabRecord Clone() => new() {
		_id = _id,
		_code = _code,
		_category = _category,
		_balance = _balance,
		_payload = _payload == null ? null : Tools.Array.Clone(_payload),
		_notes = _notes,
		Dirty = Dirty
	};

	private void SetField<T>(ref T field, T value) {
		if (EqualityComparer<T>.Default.Equals(field, value))
			return;
		field = value;
		Dirty = true;
	}
}
