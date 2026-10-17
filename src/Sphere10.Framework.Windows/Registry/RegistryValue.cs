// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.Win32;

namespace Sphere10.Framework.Windows;

/// <summary>A captured Windows registry value, retaining its exact type, raw data and native read failure.</summary>
public sealed record RegistryValue {
	private readonly object _data;

	public RegistryValue(RegistryAddress address, string name, RegistryValueKind kind, object data, Exception readError = null) {
		Guard.ArgumentNotNull(name, nameof(name));
		Address = address;
		Name = name;
		Kind = kind;
		_data = Copy(data);
		ReadError = readError;
	}

	public RegistryAddress Address { get; }
	public string Name { get; }
	public RegistryValueKind Kind { get; }
	public object Data => Copy(_data);
	public Exception ReadError { get; }

	private static object Copy(object data) => data is Array array ? array.Clone() : data;
}
