// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace Sphere10.Framework;

/// <summary>
/// Extends the system <see cref="TypeDescriptor"/> class with custom type converters that were missing in .NET standard library
/// </summary>
public class TypeDescriptorEx {

	// .NET has no converter for IPAddress.
	private static readonly Dictionary<Type, TypeConverter> _customConverters = new() {
		{ typeof(IPAddress), new IPAddressTypeConverter() }
	};

	/// <summary>The converter for a type: one registered with <see cref="RegisterConverter"/>, otherwise the component model's.</summary>
	public static TypeConverter GetConverter(Type type) {
		lock (_customConverters)
			if (_customConverters.TryGetValue(type, out var converter))
				return converter;
		return TypeDescriptor.GetConverter(type);
	}

	/// <summary>Supplies the converter for a type the component model has none for. A library registers the converters for its own types.</summary>
	public static void RegisterConverter(Type type, TypeConverter converter) {
		Guard.ArgumentNotNull(type, nameof(type));
		Guard.ArgumentNotNull(converter, nameof(converter));
		lock (_customConverters)
			_customConverters[type] = converter;
	}
}

