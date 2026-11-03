// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.ComponentModel;

public class ConvertingObjectEventArgs : System.EventArgs {
	public ConvertingObjectEventArgs(object value, Type destinationType) {
		Value = value;
		DestinationType = destinationType;
	}

	public object Value { get; set; }

	/// <summary>
	/// Destination type to convert the Value. Can be null if no destination type is required.
	/// </summary>
	public Type DestinationType { get; }

	public ConvertingStatus ConvertingStatus { get; set; } = ConvertingStatus.Converting;
}
